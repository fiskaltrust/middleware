using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.AcceptanceTests
{
    public class CustomRTPrinterTests : ITSSCDTests
    {
        private static readonly Uri _serverUri = new Uri("http://10.3.16.11");
        private readonly CustomRTPrinterConfiguration _config = new CustomRTPrinterConfiguration
        {
            DeviceUrl = _serverUri.ToString(),
            Username = "STITE350116",
            Password = "STITE350116",
            ClientTimeoutMs = 60 * 1000,
            ServerTimeoutMs = 60 * 1000
        };

        protected override string SerialNumber => "STITE350116";

        protected override IMiddlewareBootstrapper GetMiddlewareBootstrapper(Guid queueId) => new ScuBootstrapper
        {
            Id = queueId,
            Configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(_config))
        };
    }
}