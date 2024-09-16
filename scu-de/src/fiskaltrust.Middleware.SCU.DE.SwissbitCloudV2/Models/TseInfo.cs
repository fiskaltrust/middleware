using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models
{
    public class TseInfo
    {
        [JsonProperty("serialNumber")]
        public string SerialNumber { get; set; }

        [JsonProperty("healthState")]
        public TseHealthState HealthState { get; set; }

        [JsonProperty("initializationState")]
        public TseInitializationState InitializationState { get; set; }

    }

}
