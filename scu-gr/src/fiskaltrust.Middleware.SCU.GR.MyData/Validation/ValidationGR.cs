using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using System;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueGR.Validation;

public class ValidationGR
{
    public static (bool, MiddlewareValidationError? middlewareValidationError) ValidateReceiptRequest(ReceiptRequest receiptRequest)
    {
        // A caller-supplied VATAmount is sent to AADE as-is, while the VAT category is derived from
        // ftChargeItemCase/VATRate. A VATAmount of 0 on an item with a non-zero VATRate would be transmitted
        // (and accepted by AADE) as e.g. category 1 with no VAT.
        var zeroVatItem = receiptRequest.cbChargeItems.FirstOrDefault(x => x.VATRate != 0
            && x.VATAmount == 0
            // By now VATAmount is always set: MapToInvoicesDoc fills in VATAmount = Math.Round(VATAmount ?? calculated, 2),
            // which keeps a caller-supplied 0 and only calculates when it was null. A 0 here is therefore either
            // caller-supplied or a calculated VAT that rounded down to 0.00 (e.g. a free item, or 0.01 at 6%).
            // Only reject when the expected VAT is not 0.00, i.e. when the 0 must have come from the caller.
            && Math.Round(x.Amount / (100 + x.VATRate) * x.VATRate, 2) != 0);
        if (zeroVatItem != null)
        {
            return (false, new MiddlewareValidationError("ZeroVatAmountWithNonZeroVatRate", FormattableString.Invariant($"VATAmount of charge item at position {zeroVatItem.Position} is 0 although VATRate is {zeroVatItem.VATRate}. Either provide the correct VATAmount (or omit it to have it calculated) or use a zero VAT rate with the matching VAT case.")));
        }

        if (receiptRequest.cbChargeItems.Any(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.NotOwnSales))
            && receiptRequest.cbChargeItems.Any(x => x.ftChargeItemCase.TypeOfService() != ChargeItemCaseTypeOfService.NotOwnSales && x.ftChargeItemCase.TypeOfService() != (ChargeItemCaseTypeOfService) 0xF0))
        {
            return (false, new MiddlewareValidationError("ChargeItemTypeNotSupported", "All charge items must be of type 'NotOwnSales' for this receipt type."));
        }

        if (receiptRequest.cbChargeItems.Any(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.OwnConsumption))
            && receiptRequest.cbChargeItems.Any(x => x.ftChargeItemCase.TypeOfService() != ChargeItemCaseTypeOfService.OwnConsumption && x.ftChargeItemCase.TypeOfService() != (ChargeItemCaseTypeOfService) 0xF0))
        {
            return (false, new MiddlewareValidationError("ChargeItemTypeNotSupported", "All charge items must be of type 'OwnConsumption' for this receipt type."));
        }

        if (!receiptRequest.ftReceiptCase.IsType(ReceiptCaseType.Log) && !receiptRequest.ftReceiptCase.IsCase(ReceiptCase.DeliveryNote0x0005) && receiptRequest.cbChargeItems.Sum(x => x.Amount) != receiptRequest.cbPayItems.Sum(x => x.Amount))
        {
            return (false, new MiddlewareValidationError("ChargePayItemsMismatch", "The sum of the charge items must be equal to the sum of the pay items."));
        }

        if (AADEMappings.RequiresCustomerInfo(AADEMappings.GetInvoiceType(receiptRequest)) && !receiptRequest.ContainsCustomerInfo())
        {
            return (false, new MiddlewareValidationError("CustomerInfoRequired", "Customer info is required for this invoice type."));
        }

        return (true, null);
    }
}
