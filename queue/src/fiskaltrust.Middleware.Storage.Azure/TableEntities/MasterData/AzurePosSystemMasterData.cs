using System;
using Microsoft.Azure.Cosmos.Table;

namespace fiskaltrust.Middleware.Storage.Azure.TableEntities
{
    public class AzurePosSystemMasterData : TableEntity
    {
        public Guid PosSystemId { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string SoftwareVersion { get; set; }
        public string BaseCurrency { get; set; }
        public string Type { get; set; }
    }
}
