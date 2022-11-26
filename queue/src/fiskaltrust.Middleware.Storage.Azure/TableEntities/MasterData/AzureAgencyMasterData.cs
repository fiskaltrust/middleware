using System;

namespace fiskaltrust.Middleware.Storage.Azure.TableEntities
{
    public class AzureAgencyMasterData : TableEntity
    {
        public Guid AgencyId { get; set; }
        public string Name { get; set; }
        public string Street { get; set; }
        public string Zip { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string TaxId { get; set; }
        public string VatId { get; set; }
    }
}
