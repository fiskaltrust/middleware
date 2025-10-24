using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Category
{
    SPF_FOD,
    FDM,
    OTHER
}
