using System;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Contracts.Models
{
    public class MiddlewareConfiguration
    {
        public Guid QueueId { get; set; }
        public Guid CashBoxId { get; set; }
        public int ReceiptRequestMode { get; set; }
        public int JournalChunkSize { get; set; } = 1024 * 1024; // 1 MB
        public bool IsSandbox { get; set; }
        public string ServiceFolder { get; set; }
        public bool AddEReceiptLink { get; set; }
        public Action<string> OnMessage { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
    }
}