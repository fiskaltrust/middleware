using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models
{
    public class ClientDto
    {
        [JsonProperty("clientId")]
        public string ClientId { get; set; }
    }
}
