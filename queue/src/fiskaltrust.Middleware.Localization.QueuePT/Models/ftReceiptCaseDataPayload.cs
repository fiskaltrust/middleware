using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Models;

public class ftReceiptCaseDataPayload
{
    [JsonPropertyName("PT")]
    public ftReceiptCaseDataPortugalPayload? PT { get; set; }
}

public class ftReceiptCaseDataPortugalPayload
{
    public string? Series { get; set; }
    public long? Number { get; set; }
}
