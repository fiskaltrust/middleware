using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventOperation
{
    WORK_IN,
    WORK_OUT,
    SALE,
    INVOICE,
    COST_CENTER_CHANGE,
    ORDER,
    PRE_BILL,
    MONEY_IN_OUT,
    DRAWER_OPEN,
    PAYMENT_CORRECTION,
    COPY,
    REPORT_TURNOVER_X,
    REPORT_TURNOVER_Z,
    REPORT_USER_X,
    REPORT_USER_Z
}
