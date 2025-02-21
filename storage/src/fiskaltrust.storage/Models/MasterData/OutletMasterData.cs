using System;

namespace fiskaltrust.storage.V0.MasterData
{
    public class OutletMasterData
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