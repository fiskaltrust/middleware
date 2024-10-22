using fiskaltrust.Api.POS.Models.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.v2.Helpers;

public static class ChargeItemExt
{
    public static decimal GetVATAmount(this ChargeItem chargeItem) => chargeItem.VATAmount ?? (chargeItem.Amount * chargeItem.VATRate);
}