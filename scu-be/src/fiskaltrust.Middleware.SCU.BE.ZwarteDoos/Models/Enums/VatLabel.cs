using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

/// <summary>
/// The label of the VAT rate that the totals apply to.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VatLabel
{
    A, // 21%
    B, // 12%
    C, // 6%
    D  // 0%
}
