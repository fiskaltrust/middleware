using System;

namespace fiskaltrust.storage.V0.MasterData
{
    public class AccountMasterData
    {
        public Guid AccountId { get; set; }
        public string AccountName { get; set; }
        public string Street { get; set; }
        public string Zip { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string TaxId { get; set; }
        public string VatId { get; set; }
    }
}