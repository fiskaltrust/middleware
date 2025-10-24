using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;

public class SignSaleData : SignOrderData
{
    public string ShortSignature { get; set; } = null!;
    public string VerificationUrl { get; set; } = null!;
    public List<VatCalc> VatCalc { get; set; } = [];
}