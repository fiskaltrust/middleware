using System;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.MasterData
{
    public class AzurePosSystemMasterData : BaseTableEntity
    {
        public Guid PosSystemId { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string SoftwareVersion { get; set; }
        public string BaseCurrency { get; set; }
        public string Type { get; set; }
    }
}
