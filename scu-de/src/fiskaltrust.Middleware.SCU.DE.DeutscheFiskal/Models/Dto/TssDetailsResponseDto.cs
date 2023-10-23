﻿using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Models
{
    public class TssDetailsResponseDto
    {
        [JsonProperty("serialNumberHex")]
        public string SerialNumberHex { get; set; }

        [JsonProperty("algorithm")]
        public string Algorithm { get; set; }

        [JsonProperty("timeFormat")]
        public string TimeFormat { get; set; }

        [JsonProperty("publicKey")]
        public string PublicKey { get; set; }

        [JsonProperty("leafCertificate")]
        public string LeafCertificate { get; set; }
    }
}
