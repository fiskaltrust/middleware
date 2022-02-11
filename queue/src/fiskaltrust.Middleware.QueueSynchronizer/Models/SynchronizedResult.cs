using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.QueueSynchronizer
{
    internal class SynchronizedResult
    {
        public AutoResetEvent AutoResetEvent { get; set; } = new AutoResetEvent(false);
        public ReceiptResponse Response { get; set; }
        public ExceptionDispatchInfo ExceptionDispatchInfo { get; set; }
    }
}