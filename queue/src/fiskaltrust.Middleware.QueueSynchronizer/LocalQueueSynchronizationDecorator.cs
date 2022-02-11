using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;

namespace fiskaltrust.Middleware.QueueSynchronizer
{
    public sealed class LocalQueueSynchronizationDecorator : ISignProcessor, IDisposable
    {
        private readonly ISignProcessor _signProcessor;
        private readonly BlockingCollection<(Guid identifier, ReceiptRequest request)> _concurrentQueue;
        private readonly ConcurrentDictionary<Guid, SynchronizedResult> _stateDictionary;

        public LocalQueueSynchronizationDecorator(ISignProcessor signProcessor)
        {
            _signProcessor = signProcessor;
            _concurrentQueue = new BlockingCollection<(Guid, ReceiptRequest)>(new ConcurrentQueue<(Guid, ReceiptRequest)>());
            _stateDictionary = new ConcurrentDictionary<Guid, SynchronizedResult>();

            _ = Task.Run(ProcessReceipts);
        }

        public async Task<ReceiptResponse> ProcessAsync(ReceiptRequest receiptRequest)
        {
            var identifier = Guid.NewGuid();
            var syncResult = new SynchronizedResult();
            _stateDictionary.TryAdd(identifier, syncResult);
            _concurrentQueue.Add((identifier, receiptRequest));

            syncResult.AutoResetEvent.WaitOne();
            if (!_stateDictionary.TryRemove(identifier, out var synchronizedResult))
            {
                throw new KeyNotFoundException(identifier.ToString());
            }

            if(synchronizedResult.ExceptionDispatchInfo != null)
            {
                synchronizedResult.ExceptionDispatchInfo.Throw();
            }

            return await Task.FromResult(synchronizedResult.Response).ConfigureAwait(false);
        }

        private async Task ProcessReceipts()
        {
            while (!_concurrentQueue.IsCompleted && _concurrentQueue.TryTake(out var tuple, -1))
            {
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
