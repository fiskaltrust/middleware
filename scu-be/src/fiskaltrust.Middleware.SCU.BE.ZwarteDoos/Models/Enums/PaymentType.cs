using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

/// <summary>
/// The type of the payment method.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentType
{
    UNKNOWN,
    CASH,
    CARD_UNKNOWN,
    CARD_DEBIT,
    CARD_CREDIT,
    CARD_OTHER,
    CHEQUE_MEAL,
    CHEQUE_OTHER,
    APP,
    ONLINE,
    CUSTOMER_CREDIT,
    ROOM_CREDIT,
    LOYALTY_REWARDS,
    VOUCHER_STORE,
    VOUCHER_SUPPLIER,
    VOUCHER_OTHER,
    OTHER
}
