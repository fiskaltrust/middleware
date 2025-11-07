using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;

public class PaymentLineInput
{
    /// <summary>
    /// The language independent and case sensitive id of the payment method. A maximum of 600 characters is allowed with a minimum of 1 character. Leading or trailing whitespace characters are not allowed in the field.
    /// This is not an identification of an individual payment. All payments using the same method must share the same id.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// # The name of the payment method (for example "Cash", "VISA", "Credit card", etc.). A maximum of 600 characters is allowed with a minimum of 1 character. Leading or trailing whitespace characters are not allowed in the field.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The type of the payment method.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required PaymentType Type { get; set; }

    /// <summary>
    /// Specifies the provider of the payment method (for example "Visa", "MasterCard", "Edenred", etc.). A maximum of 600 characters is allowed with a minimum of 1 character. Leading or trailing whitespace characters are not allowed in the field.
    /// </summary>
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    /// <summary>
    /// Specifies how the payment was entered in the system.
    /// </summary>
    [JsonPropertyName("inputMethod")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required InputMethod InputMethod { get; set; }

    /// <summary>
    /// The amount of the payment. Always in Euro even if the amount was received in a foreign currency.
    /// </summary>
    [JsonPropertyName("amount")]
    public required decimal Amount { get; set; }

    /// <summary>
    /// Specifies the nature of the payment.
    /// </summary>
    [JsonPropertyName("amountType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required PaymentLineType AmountType { get; set; }

    /// <summary>
    /// For payments received in a foreign currency the ISO code of the currency and the amount of the payment expressed in the currency must be specified.
    /// </summary>
    [JsonPropertyName("foreignCurrency")]
    public ForeignCurrencyInput? ForeignCurrency { get; set; }

    /// <summary>
    /// A reference for the payment. This can be information about the payment received from an EFT terminal, an online application, a cash automation machine, etc. or other reference in regards to the payment. A maximum of 600 characters is allowed with a minimum of 1 character. Leading or trailing whitespace characters are not allowed in the field.
    /// </summary>
    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    /// <summary>
    /// When the POS system supports multiple drawers or purses, specifies the drawer or purse used for the payment.
    /// </summary>
    [JsonPropertyName("drawer")]
    public DrawerInput? Drawer { get; set; }
}
