using System;

namespace fiskaltrust.Middleware.Journal.DE.Common.Models
{
    public class DailyClosingReceipt
    {
        public long ZNumber { get; set; }
        public DateTime ZTime { get; set; }
        public long QueueRow { get; set; }
    }
}
