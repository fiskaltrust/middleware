using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class ftReceiptCaseDataPayload
{
    [JsonPropertyName("GR")]
    public ftReceiptCaseDataGreekPayload? GR { get; set; }
}

public class ftReceiptCaseDataGreekPayload
{
    public string? MerchantVATID { get; set; }
    public string? Series { get; set; }
    public long? AA { get; set; }
    public string? HashAlg { get; set; }
    public string? HashPayload { get; set; }

    [JsonPropertyName("mydataoverride")]
    public ReceiptRequestMyDataOverride? MyDataOverride { get; set; }
}

public class ftChargeItemCaseDataPayload
{
    [JsonPropertyName("GR")]
    public ftChargeItemCaseDataGreekPayload? GR { get; set; }
}

public class ftChargeItemCaseDataGreekPayload
{
    [JsonPropertyName("mydataoverride")]
    public ChargeItemMyDataOverride? MyDataOverride { get; set; }
}
