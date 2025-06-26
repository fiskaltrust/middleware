using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using Microsoft.Extensions.Logging;
using ISignProcessor = fiskaltrust.Middleware.Localization.v2.Interface.ISignProcessor;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using System.Diagnostics;

namespace fiskaltrust.Middleware.Localization.v2.Synchronizer
{
    public sealed class LocalQueueSynchronizationDecorator : ISignProcessor, IDisposable
    {
        private readonly ISignProcessor _signProcessor;
        private readonly ActionBlock<(ReceiptRequest request, Activity? activity, TaskCompletionSource<ReceiptResponse?> tcs)> _processor;
        private volatile bool _disposed = false;

        public LocalQueueSynchronizationDecorator(ISignProcessor signProcessor)
        {
            _signProcessor = signProcessor;

            _processor = new(async task =>
            {
                var currentActivity = Activity.Current;
                try
                {
                    if (_disposed)
                    {
                        throw new ObjectDisposedException(nameof(ISignProcessor), "Queue has already been disposed.");
                    }
                    Activity.Current = task.activity;
                    var response = await _signProcessor.ProcessAsync(task.request).ConfigureAwait(false);
                    task.tcs.SetResult(response);
                }
                catch (Exception ex)
                {
                    task.tcs.SetException(ex);
                }
                finally
                {
                    Activity.Current = currentActivity;
                }
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1 // Sequential processing
            });
        }

        public Task<ReceiptResponse?> ProcessAsync(ReceiptRequest request)
        {
            var tcs = new TaskCompletionSource<ReceiptResponse?>();
            _processor.Post((request, Activity.Current, tcs));
            return tcs.Task;
        }

        public void Dispose()
        {
            _disposed = true;
            _processor.Complete();
        }
    }
}
