using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Fixtures
{
    public class MasterDataFactoryTestsFixture
    {
        public MasterDataConfiguration MasterData { get; } = new MasterDataConfiguration
        {
            Account = new AccountMasterData
            {
                AccountId = Guid.NewGuid(),
                AccountName = "Test Company GmbH",
                City = "Düsseldorf",
                Country = "DE",
                Street = "Toulouser Allee 19a",
                TaxId = "123456",
                VatId = "DE323821961",
                Zip = "40211"
            },
            Outlet = new OutletMasterData
            {
                OutletId = Guid.NewGuid(),
                OutletName = "Factory 1",
                City = "Berlin",
                Country = "DE",
                Street = "Ohther Allee 19a",
                VatId = "DE323821961",
                Zip = "10211"
            },
            Agencies = new List<AgencyMasterData>
            {
                new AgencyMasterData
                {
                    AgencyId = Guid.Parse("4360b32a-d242-475d-8786-e6436b63fce4"),
                    Name = "ASFINAG",
                    City = "Wien",
                    Country = "AT",
                    Street = "Pirate Street 666",
                    VatId = "AT123821961",
                    Zip = "1010",
                    TaxId = "12345"
                }
            },
            PosSystems = new List<PosSystemMasterData>
            {
                new PosSystemMasterData
                {
                    PosSystemId = Guid.NewGuid(),
                    BaseCurrency = "EUR",
                    Brand = "APRO",
                    Model = "AK-123",
                    SoftwareVersion = "12.0"
                }
            }
        };
    }
}
