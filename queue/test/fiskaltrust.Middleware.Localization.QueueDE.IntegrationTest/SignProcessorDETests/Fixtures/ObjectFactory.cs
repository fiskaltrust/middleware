using System.IO;
using AutoFixture;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0.MasterData;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures
{
    public class TestObjectFactory
    {
        public static ReceiptRequest GetReceipt(string jsonFileDir)
        {
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(File.ReadAllText(Path.Combine(jsonFileDir, "Request.json")));
            return receiptRequest;
        }
        public static MasterDataConfiguration CreateMasterdata()
        {
            var fixture = new Fixture();
            var accountMasterData = fixture.Create<AccountMasterData>();
            var outletMasterData = fixture.Create<OutletMasterData>();
            var agencyMasterData = fixture.CreateMany<AgencyMasterData>();
            var posSystemMasterData = fixture.CreateMany<PosSystemMasterData>();
            return new MasterDataConfiguration()
            {
                Account = accountMasterData,
                Agencies = agencyMasterData,
                Outlet = outletMasterData,
                PosSystems = posSystemMasterData
            };
        }
    }
}
