using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using Azure.Core;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using fiskaltrust.Middleware.Localization.QueueGR.Validation;
using System.IO;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class AADEFactoryError
{
    public Exception Exception { get; set; } = null!;
}

public class AADEFactory
{
    private const string VIVA_FISCAL_PROVIDER_ID = "126";

    private readonly MasterDataConfiguration _masterDataConfiguration;
    private readonly string? _receiptBaseAddress;

    public AADEFactory(MasterDataConfiguration masterDataConfiguration, string? receiptBaseAddress = null)
    {
        _masterDataConfiguration = masterDataConfiguration;
        _receiptBaseAddress = receiptBaseAddress;
    }

    public InvoicesDoc LoadInvoiceDocsFromQueueItems(List<ftQueueItem> queueItems)
    {
        var receiptRequests = queueItems.Where(x => !string.IsNullOrEmpty(x.request) && !string.IsNullOrEmpty(x.response)).Select(x => (receiptRequest: JsonSerializer.Deserialize<ReceiptRequest>(x.request)!, receiptResponse: JsonSerializer.Deserialize<ReceiptResponse>(x.response))).ToList();
        var actualReceiptRequests = receiptRequests.Where(x => x.receiptResponse != null && ((long) x.receiptResponse.ftState & 0xFF) == 0x00).Cast<(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)>().ToList();
        actualReceiptRequests = actualReceiptRequests.Where(x =>
        {
            var mark = x.receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "invoiceMark")?.Data;
            if (mark == null)
            {
                return false;
            }

            try
            {
                AADEMappings.GetInvoiceType(x.receiptRequest);
                return true;
            }
            catch
            {
                return false;
            }
        }).ToList();
        var doc = new InvoicesDoc
        {
            invoice = actualReceiptRequests.Select(x => CreateInvoiceDocType(x.receiptRequest, x.receiptResponse)).ToArray()
        };
        var invoices = new List<AadeBookInvoiceType>();
        foreach (var receipt in actualReceiptRequests)
        {
            var inv = CreateInvoiceDocType(receipt.receiptRequest, receipt.receiptResponse);
            if (receipt.receiptResponse.ftSignatures.Count > 0)
            {
                var invoiceUid = receipt.receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "invoiceUid")?.Data;
                var invoiceMarkText = receipt.receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "invoiceMark")?.Data;
                var authenticationCode = receipt.receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "authenticationCode")?.Data;
                var qrCode = receipt.receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "authenticationCode")?.Data;
                if (long.TryParse(invoiceMarkText, out var invoiceMark))
                {
                    inv.uid = invoiceUid;
                    inv.authenticationCode = authenticationCode;
                    inv.mark = invoiceMark;
                    inv.markSpecified = true;
                }
                else
                {
                    invoiceMark = -1;
                }

                if (receipt.receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.LateSigning))
                {
                    inv.transmissionFailureSpecified = true;
                    inv.transmissionFailure = 1;
                }
                var transmissionFailure1 = receipt.receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "Transmission Failure_1")?.Data;
                if (transmissionFailure1 != null)
                {

                }

                var transmissionFailure2 = receipt.receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "Transmission Failure_2")?.Data;
                if (transmissionFailure2 != null)
                {
                    inv.transmissionFailureSpecified = true;
                    inv.transmissionFailure = 2;
                }
            }
        }
        return doc;
    }

    public (InvoicesDoc? invoiceDoc, AADEFactoryError? error) MapToInvoicesDoc(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, List<(ReceiptRequest, ReceiptResponse)>? receiptReferences = null)
    {
        try
        {
            foreach (var chargeItem in receiptRequest.cbChargeItems)
            {
                chargeItem.Amount = Math.Round(chargeItem.Amount, 2);
                chargeItem.VATAmount = Math.Round(chargeItem.GetVATAmount(), 2);
                chargeItem.Quantity = Math.Round(chargeItem.Quantity, 2);
            }

            foreach (var payItem in receiptRequest.cbPayItems)
            {
                payItem.Amount = Math.Round(payItem.Amount, 2);
                payItem.Quantity = Math.Round(payItem.Quantity, 2);
            }
            (var valid, var validationError) = ValidationGR.ValidateReceiptRequest(receiptRequest);
            if (!valid)
            {
                throw new Exception(validationError?.ErrorMessage ?? "Invalid receipt request.");
            }
            var inv = CreateInvoiceDocType(receiptRequest, receiptResponse, receiptReferences);
            var doc = new InvoicesDoc
            {
                invoice = [inv]
            };
            return (doc, null);
        }
        catch (Exception ex)
        {
            return (null, new AADEFactoryError
            {
                Exception = ex
            });
        }
    }

    private AadeBookInvoiceType CreateInvoiceDocType(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, List<(ReceiptRequest, ReceiptResponse)>? receiptReferences = null)
    {
        var invoiceDetails = GetInvoiceDetails(receiptRequest);
        var documentLevelTaxes = GetDocumentLevelTaxes(receiptRequest);

        var incomeClassificationGroups = invoiceDetails.Where(x => x.incomeClassification != null).SelectMany(x => x.incomeClassification).Where(x => x.classificationTypeSpecified).GroupBy(x => (x.classificationCategory, x.classificationType)).Select(x => new IncomeClassificationType
        {
            amount = x.Sum(y => y.amount),
            classificationCategory = x.Key.classificationCategory,
            classificationType = x.Key.classificationType,
            classificationTypeSpecified = true
        }).ToList();
        incomeClassificationGroups.AddRange(invoiceDetails.Where(x => x.incomeClassification != null).SelectMany(x => x.incomeClassification).Where(x => !x.classificationTypeSpecified).GroupBy(x => x.classificationCategory).Select(x => new IncomeClassificationType
        {
            amount = x.Sum(y => y.amount),
            classificationCategory = x.Key,
        }).ToList());

        var expensesClassificationGroups = invoiceDetails.Where(x => x.expensesClassification != null).SelectMany(x => x.expensesClassification).Where(x => x.classificationTypeSpecified & x.classificationCategorySpecified).GroupBy(x => (x.classificationCategory, x.classificationType)).Select(x => new ExpensesClassificationType
        {
            amount = x.Sum(y => y.amount),
            classificationCategory = x.Key.classificationCategory,
            classificationCategorySpecified = true,
            classificationType = x.Key.classificationType,
            classificationTypeSpecified = true
        }).ToList();
        expensesClassificationGroups.AddRange(invoiceDetails.Where(x => x.expensesClassification != null).SelectMany(x => x.expensesClassification).Where(x => !x.classificationTypeSpecified && x.classificationCategorySpecified).GroupBy(x => x.classificationCategory).Select(x => new ExpensesClassificationType
        {
            amount = x.Sum(y => y.amount),
            classificationCategorySpecified = true,
            classificationCategory = x.Key,
        }).ToList());
        expensesClassificationGroups.AddRange(invoiceDetails.Where(x => x.expensesClassification != null).SelectMany(x => x.expensesClassification).Where(x => x.classificationTypeSpecified && !x.classificationCategorySpecified).GroupBy(x => x.classificationType).Select(x => new ExpensesClassificationType
        {
            amount = x.Sum(y => y.amount),
            classificationTypeSpecified = true,
            classificationType = x.Key,
        }).ToList());

        var identification = long.Parse(receiptResponse.ftReceiptIdentification.Replace("ft", "").Split("#")[0], System.Globalization.NumberStyles.HexNumber);
        var paymentMethods = GetPayments(receiptRequest);
        var issuer = CreateIssuer(receiptRequest);
        var inv = new AadeBookInvoiceType
        {
            issuer = issuer,
            invoiceHeader = new InvoiceHeaderType
            {
                series = receiptResponse.ftCashBoxIdentification,
                aa = identification.ToString(),
                issueDate = receiptRequest.cbReceiptMoment,
                invoiceType = AADEMappings.GetInvoiceType(receiptRequest),
                currency = CurrencyType.EUR,
                currencySpecified = true
            },
            invoiceDetails = [.. invoiceDetails],
            invoiceSummary = new InvoiceSummaryType
            {
                totalNetValue = invoiceDetails.Sum(x => x.netValue),
                totalVatAmount = invoiceDetails.Sum(x => x.vatAmount),
                totalWithheldAmount = documentLevelTaxes.Where(x => x.taxType == MyDataTaxCategories.WithHeldTaxes).Sum(x => x.taxAmount) + invoiceDetails.Sum(x => x.withheldAmount),
                totalFeesAmount = documentLevelTaxes.Where(x => x.taxType == MyDataTaxCategories.Fees).Sum(x => x.taxAmount) + invoiceDetails.Sum(x => x.feesAmount),
                totalStampDutyAmount = documentLevelTaxes.Where(x => x.taxType == MyDataTaxCategories.StampDuty).Sum(x => x.taxAmount) + invoiceDetails.Sum(x => x.stampDutyAmount),
                totalOtherTaxesAmount = documentLevelTaxes.Where(x => x.taxType == MyDataTaxCategories.OtherTaxes).Sum(x => x.taxAmount) + invoiceDetails.Sum(x => x.otherTaxesAmount),
                totalDeductionsAmount = documentLevelTaxes.Where(x => x.taxType == MyDataTaxCategories.Deduction).Sum(x => x.taxAmount) + invoiceDetails.Sum(x => x.deductionsAmount),
                incomeClassification = [.. incomeClassificationGroups],
                expensesClassification = [.. expensesClassificationGroups],
            }
        };

        if (inv.invoiceHeader.invoiceType == InvoiceType.Item93)
        {
            // It looks like Item93 does NOT allow to specify the currency
            inv.invoiceHeader.currencySpecified = false;
        }

        // Add withholding taxes to the invoice if any exist
        if (documentLevelTaxes.Count > 0)
        {
            inv.taxesTotals = documentLevelTaxes.ToArray();
        }
        if (paymentMethods?.Count > 0)
        {
            inv.paymentMethods = [.. paymentMethods];
        }

        if (receiptRequest.ContainsCustomerInfo() && (AADEMappings.SupportsCounterpart(inv.invoiceHeader.invoiceType) || receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsGR.HasTransportInformation)))
        {
            var counterpart = GetCounterPart(receiptRequest);
            if (counterpart != null)
            {
                inv.counterpart = counterpart;
            }
        }

        if (receiptRequest.cbPreviousReceiptReference is not null && receiptReferences?.Count > 0)
        {
            if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
            {
                if (AADEMappings.SupportsCorrelatedInvoices(inv.invoiceHeader.invoiceType))
                {
                    inv.invoiceHeader.correlatedInvoices = receiptReferences.Select(x => GetInvoiceMark(x.Item2)).ToArray();
                }
                else
                {
                    // Retail refunds (11.4) use multipleConnectedMarks
                    inv.invoiceHeader.multipleConnectedMarks = receiptReferences.Select(x => GetInvoiceMark(x.Item2)).ToArray();
                }
            }
            else
            {
                // NON-REFUNDS
                if (AADEMappings.SupportsMultipleConnectedMarks(inv.invoiceHeader.invoiceType))
                {
                    inv.invoiceHeader.multipleConnectedMarks = receiptReferences.Select(x => GetInvoiceMark(x.Item2)).ToArray();
                }
            }
        }

        if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.Order0x3004))
        {
            inv.invoiceHeader.tableAA = receiptRequest.cbArea?.ToString();
        }

        if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) && receiptRequest.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayload>(out var data))
        {
            if (string.IsNullOrEmpty(data?.GR?.Series))
            {
                throw new Exception("When using Handwritten receipts the Series must be provided in the ftReceiptCaseData payload.");
            }


            if (data?.GR?.AA == null || data?.GR?.AA == 0)
            {
                throw new Exception("When using Handwritten receipts the AA must be provided in the ftReceiptCaseData payload.");
            }

            if (string.IsNullOrEmpty(data?.GR?.MerchantVATID))
            {
                throw new Exception("When using Handwritten receipts the MerchantVATID must be provided in the ftReceiptCaseData payload.");
            }

            if (GetAADEVAT(data?.GR?.MerchantVATID) != GetAADEVAT(_masterDataConfiguration.Account.VatId))
            {
                throw new Exception("When using Handwritten receipts the MerchantVATID that is provided must match with the one configured in the Account.");
            }

            if (string.IsNullOrEmpty(data?.GR?.HashAlg))
            {
                throw new Exception("When using Handwritten receipts the HashAlg must be provided in the ftReceiptCaseData payload.");
            }

            if (string.IsNullOrEmpty(data?.GR?.HashPayload))
            {
                throw new Exception("When using Handwritten receipts the HashPayload must be provided in the ftReceiptCaseData payload.");
            }

            inv.invoiceHeader.series = data.GR.Series;
            inv.invoiceHeader.aa = data.GR.AA.ToString();

            var totalAmount = receiptRequest.cbReceiptAmount ?? receiptRequest.cbChargeItems.Sum(x => x.Amount);
            //  #utf8([MerchantVATID]-[Series]-[given-AA]-[cbReceiptReference]-[cbReceiptMoment]-[TotalAmount])    
            var hashPayloadExpected = data.GR.MerchantVATID + "-" + data.GR.Series + "-" + data.GR.AA + "-" + receiptRequest.cbReceiptReference + "-" + receiptRequest.cbReceiptMoment.ToString("yyyy-MM-ddTHH:mm:ssZ") + "-" + totalAmount;
            if (hashPayloadExpected != data.GR.HashPayload)
            {
                throw new Exception($"The HashPayload does not match the expected value. Expected: {hashPayloadExpected}, Actual: {data.GR.HashPayload}");
            }
        }

        inv.invoiceSummary.totalGrossValue = inv.invoiceSummary.totalNetValue + inv.invoiceSummary.totalVatAmount - inv.invoiceSummary.totalWithheldAmount + inv.invoiceSummary.totalFeesAmount + inv.invoiceSummary.totalStampDutyAmount + inv.invoiceSummary.totalOtherTaxesAmount - inv.invoiceSummary.totalDeductionsAmount;

        // Set downloadingInvoiceUrl if receiptBaseAddress is available
        if (!string.IsNullOrEmpty(_receiptBaseAddress) && receiptResponse.ftQueueID != Guid.Empty && receiptResponse.ftQueueItemID != Guid.Empty)
        {
            inv.downloadingInvoiceUrl = $"{_receiptBaseAddress}/{receiptResponse.ftQueueID}/{receiptResponse.ftQueueItemID}";
        }

        // Set isDeliveryNote if HasTransportInformation flag is set
        if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsGR.HasTransportInformation) && !receiptRequest.ftReceiptCase.IsCase(ReceiptCase.DeliveryNote0x0005))
        {
            inv.invoiceHeader.isDeliveryNote = true;
            inv.invoiceHeader.isDeliveryNoteSpecified = true;
        }

        // Apply mydataoverride if present
        if (receiptRequest.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayload>(out var overrideData) && overrideData?.GR?.MyDataOverride != null)
        {
            ApplyMyDataOverride(inv, overrideData.GR.MyDataOverride);
        }

        return inv;
    }

    private static void ApplyMyDataOverride(AadeBookInvoiceType invoice, MyDataOverride overrideData)
    {
        // Apply downloadingInvoiceUrl override if provided
        if (!string.IsNullOrEmpty(overrideData?.Invoice?.DownloadingInvoiceUrl))
        {
            invoice.downloadingInvoiceUrl = overrideData.Invoice.DownloadingInvoiceUrl;
        }

        if (overrideData?.Invoice?.InvoiceHeader == null)
        {
            return;
        }

        var headerOverride = overrideData.Invoice.InvoiceHeader;
        // Apply invoice type override with validation
        // Apply VAT payment suspension
        if (headerOverride.VatPaymentSuspension.HasValue)
        {
            invoice.invoiceHeader.vatPaymentSuspension = headerOverride.VatPaymentSuspension.Value;
            invoice.invoiceHeader.vatPaymentSuspensionSpecified = true;
        }

        // Apply self-pricing
        if (headerOverride.SelfPricing.HasValue)
        {
            invoice.invoiceHeader.selfPricing = headerOverride.SelfPricing.Value;
            invoice.invoiceHeader.selfPricingSpecified = true;
        }

        // Apply dispatch date
        if (headerOverride.DispatchDate.HasValue)
        {
            invoice.invoiceHeader.dispatchDate = headerOverride.DispatchDate.Value;
            invoice.invoiceHeader.dispatchDateSpecified = true;
        }

        // Apply dispatch time
        if (headerOverride.DispatchTime.HasValue)
        {
            invoice.invoiceHeader.dispatchTime = headerOverride.DispatchTime.Value;
            invoice.invoiceHeader.dispatchTimeSpecified = true;
        }

        // Apply vehicle number
        if (!string.IsNullOrEmpty(headerOverride.VehicleNumber))
        {
            invoice.invoiceHeader.vehicleNumber = headerOverride.VehicleNumber;
        }

        // Apply move purpose
        if (headerOverride.MovePurpose.HasValue)
        {
            invoice.invoiceHeader.movePurpose = headerOverride.MovePurpose.Value;
            invoice.invoiceHeader.movePurposeSpecified = true;
        }

        // Apply fuel invoice
        if (headerOverride.FuelInvoice.HasValue)
        {
            invoice.invoiceHeader.fuelInvoice = headerOverride.FuelInvoice.Value;
            invoice.invoiceHeader.fuelInvoiceSpecified = true;
        }

        // Apply special invoice category
        if (headerOverride.SpecialInvoiceCategory.HasValue)
        {
            invoice.invoiceHeader.specialInvoiceCategory = headerOverride.SpecialInvoiceCategory.Value;
            invoice.invoiceHeader.specialInvoiceCategorySpecified = true;
        }

        // Apply invoice variation type
        if (headerOverride.InvoiceVariationType.HasValue)
        {
            invoice.invoiceHeader.invoiceVariationType = headerOverride.InvoiceVariationType.Value;
            invoice.invoiceHeader.invoiceVariationTypeSpecified = true;
        }

        // Apply other move purpose title
        if (!string.IsNullOrEmpty(headerOverride.OtherMovePurposeTitle))
        {
            invoice.invoiceHeader.otherMovePurposeTitle = headerOverride.OtherMovePurposeTitle;
        }

        // Apply other delivery note header
        if (headerOverride.OtherDeliveryNoteHeader != null)
        {
            if (invoice.invoiceHeader.otherDeliveryNoteHeader == null)
            {
                invoice.invoiceHeader.otherDeliveryNoteHeader = new OtherDeliveryNoteHeaderType();
            }

            // Apply loading address
            if (headerOverride.OtherDeliveryNoteHeader.LoadingAddress != null)
            {
                invoice.invoiceHeader.otherDeliveryNoteHeader.loadingAddress = new AddressType
                {
                    street = headerOverride.OtherDeliveryNoteHeader.LoadingAddress.Street,
                    number = headerOverride.OtherDeliveryNoteHeader.LoadingAddress.Number ?? "0",
                    postalCode = headerOverride.OtherDeliveryNoteHeader.LoadingAddress.PostalCode,
                    city = headerOverride.OtherDeliveryNoteHeader.LoadingAddress.City
                };
            }

            // Apply delivery address
            if (headerOverride.OtherDeliveryNoteHeader.DeliveryAddress != null)
            {
                invoice.invoiceHeader.otherDeliveryNoteHeader.deliveryAddress = new AddressType
                {
                    street = headerOverride.OtherDeliveryNoteHeader.DeliveryAddress.Street,
                    number = headerOverride.OtherDeliveryNoteHeader.DeliveryAddress.Number ?? "0",
                    postalCode = headerOverride.OtherDeliveryNoteHeader.DeliveryAddress.PostalCode,
                    city = headerOverride.OtherDeliveryNoteHeader.DeliveryAddress.City
                };
            }

            // Apply start shipping branch
            if (headerOverride.OtherDeliveryNoteHeader.StartShippingBranch.HasValue)
            {
                invoice.invoiceHeader.otherDeliveryNoteHeader.startShippingBranch = headerOverride.OtherDeliveryNoteHeader.StartShippingBranch.Value;
                invoice.invoiceHeader.otherDeliveryNoteHeader.startShippingBranchSpecified = true;
            }

            // Apply complete shipping branch
            if (headerOverride.OtherDeliveryNoteHeader.CompleteShippingBranch.HasValue)
            {
                invoice.invoiceHeader.otherDeliveryNoteHeader.completeShippingBranch = headerOverride.OtherDeliveryNoteHeader.CompleteShippingBranch.Value;
                invoice.invoiceHeader.otherDeliveryNoteHeader.completeShippingBranchSpecified = true;
            }
        }
    }

    private static List<TaxTotalsType> GetDocumentLevelTaxes(ReceiptRequest receiptRequest)
    {
        if (AADEMappings.GetInvoiceType(receiptRequest) == InvoiceType.Item82)
        {
            // For item 82 we define the taxes at line level only
            return new List<TaxTotalsType>();
        }

        var documentTaxes = new List<TaxTotalsType>();
        foreach (var item in receiptRequest.cbChargeItems.Where(x => SpecialTaxMappings.IsSpecialTaxItem(x)))
        {
            var withholdingMapping = SpecialTaxMappings.GetWithholdingTaxMapping(item.Description);
            if (withholdingMapping != null)
            {
                documentTaxes.Add(new TaxTotalsType
                {
                    taxType = MyDataTaxCategories.WithHeldTaxes,
                    taxCategory = withholdingMapping.Code,
                    taxCategorySpecified = true,
                    taxAmount = Math.Abs(item.Amount)
                });
                continue;
            }

            var feeMapping = SpecialTaxMappings.GetFeeMapping(item.Description);
            if (feeMapping != null)
            {
                documentTaxes.Add(new TaxTotalsType
                {
                    taxType = MyDataTaxCategories.Fees,
                    taxCategory = feeMapping.Code,
                    taxCategorySpecified = true,
                    taxAmount = Math.Abs(item.Amount)
                });
                continue;
            }

            // If no fee mapping found, try stamp duty mapping
            var stampDutyMapping = SpecialTaxMappings.GetStampDutyMapping(item.Description);
            if (stampDutyMapping != null)
            {
                documentTaxes.Add(new TaxTotalsType
                {
                    taxType = MyDataTaxCategories.StampDuty,
                    taxCategory = stampDutyMapping.Code,
                    taxCategorySpecified = true,
                    taxAmount = Math.Abs(item.Amount)
                });
                continue;
            }

            // If no stamp duty mapping found, try other tax mapping
            var otherTaxMapping = SpecialTaxMappings.GetOtherTaxMapping(item.Description);
            if (otherTaxMapping != null)
            {
                documentTaxes.Add(new TaxTotalsType
                {
                    taxType = MyDataTaxCategories.OtherTaxes,
                    taxCategory = otherTaxMapping.Code,
                    taxCategorySpecified = true,
                    taxAmount = Math.Abs(item.Amount)
                });
                continue;
            }

            // If no mapping found, throw exception. To add new mappings based on the category in the official mydata repo. At this stage we do a 1:1 mapping from description in the given original table (e.g. withholding) to the mydata category.
            throw new Exception($"No withholding tax, fee, stamp duty, or other tax mapping found for description: '{item.Description}'. " +
                              "Please use one of the supported Greek tax descriptions or add a new mapping.");
        }
        return documentTaxes;
    }

    private static List<InvoiceRowType> GetInvoiceDetailsIncludingTaxes(ReceiptRequest receiptRequest)
    {
        var nonSpecialTaxes = receiptRequest.GetGroupedChargeItems()
            .Where(grouped => !SpecialTaxMappings.IsSpecialTaxItem(grouped.chargeItem))
            .ToList();

        if (nonSpecialTaxes.Count > 0)
        {
            throw new Exception("When using this type of invoice only ChargeItems of type Special Tax are supported.");
        }
        var chargeItems = receiptRequest.GetGroupedChargeItems().ToList();
        var invoiceRows = new List<InvoiceRowType>();
        var nextPosition = 1;
        foreach (var chargeItem in chargeItems)
        {
            var item = chargeItem.chargeItem;
            var invoiceRow = new InvoiceRowType
            {
                lineNumber = (int) item.Position,
                netValue = 0,
                vatCategory = AADEMappings.GetVATCategory(item),
                vatAmount = 0
            };

            if (((int) item.Position) == 0)
            {
                invoiceRow.lineNumber = nextPosition++;
            }
            else
            {
                nextPosition = (int) item.Position + 1;
            }

            var withholdingMapping = SpecialTaxMappings.GetWithholdingTaxMapping(item.Description);
            if (withholdingMapping != null)
            {
                invoiceRow.withheldAmount = Math.Abs(item.Amount);
                invoiceRow.withheldAmountSpecified = true;
                invoiceRow.withheldPercentCategory = withholdingMapping.Code;
                invoiceRow.withheldPercentCategorySpecified = true;
                invoiceRows.Add(invoiceRow);
                continue;
            }

            var feeMapping = SpecialTaxMappings.GetFeeMapping(item.Description);
            if (feeMapping != null)
            {
                invoiceRow.feesAmount = Math.Abs(item.Amount);
                invoiceRow.feesAmountSpecified = true;
                invoiceRow.feesPercentCategory = feeMapping.Code;
                invoiceRow.feesPercentCategorySpecified = true;
                invoiceRows.Add(invoiceRow);
                continue;
            }

            // If no fee mapping found, try stamp duty mapping
            var stampDutyMapping = SpecialTaxMappings.GetStampDutyMapping(item.Description);
            if (stampDutyMapping != null)
            {
                invoiceRow.stampDutyAmount = Math.Abs(item.Amount);
                invoiceRow.stampDutyAmountSpecified = true;
                invoiceRow.stampDutyPercentCategory = stampDutyMapping.Code;
                invoiceRow.stampDutyPercentCategorySpecified = true;
                invoiceRows.Add(invoiceRow);
                continue;
            }

            // If no stamp duty mapping found, try other tax mapping
            var otherTaxMapping = SpecialTaxMappings.GetOtherTaxMapping(item.Description);
            if (otherTaxMapping != null)
            {
                invoiceRow.otherTaxesAmount = Math.Abs(item.Amount);
                invoiceRow.otherTaxesAmountSpecified = true;
                invoiceRow.otherTaxesPercentCategory = otherTaxMapping.Code;
                invoiceRow.otherTaxesPercentCategorySpecified = true;
                invoiceRows.Add(invoiceRow);
                continue;
            }

            // If no mapping found, throw exception. To add new mappings based on the category in the official mydata repo. At this stage we do a 1:1 mapping from description in the given original table (e.g. withholding) to the mydata category.
            throw new Exception($"No withholding tax, fee, stamp duty, or other tax mapping found for description: '{item.Description}'. " +
                              "Please use one of the supported Greek tax descriptions or add a new mapping.");
        }
        return invoiceRows;
    }

    private static List<InvoiceRowType> GetInvoiceDetails(ReceiptRequest receiptRequest)
    {
        if(AADEMappings.GetInvoiceType(receiptRequest) == InvoiceType.Item82)
        {
            // For Invoice Types of type 82 we use a different loading mechanism for the invocies to ensure that taxlevels are included
            return GetInvoiceDetailsIncludingTaxes(receiptRequest);
        }

        var chargeItems = receiptRequest.GetGroupedChargeItems()
            .Where(grouped => !SpecialTaxMappings.IsSpecialTaxItem(grouped.chargeItem))
            .ToList();

        var nextPosition = 1;
        return chargeItems.Select(grouped =>
        {
            var x = grouped.chargeItem;

            var vatAmount = x.GetVATAmount();
            var invoiceRow = new InvoiceRowType
            {
                quantity = receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) ? -x.Quantity : x.Quantity,
                lineNumber = (int) x.Position,
                vatAmount = receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) ? -vatAmount : vatAmount,
                netValue = receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) ? -x.Amount - -vatAmount : x.Amount - vatAmount,
            };

            if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.Order0x3004) || receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsGR.HasTransportInformation))
            {
                invoiceRow.quantitySpecified = true;
            }

            if (((int) x.Position) == 0)
            {
                invoiceRow.lineNumber = nextPosition++;
            }
            else
            {
                nextPosition = (int) x.Position + 1;
            }

            if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.Order0x3004) || receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsGR.HasTransportInformation))
            {
                invoiceRow.itemDescr = x.Description;
                if (x.Unit == "Litres")
                {
                    invoiceRow.measurementUnit = 3;
                    invoiceRow.measurementUnitSpecified = true;
                }
                else if (x.Unit == "Kg")
                {
                    invoiceRow.measurementUnit = 2;
                    invoiceRow.measurementUnitSpecified = true;
                }
                else
                {
                    invoiceRow.measurementUnit = 1;
                    invoiceRow.measurementUnitSpecified = true;
                }

            }

            if (x.ftChargeItemCase.NatureOfVat() != ChargeItemCaseNatureOfVatGR.UsualVatApplies)
            {
                // In cases of using exempt reasons we will have a zero VAT Rate
                invoiceRow.vatCategory = MyDataVatCategory.VatRate0_ExcludingVat_Category7;
                var exemptionCategory = AADEMappings.GetVatExemptionCategory(x);
                if (exemptionCategory.HasValue)
                {
                    invoiceRow.vatExemptionCategorySpecified = true;
                    invoiceRow.vatExemptionCategory = exemptionCategory.Value;
                    invoiceRow.incomeClassification = [AADEMappings.GetIncomeClassificationType(receiptRequest, x)];
                }
                else
                {
                    throw new Exception($"The VAT exemption for the given Nature 0x{x.ftChargeItemCase.NatureOfVat():x}  is not supported.");
                }
            }
            else
            {
                if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002))
                {
                    invoiceRow.vatCategory = MyDataVatCategory.RegistrationsWithoutVat;
                    invoiceRow.incomeClassification = [AADEMappings.GetIncomeClassificationType(receiptRequest, x)];
                }
                else if (x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.Voucher))
                {
                    if (x.ftChargeItemCase.IsVat(ChargeItemCase.NotTaxable))
                    {
                        invoiceRow.vatExemptionCategorySpecified = true;
                        invoiceRow.vatExemptionCategory = 27;
                        invoiceRow.vatCategory = MyDataVatCategory.VatRate0_ExcludingVat_Category7;
                    }
                    else
                    {
                        invoiceRow.recType = 6;
                        invoiceRow.vatCategory = AADEMappings.GetVATCategory(x);
                    }
                }
                else
                {
                    invoiceRow.vatCategory = AADEMappings.GetVATCategory(x);
                    if (receiptRequest.cbChargeItems.Any(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.Receivable)))
                    {
                        if (receiptRequest.cbChargeItems.Any(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.Receivable) && !x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.Receivable)))
                        {
                            invoiceRow.invoiceDetailType = 2;
                            invoiceRow.invoiceDetailTypeSpecified = true;
                            invoiceRow.incomeClassification = [
                                new IncomeClassificationType {
                                            amount = invoiceRow.netValue,
                                            classificationCategory = AADEMappings.GetIncomeClassificationCategoryType(receiptRequest, x),
                                            classificationType = AADEMappings.GetIncomeClassificationValueType(receiptRequest, x),
                                            classificationTypeSpecified = true
                                        }
                            ];
                        }
                        else if (x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.Receivable))
                        {
                            invoiceRow.invoiceDetailType = 1;
                            invoiceRow.invoiceDetailTypeSpecified = true;
                            invoiceRow.expensesClassification = [
                                new ExpensesClassificationType {
                                                amount = invoiceRow.netValue,
                                                classificationCategorySpecified = true,
                                                classificationCategory = ExpensesClassificationCategoryType.category2_9
                                            }
                            ];
                        }
                    }
                    else
                    {
                        invoiceRow.incomeClassification = [AADEMappings.GetIncomeClassificationType(receiptRequest, x)];
                    }
                }
            }
            if (grouped.modifiers.Count > 0)
            {
                invoiceRow.deductionsAmount = grouped.modifiers.Sum(x => x.Amount) * -1;
                invoiceRow.deductionsAmountSpecified = true;
                invoiceRow.discountOption = true;
                invoiceRow.discountOptionSpecified = true;
            }
            return invoiceRow;
        }).ToList();
    }

    private static long GetInvoiceMark(ReceiptResponse receiptResponse)
    {
        if (receiptResponse.ftSignatures.Count > 0)
        {
            var invoiceMarkText = receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "invoiceMark")?.Data;
            if (long.TryParse(invoiceMarkText, out var invoiceMark))
            {
                return invoiceMark;
            }
            else
            {
                return -1;
            }
        }
        return -1;
    }

    private static List<PaymentMethodDetailType> GetPayments(ReceiptRequest receiptRequest)
    {
        // what is payitemcase 99?
        return receiptRequest.cbPayItems.Where(x => !(x.ftPayItemCase.IsCase(PayItemCase.Grant) && x.ftPayItemCase.IsFlag(PayItemCaseFlags.Tip)) && !(x.ftPayItemCase.IsCase(PayItemCase.DebitCardPayment) && x.ftPayItemCase.IsFlag(PayItemCaseFlags.Tip))).Select(x =>
        {
            var payment = new PaymentMethodDetailType
            {
                type = AADEMappings.GetPaymentType(x),
                amount = receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) ? -x.Amount : x.Amount,
                paymentMethodInfo = x.Description,
            };
            var tipPayment = receiptRequest.cbPayItems.FirstOrDefault(x => x.ftPayItemCase.IsFlag(PayItemCaseFlags.Tip));
            if (tipPayment != null)
            {
                payment.tipAmount = tipPayment.Amount;
                payment.tipAmountSpecified = true;
            }

            if (x.ftPayItemCaseData != null)
            {
                var providerData = JsonSerializer.Deserialize<GenericPaymentPayload>(JsonSerializer.Serialize(x.ftPayItemCaseData), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (providerData != null && providerData.Provider != null && providerData.Provider.ProtocolRequest is JsonElement dat && dat.ValueKind == JsonValueKind.String)
                {
                    var app2AppApi = JsonSerializer.Deserialize<PayItemCaseDataApp2App>(JsonSerializer.Serialize(x.ftPayItemCaseData), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })!;
                    if (app2AppApi.Provider is PayItemCaseProviderVivaWalletApp2APp vivaAppToApp)
                    {
                        var requestUri = HttpUtility.ParseQueryString(new Uri(vivaAppToApp.ProtocolRequest).Query);
                        var responesUri = HttpUtility.ParseQueryString(new Uri(vivaAppToApp.ProtocolResponse).Query);
                        payment.transactionId = responesUri["aadeTransactionId"];

                        payment.ProvidersSignature = new ProviderSignatureType
                        {
                            Signature = requestUri["aadeProviderSignature"],
                            SigningAuthor = VIVA_FISCAL_PROVIDER_ID
                        };
                    }
                }
                else if (providerData != null && providerData.Provider != null && providerData.Provider.ProtocolRequest is JsonElement datS && datS.ValueKind == JsonValueKind.Object)
                {
                    var providerCloudRestApi = JsonSerializer.Deserialize<PayItemCaseDataCloudApi>(JsonSerializer.Serialize(x.ftPayItemCaseData), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })!;
                    if (providerCloudRestApi.Provider is PayItemCaseProviderVivaWallet vivaPayment)
                    {

                        payment.transactionId = vivaPayment.ProtocolResponse?.aadeTransactionId;
                        payment.ProvidersSignature = new ProviderSignatureType
                        {
                            Signature = vivaPayment.ProtocolRequest?.aadeProviderSignature,
                            SigningAuthor = VIVA_FISCAL_PROVIDER_ID
                        };
                    }
                }
                else
                {
                    try
                    {
                        var payItemCaseDataJson = JsonSerializer.Serialize(x.ftPayItemCaseData);
                        using var jsonDoc = JsonDocument.Parse(payItemCaseDataJson);
                        if (jsonDoc.RootElement.TryGetProperty("aadeSignatureData", out var aadeSignatureDataElement))
                        {
                            if (aadeSignatureDataElement.TryGetProperty("aadeProviderSignature", out var aadeProviderSignatureElement) && aadeProviderSignatureElement.ValueKind == JsonValueKind.String)
                            {

                                payment.ProvidersSignature = new ProviderSignatureType
                                {
                                    Signature = aadeProviderSignatureElement.GetString(),
                                    SigningAuthor = VIVA_FISCAL_PROVIDER_ID
                                };
                            }

                            if (aadeSignatureDataElement.TryGetProperty("aadeTransactionId", out var aadeTransactionIdElement) && aadeTransactionIdElement.ValueKind == JsonValueKind.String)
                            {
                                payment.transactionId = aadeTransactionIdElement.GetString();
                            }
                        }
                    }
                    catch { }
                }
            }
            return payment;
        }).ToList();
    }

    public static PartyType? GetCounterPart(ReceiptRequest receiptRequest)
    {
        var customer = receiptRequest.GetCustomerOrNull();
        if (customer == null)
        {
            return null;
        }

        if (!CountryTypeMapper.TryParseCountryCode(customer.CustomerCountry ?? "", out var countryType))
        {
            return null;
        }

        if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsGR.HasTransportInformation))
        {
            return new PartyType
            {
                vatNumber = customer?.CustomerVATId,
                country = countryType,
                branch = 0,
                address = new AddressType
                {
                    street = customer?.CustomerStreet,
                    city = customer?.CustomerCity,
                    postalCode = customer?.CustomerZip,
                    number = 0.ToString()
                },
                name = customer?.CustomerName,
            };
        }


        if (customer?.CustomerVATId?.StartsWith("EL") == true && countryType == CountryType.GR)
        {
            return new PartyType
            {
                vatNumber = customer?.CustomerVATId.Replace("EL", ""),
                country = CountryType.GR,
                branch = 0,
            };
        }
        else if (customer?.CustomerVATId?.StartsWith("GR") == true && countryType == CountryType.GR)
        {
            return new PartyType
            {
                vatNumber = customer?.CustomerVATId.Replace("GR", ""),
                country = CountryType.GR,
                branch = 0,
            };
        }
        else if (customer?.CustomerCountry == "GR" && countryType == CountryType.GR)
        {
            return new PartyType
            {
                vatNumber = customer?.CustomerVATId,
                country = CountryType.GR,
                branch = 0,
            };
        }
        else
        {
            return new PartyType
            {
                vatNumber = customer?.CustomerVATId,
                country = countryType,
                branch = 0,
                address = new AddressType
                {
                    street = customer?.CustomerStreet,
                    city = customer?.CustomerCity,
                    postalCode = customer?.CustomerZip
                },
                name = customer?.CustomerName,
            };
        }
    }

    public static string GetAADEVAT(string issuerVat)
    {
        if (issuerVat?.StartsWith("EL") == true)
        {
            issuerVat = issuerVat.Replace("EL", "");
        }
        else if (issuerVat?.StartsWith("GR") == true)
        {
            issuerVat = issuerVat.Replace("GR", "");
        }
        return issuerVat;
    }

    private PartyType CreateIssuer(ReceiptRequest receiptRequest)
    {
        var branch = 0;
        if (!string.IsNullOrEmpty(_masterDataConfiguration?.Outlet?.LocationId) && int.TryParse(_masterDataConfiguration?.Outlet?.LocationId, out var locationId))
        {
            branch = locationId;
        }


        if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsGR.HasTransportInformation))
        {
            return new PartyType
            {
                vatNumber = GetAADEVAT(_masterDataConfiguration?.Account?.VatId),
                country = CountryType.GR,
                branch = branch,
                address = new AddressType
                {
                    street = _masterDataConfiguration.Outlet.Street,
                    city = _masterDataConfiguration.Outlet.City,
                    postalCode = _masterDataConfiguration.Outlet.Zip,
                    number = "0"
                },
                name = _masterDataConfiguration.Account.AccountName,
            };
        }

        return new PartyType
        {
            vatNumber = GetAADEVAT(_masterDataConfiguration?.Account?.VatId),
            country = CountryType.GR,
            branch = branch
        };
    }

    public string GetUid(AadeBookInvoiceType invoice) => BitConverter.ToString(SHA1.HashData(Encoding.UTF8.GetBytes($"{invoice.issuer.vatNumber}-{invoice.invoiceHeader.issueDate.ToString("yyyy-MM-dd")}-{invoice.issuer.branch}-{invoice.invoiceHeader.invoiceType.GetXmlEnumAttributeValueFromEnum() ?? ""}-{invoice.invoiceHeader.series}-{invoice.invoiceHeader.aa}"))).Replace("-", "");

    public static string GenerateInvoicePayload(InvoicesDoc doc)
    {
        var xmlSerializer = new XmlSerializer(typeof(InvoicesDoc));
        var settings = new XmlWriterSettings
        {
            Indent = false,
            NewLineHandling = NewLineHandling.None
        };
        using (var stringWriter = new StringWriter())
        using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
        {
            xmlSerializer.Serialize(xmlWriter, doc);
            return stringWriter.ToString();
        }
    }
}