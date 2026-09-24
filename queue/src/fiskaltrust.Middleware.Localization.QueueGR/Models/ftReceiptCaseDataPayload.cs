using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueGR.Models;

/// <summary>
/// Minimal projection of the GR ftReceiptCaseData payload — only the fields the queue
/// needs to take caller-supplied numbering (handwritten documents) inbound. The payload
/// is an open extension point: unknown fields are ignored, and full validation of the
/// handwritten payload stays in the SCU (AADEFactory).
/// </summary>
public class ftReceiptCaseDataPayload
{
    [JsonPropertyName("GR")]
    public ftReceiptCaseDataGreekPayload? GR { get; set; }
}

public class ftReceiptCaseDataGreekPayload
{
    public string? Series { get; set; }

    public long? AA { get; set; }
}
