using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;

public class PaymentLineInput
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required PaymentType Type { get; set; }

    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    [JsonPropertyName("inputMethod")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required InputMethod InputMethod { get; set; }

    [JsonPropertyName("amount")]
    public required decimal Amount { get; set; }

    [JsonPropertyName("amountType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required PaymentLineType AmountType { get; set; }

    [JsonPropertyName("foreignCurrency")]
    public ForeignCurrencyInput? ForeignCurrency { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("drawer")]
    public DrawerInput? Drawer { get; set; }
}
