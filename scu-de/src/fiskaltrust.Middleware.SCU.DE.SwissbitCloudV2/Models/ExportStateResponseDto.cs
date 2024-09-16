using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models
{
    public class ExportStateResponseDto
    {
        /* 
            "pending""success""failure"
         */
        [JsonProperty("status")]
        public string State { get; set; }

        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }

    }
}
