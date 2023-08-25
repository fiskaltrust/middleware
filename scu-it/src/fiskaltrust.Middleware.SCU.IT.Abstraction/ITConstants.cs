using System.Globalization;

namespace fiskaltrust.Middleware.SCU.IT.Abstraction;

public static class ITConstants
{
    public static NumberFormatInfo CurrencyFormatter = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = "",
        CurrencyDecimalDigits = 2
    };

    public static long ConvertToV2Case(long legacyReceiptcase)
    {
        var value = legacyReceiptcase switch
        {
            0x002 => ITReceiptCases.ZeroReceipt0x200,
            0x003 => ITReceiptCases.InitialOperationReceipt0x4001,
            0x004 => ITReceiptCases.OutOfOperationReceipt0x4002,
            0x005 => ITReceiptCases.MonthlyClosing0x2012,
            0x006 => ITReceiptCases.YearlyClosing0x2013,
            0x007 => ITReceiptCases.DailyClosing0x2011,
            0x000 => ITReceiptCases.UnknownReceipt0x0000,
            0x001 => ITReceiptCases.PointOfSaleReceipt0x0001,
            _ => ITReceiptCases.UnknownReceipt0x0000
        };
        return (long) value;
    }
}
