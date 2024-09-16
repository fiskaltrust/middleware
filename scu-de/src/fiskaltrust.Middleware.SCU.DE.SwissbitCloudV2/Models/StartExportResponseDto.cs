using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models
{
    public class StartExportResponseDto
    {
        [JsonProperty("id")]
        public string ExportId { get; set; }
    }
}
