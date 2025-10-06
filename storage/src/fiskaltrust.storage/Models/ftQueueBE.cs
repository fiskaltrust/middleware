using System;

namespace fiskaltrust.storage.V0
{
    public class ftQueueBE
    {
        public Guid ftQueueBEId { get; set; }

        public Guid? ftSignaturCreationUnitBEId { get; set; }

        public string CashBoxIdentification { get; set; }

        public long TimeStamp { get; set; }
    }
}