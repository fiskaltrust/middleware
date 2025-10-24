using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CostCenterType
{
    TABLE,
    CHAIR,
    ROOM,
    CUSTOMER,
    ON_HOLD,
    KIOSK,
    PLATFORM,
    WEBSHOP,
    OTHER
}
