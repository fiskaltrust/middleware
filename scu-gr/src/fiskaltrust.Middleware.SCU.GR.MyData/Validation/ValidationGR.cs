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
    private const decimal VatAmountRoundingTolerance = 0.01m;

    public static (bool, MiddlewareValidationError? middlewareValidationError) ValidateReceiptRequest(ReceiptRequest receiptRequest)
    {
        // A caller-supplied VATAmount is sent to AADE as-is, while the VAT category is derived from
        // ftChargeItemCase/VATRate. Without this check a VATAmount of 0 on a 24% item is transmitted
        // (and accepted by AADE) as category 1 with no VAT. Mirrors the queue's global VatCalculation rule.
        // Special tax items (fees, withholdings, stamp duty, ...) carry their own VAT semantics and are excluded.
        if (!receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
        {
            var mismatch = receiptRequest.cbChargeItems.FirstOrDefault(x => x.VATAmount.HasValue && x.VATRate > 0
                && !SpecialTaxMappings.IsSpecialTaxItem(x)
                && Math.Abs(x.VATAmount.Value - x.Amount / (100 + x.VATRate) * x.VATRate) > VatAmountRoundingTolerance);
            if (mismatch != null)
            {
                var calculated = mismatch.Amount / (100 + mismatch.VATRate) * mismatch.VATRate;
                return (false, new MiddlewareValidationError("VatAmountMismatch", FormattableString.Invariant($"VATAmount ({mismatch.VATAmount}) of charge item at position {mismatch.Position} does not match calculated value ({calculated:F2}) for VATRate {mismatch.VATRate}, difference exceeds tolerance of {VatAmountRoundingTolerance}.")));
            }
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
