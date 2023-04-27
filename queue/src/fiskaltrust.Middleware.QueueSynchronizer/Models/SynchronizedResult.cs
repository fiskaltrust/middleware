using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.QueueSynchronizer
{
    internal class SynchronizedResult
    {
        public ReceiptResponse Response { get; set; }
        public ExceptionDispatchInfo ExceptionDispatchInfo { get; set; }
    }
}