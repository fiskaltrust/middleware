using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData;

public class MyDataAADEValidation
{
    public static void ValidateReceiptRequest(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.cbChargeItems.Any(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.NotOwnSales)) && !receiptRequest.cbChargeItems.All(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.NotOwnSales)))
        {
            throw new Exception("It is not allowed to mix agency and non agency receipts.");
        }

        if (!receiptRequest.ftReceiptCase.IsType(ReceiptCaseType.Log) && receiptRequest.cbChargeItems.Sum(x => x.Amount) != receiptRequest.cbPayItems.Sum(x => x.Amount))
        {
            throw new Exception("The sum of the charge items must be equal to the sum of the pay items.");
        }

        if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            throw new Exception("The Voiding of documents is not supported. Please use refund.");
        }

        if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsGR.IsSelfPricingOperation))
        {
            throw new Exception("SelfPricing is not supported.");
        }

        if (AADEMappings.RequiresCustomerInfo(AADEMappings.GetInvoiceType(receiptRequest)) && !receiptRequest.ContainsCustomerInfo())
        {
            throw new Exception("Customer info is required for this invoice type");
        }
    }
}
