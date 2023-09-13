using fiskaltrust.Middleware.Abstractions;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.UnitTest
{
    public class EpsonRTPrinterTests : ITSSCDTests
    {
        private static readonly Uri _serverUri = new Uri("http://192.168.0.34/");
        private readonly EpsonRTPrinterSCUConfiguration _config = new EpsonRTPrinterSCUConfiguration
        {
            DeviceUrl = _serverUri.ToString()
        };

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