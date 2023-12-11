using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.AcceptanceTests
{
    public class EpsonRTPrinterTests : ITSSCDTests
    {
        private static readonly Uri _serverUri = new Uri("http://192.168.0.14/");
        private readonly EpsonRTPrinterSCUConfiguration _config = new EpsonRTPrinterSCUConfiguration
        {
            DeviceUrl = _serverUri.ToString(),
            Password = "21719",
            AdditionalTrailerLines = "[\"T.{cbArea} K.{cbUser}\",\"\"]"
        };

        protected override string SerialNumber => "99IEC018305";

        protected override IMiddlewareBootstrapper GetMiddlewareBootstrapper(Guid queueId) => new ScuBootstrapper
        {
            Id = queueId,
            Configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(_config))
        };

        [Fact]
        public void Deserialize()
        {
            var json = JsonConvert.SerializeObject(_config);

            var data = """
                {"DeviceUrl":"http://10.1.16.110","AdditionalTrailerLines":"[\\\"T.{cbArea} K.{cbUser}\\\",\\\"\\\"]"}
                """;
            var config = JsonConvert.DeserializeObject<EpsonRTPrinterSCUConfiguration>(data);
        }
    }
}