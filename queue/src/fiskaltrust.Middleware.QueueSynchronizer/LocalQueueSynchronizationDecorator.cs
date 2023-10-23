using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.QueueSynchronizer
{
#if NET461
    public sealed class LocalQueueSynchronizationDecorator : ISignProcessor, IDisposable
    {
        private readonly ISignProcessor _signProcessor;
        private readonly ILogger<LocalQueueSynchronizationDecorator> _logger;
        private readonly BlockingCollection<(Guid identifier, ReceiptRequest request)> _concurrentQueue;
        private readonly ConcurrentDictionary<Guid, SynchronizedResult> _stateDictionary;

        public LocalQueueSynchronizationDecorator(ISignProcessor signProcessor, ILogger<LocalQueueSynchronizationDecorator> logger)
        {
            _signProcessor = signProcessor;
            _logger = logger;
            _concurrentQueue = new BlockingCollection<(Guid, ReceiptRequest)>(new ConcurrentQueue<(Guid, ReceiptRequest)>());
            _stateDictionary = new ConcurrentDictionary<Guid, SynchronizedResult>();

            _ = Task.Run(ProcessReceipts);
        }

        public async Task<ReceiptResponse> ProcessAsync(ReceiptRequest receiptRequest)
        {
            _logger.LogTrace("LocalQueueSynchronizationDecorator.ProcessAsync called.");
            var identifier = Guid.NewGuid();
            var syncResult = new SynchronizedResult();
            _stateDictionary.TryAdd(identifier, syncResult);
            _concurrentQueue.Add((identifier, receiptRequest));

            _logger.LogTrace("LocalQueueSynchronizationDecorator.ProcessAsync: Waiting until result is available.");
            await syncResult.AutoResetEvent.WaitAsync();
            if (!_stateDictionary.TryRemove(identifier, out var synchronizedResult))
            {
                throw new KeyNotFoundException(identifier.ToString());
            }

            _logger.LogTrace("LocalQueueSynchronizationDecorator.ProcessAsync: Got receipt result.");
            synchronizedResult.ExceptionDispatchInfo?.Throw();

            return await Task.FromResult(synchronizedResult.Response).ConfigureAwait(false);
        }

        private async Task ProcessReceipts()
        {
            while (!_concurrentQueue.IsCompleted && _concurrentQueue.TryTake(out var tuple, -1))
            {
                _logger.LogTrace("LocalQueueSynchronizationDecorator.ProcessReceipts: Processing a new receipt.");
                try
                {
                    var response = await _signProcessor.ProcessAsync(tuple.request).ConfigureAwait(false);
                    _stateDictionary[tuple.identifier].Response = response;
                }
                catch (Exception ex)
                {
                    _stateDictionary[tuple.identifier].ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    _stateDictionary[tuple.identifier].AutoResetEvent.Set();
                }
            }
        }

        public void Dispose() => _concurrentQueue.Dispose();
    }

#else
using System.Threading;
using System.Threading.Channels;

    public sealed class LocalQueueSynchronizationDecorator : ISignProcessor, IDisposable
    {
        private readonly ISignProcessor _signProcessor;
        private readonly ILogger<LocalQueueSynchronizationDecorator> _logger;
        private readonly Channel<(ReceiptRequest request, ChannelWriter<SynchronizedResult>)> _channel;
        private volatile bool _disposed = false;

        public LocalQueueSynchronizationDecorator(ISignProcessor signProcessor, ILogger<LocalQueueSynchronizationDecorator> logger)
        {
            _signProcessor = signProcessor;
            _logger = logger;
            _channel = Channel.CreateUnbounded<(ReceiptRequest, ChannelWriter<SynchronizedResult>)>();

            _ = Task.Run(ProcessReceipts);
        }

        public async Task<ReceiptResponse> ProcessAsync(ReceiptRequest receiptRequest)
        {
            _logger.LogTrace("LocalQueueSynchronizationDecorator.ProcessAsync called.");
            var responseChannel = Channel.CreateBounded<SynchronizedResult>(1);

            if (!await _channel.Writer.WaitToWriteAsync())
            {
                throw new ObjectDisposedException(nameof(ISignProcessor), "Queue was already disposed");
            }

            await _channel.Writer.WriteAsync((receiptRequest, responseChannel.Writer));

            _logger.LogTrace("LocalQueueSynchronizationDecorator.ProcessAsync: Waiting until result is available.");
            var synchronizedResult = await responseChannel.Reader.ReadAsync();

            _logger.LogTrace("LocalQueueSynchronizationDecorator.ProcessAsync: Got receipt result.");
            synchronizedResult.ExceptionDispatchInfo?.Throw();

            return await Task.FromResult(synchronizedResult.Response).ConfigureAwait(false);
        }

        private async Task ProcessReceipts()
        {
            while (true)
            {
                if (!await _channel.Reader.WaitToReadAsync())
                {
                    break;
                }

                var (request, responseChannel) = await _channel.Reader.ReadAsync();

                _logger.LogTrace("LocalQueueSynchronizationDecorator.ProcessReceipts: Processing a new receipt.");
                var synchronizedResult = new SynchronizedResult();

                try
                {
                    if (_disposed)
                    {
                        throw new ObjectDisposedException(nameof(ISignProcessor), "Queue was already disposed");
                    }

                    var response = await _signProcessor.ProcessAsync(request).ConfigureAwait(false);
                    synchronizedResult.Response = response;
                }
                catch (Exception ex)
                {
                    synchronizedResult.ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    await responseChannel.WriteAsync(synchronizedResult);
                    responseChannel.Complete();
                }
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _channel.Writer.Complete();
        }
    }
#endif
}
