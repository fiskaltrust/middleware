using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Contracts.Models
{
    public class StorageBaseInitConfiguration
    {
        public ftCashBox CashBox { get; set; }
        public List<ftQueue> Queues { get; set; }
        public List<ftQueueAT> QueuesAT { get; set; }
        public List<ftQueueDE> QueuesDE { get; set; }
        public List<ftQueueFR> QueuesFR { get; set; }
        public List<ftQueueME> QueuesME { get; set; }
        public List<ftSignaturCreationUnitAT> SignaturCreationUnitsAT { get; set; }
        public List<ftSignaturCreationUnitDE> SignaturCreationUnitsDE { get; set; }
        public List<ftSignaturCreationUnitFR> SignaturCreationUnitsFR { get; set; }
        public List<ftSignaturCreationUnitME> SignaturCreationUnitsME { get; set; }
        public MasterDataConfiguration MasterData { get; set; }
    }
}
