#pragma warning disable
using fiskaltrust;
using fiskaltrust.Middleware;
using fiskaltrust.Middleware.Localization;
using fiskaltrust.Middleware.Localization.QueueGR;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE.Models;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.myDataSCU;

namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE.Models;

public class MyDataVatCategory
{
    public const int VatRate24 = 1;  // 24% VAT
    public const int VatRate13 = 2;  // 13% VAT
    public const int VatRate6 = 3;   // 6% VAT
    public const int VatRate17 = 4;  // 17% VAT
    public const int VatRate9 = 5;   // 9% VAT
    public const int VatRate4 = 6;   // 4% VAT
    public const int ExcludingVat = 7;  // Excluding VAT 0%
    public const int RegistrationsWithoutVat = 8;  // Registrations without VAT
    public const int VatRate3 = 9;   // 3% VAT (ν.5057/2023)
    public const int VatRate4New = 10;  // 4% VAT (ν.5057/2023)
}
