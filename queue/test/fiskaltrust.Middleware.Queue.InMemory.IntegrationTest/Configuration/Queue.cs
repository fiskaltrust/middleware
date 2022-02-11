using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Queue.InMemory.IntegrationTest.Configuration
{
    public class Queue
    {
        public Guid Id { get; set; }
        public List<string> Url { get; set; }
        public Configuration Configuration { get; set; }
    }
}
