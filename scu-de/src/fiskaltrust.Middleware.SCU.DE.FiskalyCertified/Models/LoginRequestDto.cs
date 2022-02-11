using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models
{
    public class LoginRequestDto
    {
        [JsonProperty("admin_pin")]
        public string AdminPin { get; set; }
    }
}
