using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.Epson.ResultModels
{
    public class OutputGetExportDataResult
    {
        [JsonProperty("exportData")]
        public string ExportData { get; set; }

        [JsonProperty("exportStatus")]
        public string ExportStatus { get; set; }
    }
}

