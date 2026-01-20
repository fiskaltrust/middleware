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
        public List<ftQueueBE> QueuesBE { get; set; }
        public List<ftQueueDE> QueuesDE { get; set; }
        public List<ftQueueES> QueuesES { get; set; }
        public List<ftQueueFR> QueuesFR { get; set; }
        public List<ftQueueGR> QueuesGR { get; set; }
        public List<ftQueueIT> QueuesIT { get; set; }
        public List<ftQueueME> QueuesME { get; set; }
        public List<ftSignaturCreationUnitAT> SignaturCreationUnitsAT { get; set; }
        public List<ftSignaturCreationUnitBE> SignaturCreationUnitsBE { get; set; }
        public List<ftSignaturCreationUnitDE> SignaturCreationUnitsDE { get; set; }
        public List<ftSignaturCreationUnitES> SignaturCreationUnitsES { get; set; }
        public List<ftSignaturCreationUnitFR> SignaturCreationUnitsFR { get; set; }
        public List<ftSignaturCreationUnitGR> SignaturCreationUnitsGR { get; set; }
        public List<ftSignaturCreationUnitIT> SignaturCreationUnitsIT { get; set; }
        public List<ftSignaturCreationUnitME> SignaturCreationUnitsME { get; set; }
        public MasterDataConfiguration MasterData { get; set; }
    }
}
