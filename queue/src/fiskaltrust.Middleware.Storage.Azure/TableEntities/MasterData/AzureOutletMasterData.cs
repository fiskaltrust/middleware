using System;

namespace fiskaltrust.Middleware.Storage.Azure.TableEntities
{
    public class AzureOutletMasterData : BaseTableEntity
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
