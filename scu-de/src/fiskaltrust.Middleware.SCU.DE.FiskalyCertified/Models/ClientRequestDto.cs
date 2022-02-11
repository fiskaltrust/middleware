using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models
{
    public class ClientRequestDto
    {
        [JsonProperty("serial_number")]
        public string SerialNumber { get; set; }
    }
}
