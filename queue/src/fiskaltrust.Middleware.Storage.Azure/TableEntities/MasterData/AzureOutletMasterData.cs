using System;
using Microsoft.Azure.Cosmos.Table;

namespace fiskaltrust.Middleware.Storage.Azure.TableEntities
{
    public class AzureOutletMasterData : TableEntity
    {
        public Guid OutletId { get; set; }
        public string OutletName { get; set; }
        public string Street { get; set; }
        public string Zip { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string VatId { get; set; }
    }
}
