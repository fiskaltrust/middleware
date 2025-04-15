using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueGR.Extensions;

public static class ChargeItemExtensions
{
    public static bool IsVoid(this ChargeItem chargeItem) => chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Void);

    public static bool IsVoucherRedeem(this ChargeItem ci) => ci.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.Voucher) && ((!ci.IsVoid() && !ci.IsRefund() && ci.Amount < 0) || ((ci.IsVoid() || ci.IsRefund()) && ci.Amount > 0));

    public static bool IsDiscountOrExtra(this ChargeItem ci) => ci.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.ExtraOrDiscount);

    public static bool IsDiscount(this ChargeItem ci) => ci.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.ExtraOrDiscount) && ((!ci.IsVoid() && !ci.IsRefund() && ci.Amount < 0) || ((ci.IsVoid() || ci.IsRefund()) && ci.Amount > 0));

    public static bool IsExtra(this ChargeItem ci) => ci.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.ExtraOrDiscount) && ((!ci.IsVoid() && !ci.IsRefund() && ci.Amount > 0) || ((ci.IsVoid() || ci.IsRefund()) && ci.Amount < 0));

    public static bool IsRefund(this ChargeItem chargeItem) => chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund);
}