using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionLineType
{
    // The transaction line pertains to a single product. Only mainProduct will be provided.
    SINGLE_PRODUCT,
    // The transaction line pertains to a composite product that is regarded as a single good or service. Both main product and sub products will be provided but only the vats arrays of the sub products are populated. The vats array of the main product must be empty.
    COMPOSITE_PRODUCT
}
