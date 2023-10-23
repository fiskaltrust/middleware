using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.QueueSynchronizer
{
    // https://devblogs.microsoft.com/pfxteam/building-async-coordination-primitives-part-2-asyncautoresetevent/
    public class AsyncAutoResetEvent
    {
        private readonly Queue<TaskCompletionSource<bool>> _waits = new Queue<TaskCompletionSource<bool>>();
        private bool _signaled;

        public Task WaitAsync()
        {
            lock (_waits)
            {
                if (_signaled)
                {
                    _signaled = false;
                    return Task.FromResult(true);
                }
                else
                {
                    var tcs = new TaskCompletionSource<bool>();
                    _waits.Enqueue(tcs);
                    return tcs.Task;
                }
            }
        }

        public void Set()
        {
            TaskCompletionSource<bool> toRelease = null;
            lock (_waits)
            {
                if (_waits.Count > 0)
                {
                    toRelease = _waits.Dequeue();
                }
                else if (!_signaled)
                {
                    _signaled = true;
                }
            }
            toRelease?.SetResult(true);
        }
    }

    internal class SynchronizedResult
    {
#if NET461
        public AsyncAutoResetEvent AutoResetEvent { get; set; } = new AsyncAutoResetEvent();
#endif
        public ReceiptResponse Response { get; set; }
        public ExceptionDispatchInfo ExceptionDispatchInfo { get; set; }
    }

}