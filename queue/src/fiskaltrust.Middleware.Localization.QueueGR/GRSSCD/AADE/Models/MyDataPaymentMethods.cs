#pragma warning disable
using fiskaltrust;
using fiskaltrust.Middleware;
using fiskaltrust.Middleware.Localization;
using fiskaltrust.Middleware.Localization.QueueGR;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE.Models;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.myDataSCU;

namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE.Models;

public class MyDataPaymentMethods
{
    public const int DomesticPaymentAccount = 1;
    public const int ForeignPaymentsSpecialAccount = 2;
    public const int Cash = 3;
    public const int Cheque = 4;
    public const int OnCredit = 5;
    public const int WebBanking = 6;
    public const int PosEPos = 7;
    public const int IrisDirectPayments = 8;
}
