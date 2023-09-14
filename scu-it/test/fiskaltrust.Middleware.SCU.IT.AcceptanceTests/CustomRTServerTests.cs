using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.IT.CustomRTServer;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.AcceptanceTests
{
    public class CustomRTServerTests : ITSSCDTests
    {
        private static readonly Uri _serverUri = new Uri("https://f51f-88-116-45-202.ngrok-free.app/");
        private readonly CustomRTServerConfiguration _config = new CustomRTServerConfiguration
        {
            ServerUrl = _serverUri.ToString(),
            Username = "0001ab05",
            Password = "admin",
            AccountMasterData = JsonConvert.SerializeObject(new AccountMasterData
            {
                AccountId = Guid.NewGuid(),
                VatId = "MTLFNC75A16E783N"
            })
        };

        protected override string SerialNumber => "96SRT001239";

        protected override IMiddlewareBootstrapper GetMiddlewareBootstrapper() => new ScuBootstrapper
        {
            Id = Guid.NewGuid(),
            Configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(_config))
        };
    }
}

// TODO TEsts
// - Add test that sends wrong CUstomer IVA
// - Add test that sends customer data without iva