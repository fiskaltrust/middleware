using System;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.MasterData
{
    public class AzureTableStorageOutletMasterData : BaseTableEntity
    {
        public Guid OutletId { get; set; }
        public string OutletName { get; set; }
        public string Street { get; set; }
        public string Zip { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string VatId { get; set; }
        public string LocationId { get; set; }
    }
}
