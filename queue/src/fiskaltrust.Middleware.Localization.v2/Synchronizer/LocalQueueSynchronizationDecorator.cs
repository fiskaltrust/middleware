using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using Microsoft.Extensions.Logging;
using ISignProcessor = fiskaltrust.Middleware.Localization.v2.Interface.ISignProcessor;

namespace fiskaltrust.Middleware.Localization.v2.Synchronizer
{
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

        public async Task<ReceiptResponse?> ProcessAsync(ReceiptRequest receiptRequest)
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
}
