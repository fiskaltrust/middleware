using System;

namespace fiskaltrust.storage.V0
{
    public class ftQueueDK
    {
        public Guid ftQueueDKId { get; set; }

        public Guid? ftSignaturCreationUnitDKId { get; set; }

        public string CashBoxIdentification { get; set; }

        public long TimeStamp { get; set; }
    }
}