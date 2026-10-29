using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Models;

public class ftChargeItemCaseDataPayload
{
    [JsonPropertyName("PT")]
    public ftChargeItemCaseDataPortugalPayload? PT { get; set; }
}

public class ftChargeItemCaseDataPortugalPayload
{
    /// <summary>
    /// Portuguese tax exemption code (e.g. "M06"). Takes precedence over the code encoded in the nature of VAT (NN) of ftChargeItemCase.
    /// </summary>
    public string? TaxExemptionCode { get; set; }
}
