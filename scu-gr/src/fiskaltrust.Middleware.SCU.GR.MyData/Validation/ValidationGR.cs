using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueGR.Validation;

public class ValidationGR
{
    public static (bool, MiddlewareValidationError? middlewareValidationError) ValidateReceiptRequest(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.cbChargeItems.Any(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.NotOwnSales)) && !receiptRequest.cbChargeItems.All(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.NotOwnSales)))
        {
            return (false, new MiddlewareValidationError("ChargeItemTypeNotSupported", "All charge items must be of type 'NotOwnSales' for this receipt type."));
        }

        if (!receiptRequest.ftReceiptCase.IsType(ReceiptCaseType.Log) && receiptRequest.cbChargeItems.Sum(x => x.Amount) != receiptRequest.cbPayItems.Sum(x => x.Amount))
        {
            return (false, new MiddlewareValidationError("ChargePayItemsMismatch", "The sum of the charge items must be equal to the sum of the pay items."));
        }

        if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            return (false, new MiddlewareValidationError("VoidNotSupported", "Voiding of documents is not supported. Please use refund."));
        }

        if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsGR.IsSelfPricingOperation))
        {
            return (false, new MiddlewareValidationError("SelfPricingNotSupported", "Self-pricing operations are not supported."));
        }

        if (AADEMappings.RequiresCustomerInfo(AADEMappings.GetInvoiceType(receiptRequest)) && !receiptRequest.ContainsCustomerInfo())
        {
            return (false, new MiddlewareValidationError("CustomerInfoRequired", "Customer info is required for this invoice type."));
        }

        return (true, null);
    }
}
