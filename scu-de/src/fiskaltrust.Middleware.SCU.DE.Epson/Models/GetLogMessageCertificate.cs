using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.Epson.Commands
{
    public class GetLogMessageCertificate
    {
        [JsonProperty("logMessageCertificate")]
        public string LogMessageCertificateBase64 { get; set; }
    }
}