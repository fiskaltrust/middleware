using System.Globalization;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Helpers;

public static class ChargeItemHelper
{
    public static decimal GetVATAmount(this ChargeItem self) => self.VATAmount ?? (self.Amount / (100 + self.VATRate) * self.VATRate);
}