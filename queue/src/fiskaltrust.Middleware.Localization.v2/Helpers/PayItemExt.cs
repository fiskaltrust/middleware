using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.v2.Helpers;

public static class PayItemExt
{
    public static bool IsVoid(this PayItem payItem) => payItem.ftPayItemCase.IsFlag(PayItemCaseFlags.Void);
    public static bool IsRefund(this PayItem payItem) => payItem.ftPayItemCase.IsFlag(PayItemCaseFlags.Refund);
}