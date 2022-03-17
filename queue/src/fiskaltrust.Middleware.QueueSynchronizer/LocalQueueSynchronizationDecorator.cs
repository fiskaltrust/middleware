using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.QueueSynchronizer
{
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
            syncResult.AutoResetEvent.WaitOne();
            if (!_stateDictionary.TryRemove(identifier, out var synchronizedResult))
            {
                throw new KeyNotFoundException(identifier.ToString());
            }

            _logger.LogTrace("LocalQueueSynchronizationDecorator.ProcessAsync: Got receipt result.");
            if (synchronizedResult.ExceptionDispatchInfo != null)
            {
                synchronizedResult.ExceptionDispatchInfo.Throw();
            }

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
}
