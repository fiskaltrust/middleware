using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueGR.Validation;

public class ValidationGR
{
    public static (bool, MiddlewareValidationError? middlewareValidationError) ValidateReceiptRequest(ReceiptRequest receiptRequest)
    {
        // A caller-supplied VATAmount is sent to AADE as-is, while the VAT category is derived from
        // ftChargeItemCase/VATRate. A VATAmount of 0 on an item with a non-zero VATRate would be transmitted
        // (and accepted by AADE) as e.g. category 1 with no VAT. Items whose VAT legitimately rounds to 0.00
        // (zero amount, or sub-cent VAT) are allowed.
        var zeroVatItem = receiptRequest.cbChargeItems.FirstOrDefault(x => x.VATRate != 0
            && x.VATAmount == 0
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

    private static readonly Dictionary<int, decimal> VatRateByCategory = new()
    {
        { MyDataVatCategory.VatRate24_Category1, 24m },
        { MyDataVatCategory.VatRate13_Category2, 13m },
        { MyDataVatCategory.VatRate6_Category3, 6m },
        { MyDataVatCategory.VatRate17_Category4, 17m },
        { MyDataVatCategory.VatRate9_Category5, 9m },
        { MyDataVatCategory.VatRate4_Category6, 4m },
        { MyDataVatCategory.VatRate3_Category9, 3m },
        { MyDataVatCategory.VatRate4_Category10, 4m },
    };

    /// <summary>
    /// Final-row counterpart of the charge item check in <see cref="ValidateReceiptRequest"/>: runs on the
    /// generated invoice rows after line-level mydataoverride has been applied, so an override cannot emit
    /// a row with a non-zero-rate VAT category and vatAmount 0. Rows whose VAT legitimately rounds to 0.00
    /// (e.g. netValue 0) are allowed.
    /// </summary>
    public static (bool, MiddlewareValidationError? middlewareValidationError) ValidateInvoiceDetails(IEnumerable<InvoiceRowType> invoiceDetails)
    {
        foreach (var row in invoiceDetails)
        {
            if (row.vatAmount == 0
                && VatRateByCategory.TryGetValue(row.vatCategory, out var vatRate)
                && Math.Round(row.netValue * vatRate / 100, 2) != 0)
            {
                return (false, new MiddlewareValidationError("ZeroVatAmountWithNonZeroVatCategory", FormattableString.Invariant($"Invoice row {row.lineNumber} has vatAmount 0 although vatCategory {row.vatCategory} ({vatRate}%) applies to netValue {row.netValue}. Check the VATAmount and any mydataoverride.invoiceDetails.vatAmount/vatCategory of the charge item.")));
            }
        }

        return (true, null);
    }
}
