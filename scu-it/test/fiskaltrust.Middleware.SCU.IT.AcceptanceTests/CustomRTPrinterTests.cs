using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.AcceptanceTests
{
    public class CustomRTPrinterTests : ITSSCDTests
    {
        private static readonly Uri _serverUri = new Uri("http://192.168.0.196/");
        private readonly CustomRTPrinterConfiguration _config = new CustomRTPrinterConfiguration
        {
            DeviceUrl = _serverUri.ToString(),
            Username = "STITE350119",
            Password = "STITE350119",
            ClientTimeoutMs = 60 * 1000,
            ServerTimeoutMs = 60 * 1000
        };

        protected override string SerialNumber => "STITE350119";

        protected override IMiddlewareBootstrapper GetMiddlewareBootstrapper(Guid queueId) => new ScuBootstrapper
        {
            Id = queueId,
            Configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(_config))
        };
    }
}