using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

/// <summary>
/// The label of the VAT rate that the totals apply to.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VatLabel
{
    // The high VAT rate. This is currently 21%.
    A,
    // The middle VAT rate. This is currently 12%.
    B,
    // The low VAT rate. This is currently 6%.
    C,
    // The zero VAT rate. This should remain at 0%.
    D,
    // Indicates out of scope of VAT.
    X
}
