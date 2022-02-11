using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Queue.InMemory.IntegrationTest.Configuration
{
    public class Configuration
    {
        public List<ftQueueAT> init_ftQueueAT { get; set; }
        public List<ftQueueDE> init_ftQueueDE { get; set; }
        public List<ftQueueFR> init_ftQueueFR { get; set; }
        public List<ftSignaturCreationUnitAT> init_ftSignaturCreationUnitAT { get; set; }
        public List<ftSignaturCreationUnitDE> init_ftSignaturCreationUnitDE { get; set; }
        public List<ftSignaturCreationUnitFR> init_ftSignaturCreationUnitFR { get; set; }
        public ftCashBox init_ftCashBox { get; set; }
        public List<ftQueue> init_ftQueue { get; set; }
    }
}
