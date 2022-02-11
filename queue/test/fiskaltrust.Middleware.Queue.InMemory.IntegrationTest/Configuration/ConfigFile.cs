using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Queue.InMemory.IntegrationTest.Configuration
{
    public class ConfigFile
    {
        public Guid ftCashBoxId { get; set; }
        public Guid QueueId { get; set; }
        public long TimeStamp { get; set; }

        public List<Queue> ftQueues { get; set; }
    }
}
