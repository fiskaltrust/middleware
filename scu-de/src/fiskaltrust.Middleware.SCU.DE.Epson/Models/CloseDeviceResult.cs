using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.Epson.ResultModels
{
    public class CloseDeviceResult
    {
        [JsonProperty("device_id")]
        public string DeviceId { get; set; }

        public string Code { get; set; }
    }
}
