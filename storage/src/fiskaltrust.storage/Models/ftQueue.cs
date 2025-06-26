using System;
using System.ComponentModel;

namespace fiskaltrust.storage.V0
{
    public class ftQueue
    {
        //PK
        public Guid ftQueueId { get; set; }

        //FK
        public Guid ftCashBoxId { get; set; }

        public long ftCurrentRow { get; set; }

        public long ftQueuedRow { get; set; }

        public long ftReceiptNumerator { get; set; }

        public decimal ftReceiptTotalizer { get; set; }

        public string ftReceiptHash { get; set; }

        public DateTime? StartMoment { get; set; }
        public DateTime? StopMoment { get; set; }

        /// <summary>
        /// ContryCode for default behaviour
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// Timeout in ms for the queued Receipt
        /// </summary>
        [DefaultValue(1500)]
        public int Timeout { get; set; }

        public long TimeStamp { get; set; }
    }
}