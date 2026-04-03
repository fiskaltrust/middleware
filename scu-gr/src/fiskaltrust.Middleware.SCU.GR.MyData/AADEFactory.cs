using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
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
using fiskaltrust.Middleware.Localization.QueueGR.Validation;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class AADEFactoryError
{
    public Exception Exception { get; set; } = null!;
}

public class AADEFactory
{
    private const string VIVA_FISCAL_PROVIDER_ID = "126";

    private readonly MasterDataConfiguration _masterDataConfiguration;
    private readonly string _receiptBaseAddress;

    public AADEFactory(MasterDataConfiguration masterDataConfiguration, string receiptBaseAddress)
    {
        if (string.IsNullOrWhiteSpace(receiptBaseAddress))
        {
            throw new ArgumentException("Receipt base address is required for myDATA v1.0.12", nameof(receiptBaseAddress));
        }
        _masterDataConfiguration = masterDataConfiguration;
        _receiptBaseAddress = receiptBaseAddress;
    }

    public static string GetReceiptUrl(string receiptBaseAddress, Guid ftQueueID, Guid ftQueueItemID)
    {
        return $"{receiptBaseAddress}/{ftQueueID}/{ftQueueItemID}";
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
                issueDate = AADEMappings.GetLocalTime(receiptRequest),
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

            // Reverse delivery note: 9.3 + ReceiptCaseFlags.Refund on the receipt case
            if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
            {
                inv.invoiceHeader.reverseDeliveryNote = true;
                inv.invoiceHeader.reverseDeliveryNoteSpecified = true;
            }
        }

        var isVoidFlag = receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void);
        if (inv.invoiceHeader.invoiceType == InvoiceType.Item86 && isVoidFlag)
        {
            // set the required header fields for Invoice Type 8.6 with VOID/CANCEL flag
            SetInvoiceHeaderFieldsForVoid(inv.invoiceHeader, receiptRequest);
        }
        else if (isVoidFlag)
        {
            // For other invoice types, voiding is not supported
            // we choose to throw an exception
            throw new Exception("Voiding of documents is not supported for this invoice type. Please use refund.");
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

        if (inv.invoiceHeader.invoiceType == InvoiceType.Item86)
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

        // Set downloadingInvoiceUrl (always required)
        inv.downloadingInvoiceUrl = GetReceiptUrl(_receiptBaseAddress, receiptResponse.ftQueueID, receiptResponse.ftQueueItemID);

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

        // Strip income classifications for invoice types that forbid them (e.g. 3.1, 3.2 Title Deeds).
        if (!AADEMappings.SupportsIncomeClassification(inv.invoiceHeader.invoiceType))
        {
            foreach (var detail in inv.invoiceDetails)
            {
                detail.incomeClassification = null;
            }
            inv.invoiceSummary.incomeClassification = null;
        }

        // Set correlatedInvoices / multipleConnectedMarks based on the final invoice type
        // (after override, so the correct field is used for the resolved type).
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
                else if (AADEMappings.SupportsCorrelatedInvoices(inv.invoiceHeader.invoiceType))
                {
                    inv.invoiceHeader.correlatedInvoices = receiptReferences.Select(x => GetInvoiceMark(x.Item2)).ToArray();
                }
            }
        }

        // Validate: if any charge item has incomeClassification override, all must have it and invoiceType must be overridden
        ValidateClassificationOverrideConsistency(receiptRequest, overrideData);
        return inv;
    }

    private static void ValidateClassificationOverrideConsistency(ReceiptRequest receiptRequest, ftReceiptCaseDataPayload? overrideData)
    {
        var itemsWithClassificationOverride = receiptRequest.cbChargeItems.Where(ci =>
        {
            if (ci.ftChargeItemCaseData == null)
                return false;
            try
            {
                var data = JsonSerializer.Deserialize<ftChargeItemCaseDataPayload>(
                    JsonSerializer.Serialize(ci.ftChargeItemCaseData),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var details = data?.GR?.MyDataOverride?.InvoiceDetails;
                return details?.IncomeClassification != null || details?.ExpensesClassification != null;
            }
            catch { return false; }
        }).Count();

        if (itemsWithClassificationOverride == 0)
            return;

        if (itemsWithClassificationOverride != receiptRequest.cbChargeItems.Count)
        {
            throw new ArgumentException(
                "When a classification override (incomeClassification or expensesClassification) is set on any charge item, every charge item must have a classification override.");
        }

    }

    private static void ApplyMyDataOverride(AadeBookInvoiceType invoice, ReceiptRequestMyDataOverride overrideData)
    {
        if (overrideData?.Invoice == null)
        {
            return;
        }

        var invoiceOverride = overrideData.Invoice;

        if (invoiceOverride.InvoiceHeader != null)
        {
            ApplyInvoiceHeaderOverride(invoice, invoiceOverride.InvoiceHeader);
        }

        // Reverse delivery note purpose validation (9.3 + Refund)
        if (invoice.invoiceHeader.invoiceType == InvoiceType.Item93
            && invoice.invoiceHeader.reverseDeliveryNote)
        {
            if (!invoiceOverride.InvoiceHeader?.ReverseDeliveryNotePurpose.HasValue ?? true)
            {
                throw new ArgumentException(
                    "reverseDeliveryNotePurpose is mandatory for reverse delivery note ",
                    nameof(invoiceOverride.InvoiceHeader.ReverseDeliveryNotePurpose));
            }

            invoice.invoiceHeader.reverseDeliveryNotePurpose =
               AADEMappings.GetReverseDeliveryNotePurpose(invoiceOverride.InvoiceHeader!.ReverseDeliveryNotePurpose!.Value);
            invoice.invoiceHeader.reverseDeliveryNotePurposeSpecified = true;
        }

        if (invoiceOverride.Counterpart != null && invoice.counterpart != null)
        {
            var counterpart = invoice.counterpart;
            ApplyPartyOverride(ref counterpart, invoiceOverride.Counterpart);
            invoice.counterpart = counterpart;
        }

        if (invoiceOverride.OtherTransportDetails != null)
        {
            invoice.otherTransportDetails = [.. invoiceOverride.OtherTransportDetails.Select(t => new TransportDetailType { vehicleNumber = t.VehicleNumber })];
        }
    }

    private static readonly Dictionary<string, InvoiceType> InvoiceTypeMap = typeof(InvoiceType)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .ToDictionary(
            f => f.GetCustomAttributes(typeof(System.Xml.Serialization.XmlEnumAttribute), false)
                  .Cast<System.Xml.Serialization.XmlEnumAttribute>()
                  .FirstOrDefault()?.Name ?? f.Name,
            f => (InvoiceType) f.GetValue(null)!,
            StringComparer.OrdinalIgnoreCase);

    private static void ApplyInvoiceHeaderOverride(AadeBookInvoiceType invoice, InvoiceHeaderTypeOverride headerOverride)
    {
        // Apply invoice type override
        if (!string.IsNullOrEmpty(headerOverride.InvoiceType))
        {
            if (!InvoiceTypeMap.TryGetValue(headerOverride.InvoiceType, out var invoiceType))
            {
                throw new ArgumentException(
                    $"Invalid invoiceType override value '{headerOverride.InvoiceType}'. " +
                    $"Allowed values: {string.Join(", ", InvoiceTypeMap.Keys.OrderBy(k => k))}");
            }
            invoice.invoiceHeader.invoiceType = invoiceType;
        }

        // Apply VAT payment suspension
        if (headerOverride.VatPaymentSuspension.HasValue)
        {
            invoice.invoiceHeader.vatPaymentSuspension = headerOverride.VatPaymentSuspension.Value;
            invoice.invoiceHeader.vatPaymentSuspensionSpecified = true;
        }

        // Apply exchange rate
        if (headerOverride.ExchangeRate.HasValue)
        {
            invoice.invoiceHeader.exchangeRate = headerOverride.ExchangeRate.Value;
            invoice.invoiceHeader.exchangeRateSpecified = true;
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

        // Apply other correlated entities
        if (headerOverride.OtherCorrelatedEntities != null)
        {
            invoice.invoiceHeader.otherCorrelatedEntities = [.. headerOverride.OtherCorrelatedEntities.Select(e =>
            {
                var entity = new EntityType();
                if (e.Type.HasValue)
                    entity.type = (sbyte) e.Type.Value;
                if (e.EntityData != null)
                {
                    entity.entityData = new PartyType();
                    var party = entity.entityData;
                    ApplyPartyOverride(ref party, e.EntityData);
                    entity.entityData = party;
                }
                return entity;
            })];
        }

        // Apply other delivery note header
        if (headerOverride.OtherDeliveryNoteHeader != null)
        {
            if (invoice.invoiceHeader.otherDeliveryNoteHeader == null)
            {
                invoice.invoiceHeader.otherDeliveryNoteHeader = new OtherDeliveryNoteHeaderType();
            }

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

            if (headerOverride.OtherDeliveryNoteHeader.StartShippingBranch.HasValue)
            {
                invoice.invoiceHeader.otherDeliveryNoteHeader.startShippingBranch = headerOverride.OtherDeliveryNoteHeader.StartShippingBranch.Value;
                invoice.invoiceHeader.otherDeliveryNoteHeader.startShippingBranchSpecified = true;
            }

            if (headerOverride.OtherDeliveryNoteHeader.CompleteShippingBranch.HasValue)
            {
                invoice.invoiceHeader.otherDeliveryNoteHeader.completeShippingBranch = headerOverride.OtherDeliveryNoteHeader.CompleteShippingBranch.Value;
                invoice.invoiceHeader.otherDeliveryNoteHeader.completeShippingBranchSpecified = true;
            }
        }

        // Apply other move purpose title
        if (!string.IsNullOrEmpty(headerOverride.OtherMovePurposeTitle))
        {
            invoice.invoiceHeader.otherMovePurposeTitle = headerOverride.OtherMovePurposeTitle;
        }

        // Apply third party collection
        if (headerOverride.ThirdPartyCollection.HasValue)
        {
            invoice.invoiceHeader.thirdPartyCollection = headerOverride.ThirdPartyCollection.Value;
            invoice.invoiceHeader.thirdPartyCollectionSpecified = true;
        }

        // Apply total cancel delivery orders
        if (headerOverride.TotalCancelDeliveryOrders.HasValue)
        {
            invoice.invoiceHeader.totalCancelDeliveryOrders = headerOverride.TotalCancelDeliveryOrders.Value;
            invoice.invoiceHeader.totalCancelDeliveryOrdersSpecified = true;
        }

        // Apply reverse delivery note
        if (headerOverride.ReverseDeliveryNote.HasValue)
        {
            invoice.invoiceHeader.reverseDeliveryNote = headerOverride.ReverseDeliveryNote.Value;
            invoice.invoiceHeader.reverseDeliveryNoteSpecified = true;
        }

    }

    private static void ApplyPartyOverride(ref PartyType party, PartyTypeOverride partyOverride)
    {
        if (party == null)
            party = new PartyType();
        if (partyOverride.Branch.HasValue)
            party.branch = partyOverride.Branch.Value;
        if (!string.IsNullOrEmpty(partyOverride.DocumentIdNo))
            party.documentIdNo = partyOverride.DocumentIdNo;
        if (!string.IsNullOrEmpty(partyOverride.SupplyAccountNo))
            party.supplyAccountNo = partyOverride.SupplyAccountNo;
        if (!string.IsNullOrEmpty(partyOverride.CountryDocumentId) && Enum.TryParse<CountryType>(partyOverride.CountryDocumentId, true, out var countryDocId))
        {
            party.countryDocumentId = countryDocId;
            party.countryDocumentIdSpecified = true;
        }
        if (partyOverride.Address != null)
        {
            party.address = new AddressType
            {
                number = partyOverride.Address.Number ?? "0"
            };
        }
    }

    public static void ApplyInvoiceDetailOverride(InvoiceRowType row, InvoiceRowTypeOverride detailOverride)
    {
        if (detailOverride.RecType.HasValue)
        {
            row.recType = detailOverride.RecType.Value;
            row.recTypeSpecified = true;
        }
        if (!string.IsNullOrEmpty(detailOverride.TaricNo))
            row.TaricNo = detailOverride.TaricNo;
        if (!string.IsNullOrEmpty(detailOverride.ItemCode))
            row.itemCode = detailOverride.ItemCode;
        if (detailOverride.FuelCode.HasValue)
        {
            row.fuelCode = (FuelCodes) detailOverride.FuelCode.Value;
            row.fuelCodeSpecified = true;
        }

        if (detailOverride.InvoiceDetailType.HasValue)
        {
            row.invoiceDetailType = detailOverride.InvoiceDetailType.Value;
            row.invoiceDetailTypeSpecified = true;
        }

        if (detailOverride.Dienergia != null)
        {
            row.dienergia = new ShipType
            {
                applicationId = detailOverride.Dienergia.ApplicationId,
                doy = detailOverride.Dienergia.Doy,
                shipId = detailOverride.Dienergia.ShipId
            };
            if (detailOverride.Dienergia.ApplicationDate.HasValue)
            {
                row.dienergia.applicationDate = detailOverride.Dienergia.ApplicationDate.Value;
            }
        }
        if (detailOverride.DiscountOption.HasValue)
        {
            row.discountOption = detailOverride.DiscountOption.Value;
            row.discountOptionSpecified = true;
        }

        if (!string.IsNullOrEmpty(detailOverride.LineComments))
            row.lineComments = detailOverride.LineComments;
        if (detailOverride.IncomeClassification != null)
        {
            if (detailOverride.IncomeClassification.Count != 1)
            {
                throw new ArgumentException("incomeClassification override must contain exactly one element.");
            }
            var ic = detailOverride.IncomeClassification[0];
            var existing = row.incomeClassification?.FirstOrDefault() ?? new IncomeClassificationType { amount = row.netValue };
            if (!string.IsNullOrEmpty(ic.ClassificationType))
            {
                if (!Enum.TryParse<IncomeClassificationValueType>(ic.ClassificationType, true, out var type))
                {
                    throw new ArgumentException($"Invalid incomeClassification.classificationType '{ic.ClassificationType}'. Allowed values: {string.Join(", ", Enum.GetNames(typeof(IncomeClassificationValueType)))}");
                }
                existing.classificationType = type;
                existing.classificationTypeSpecified = true;
            }
            if (!string.IsNullOrEmpty(ic.ClassificationCategory))
            {
                if (!Enum.TryParse<IncomeClassificationCategoryType>(ic.ClassificationCategory, true, out var cat))
                {
                    throw new ArgumentException($"Invalid incomeClassification.classificationCategory '{ic.ClassificationCategory}'. Allowed values: {string.Join(", ", Enum.GetNames(typeof(IncomeClassificationCategoryType)))}");
                }
                existing.classificationCategory = cat;
            }
            row.incomeClassification = [existing];
        }
        if (detailOverride.ExpensesClassification != null)
        {
            if (detailOverride.ExpensesClassification.Count != 1)
            {
                throw new ArgumentException("expensesClassification override must contain exactly one element.");
            }
            var ec = detailOverride.ExpensesClassification[0];
            var existing = row.expensesClassification?.FirstOrDefault() ?? new ExpensesClassificationType { amount = row.netValue };
            if (!string.IsNullOrEmpty(ec.ClassificationType))
            {
                if (!Enum.TryParse<ExpensesClassificationTypeClassificationType>(ec.ClassificationType, true, out var type))
                {
                    throw new ArgumentException($"Invalid expensesClassification.classificationType '{ec.ClassificationType}'. Allowed values: {string.Join(", ", Enum.GetNames(typeof(ExpensesClassificationTypeClassificationType)))}");
                }
                existing.classificationType = type;
                existing.classificationTypeSpecified = true;
            }
            if (!string.IsNullOrEmpty(ec.ClassificationCategory))
            {
                if (!Enum.TryParse<ExpensesClassificationCategoryType>(ec.ClassificationCategory, true, out var cat))
                {
                    throw new ArgumentException($"Invalid expensesClassification.classificationCategory '{ec.ClassificationCategory}'. Allowed values: {string.Join(", ", Enum.GetNames(typeof(ExpensesClassificationCategoryType)))}");
                }
                existing.classificationCategory = cat;
                existing.classificationCategorySpecified = true;
            }
            row.expensesClassification = [existing];
            row.incomeClassification = null;
        }
        if (detailOverride.Quantity15.HasValue)
        {
            row.quantity15 = detailOverride.Quantity15.Value;
            row.quantity15Specified = true;
        }
        if (detailOverride.OtherMeasurementUnitQuantity.HasValue)
        {
            row.otherMeasurementUnitQuantity = detailOverride.OtherMeasurementUnitQuantity.Value;
            row.otherMeasurementUnitQuantitySpecified = true;
        }
        if (!string.IsNullOrEmpty(detailOverride.OtherMeasurementUnitTitle))
            row.otherMeasurementUnitTitle = detailOverride.OtherMeasurementUnitTitle;
        if (detailOverride.NotVAT195.HasValue)
        {
            row.notVAT195 = detailOverride.NotVAT195.Value;
            row.notVAT195Specified = true;
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
        foreach (var item in receiptRequest.cbChargeItems.Where(x => SpecialTaxMappings.IsSpecialTaxItem(x) && !SpecialTaxMappings.IsVatableSpecialTaxItem(x)))
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
        var invoiceRows = new List<global::InvoiceRowType>();
        var nextPosition = 1;
        foreach (var chargeItem in chargeItems)
        {
            var item = chargeItem.chargeItem;
            var invoiceRow = new global::InvoiceRowType
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

    /// <summary>
    /// Returns invoice rows for AADE 8.6 VOID restaurant order (multiple/cancel):
    /// - One line (even if multiple charge items provided)
    /// - VAT category = 8 (Entries without VAT)
    /// - No expense/income classifications
    /// Fails if no charge items are present.
    /// </summary>
    private static List<InvoiceRowType> GetInvoiceDetailsForVoid(ReceiptRequest receiptRequest)
    {
        // Must have at least one charge item to describe canceled product/service
        var item = receiptRequest.cbChargeItems?.FirstOrDefault();
        if (item == null)
        {
            throw new ArgumentException("VOID orders require at least one charge item to describe the canceled item.");
        }

        // Optionally: Only use first item's description, ignore extra items per AADE "one line" requirement
        var row = new InvoiceRowType
        {
            lineNumber = 1,
            itemDescr = item.Description ?? "VOID Item",
            quantity = 1.0m,
            quantitySpecified = true,
            measurementUnit = 1,          // usually "pieces" or similar
            measurementUnitSpecified = true,
            netValue = 0.00m,
            vatCategory = 8,              // AADE: no VAT
            vatAmount = 0.00m
            // No classifications
        };

        return new List<global::InvoiceRowType> { row };
    }

    private static List<InvoiceRowType> GetInvoiceDetails(ReceiptRequest receiptRequest)
    {
        if (AADEMappings.GetInvoiceType(receiptRequest) == InvoiceType.Item82)
        {
            // For Invoice Types of type 82 we use a different loading mechanism for the invocies to ensure that taxlevels are included
            return GetInvoiceDetailsIncludingTaxes(receiptRequest);
        }

        var isVoidFlag = receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void);
        if (AADEMappings.GetInvoiceType(receiptRequest) == InvoiceType.Item86 && isVoidFlag)
        {
            // for Invoice Type 8.6 with VOID/CANCEL flag
            // generate a single invoice line with zero values and VAT category 8, as required by AADE for full order cancellation.
            return GetInvoiceDetailsForVoid(receiptRequest);
        }
        else if (isVoidFlag)
        {
            // For other invoice types, voiding is not supported
            // we choose to throw an exception
            throw new Exception("Voiding of documents is not supported for this invoice type. Please use refund.");
        }

        var chargeItems = receiptRequest.GetGroupedChargeItems()
            .Where(grouped =>
                    !SpecialTaxMappings.IsSpecialTaxItem(grouped.chargeItem)
                    ||
                    (SpecialTaxMappings.IsVatableSpecialTaxItem(grouped.chargeItem))
                  )
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

            // Per-item return flag (0x0002) inside an 8.6 order = recType 7
            var isPartialReturnItem = AADEMappings.GetInvoiceType(receiptRequest) == InvoiceType.Item86
                                      && x.IsRefund();

            // myDATA spec: for recType=7 lines amounts MUST be positive; myDATA itself treats them as cancellations.
            if (isPartialReturnItem)
            {
                invoiceRow.recType = 7;
                invoiceRow.recTypeSpecified = true;
                invoiceRow.quantity = Math.Abs(invoiceRow.quantity);
                invoiceRow.vatAmount = Math.Abs(invoiceRow.vatAmount);
                invoiceRow.netValue = Math.Abs(invoiceRow.netValue);
            }

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
                if (!string.IsNullOrEmpty(x.ProductNumber))
                {
                    invoiceRow.itemCode = x.ProductNumber;
                }

                invoiceRow.itemDescr = x.Description;

                invoiceRow.measurementUnit = AADEMappings.GetMeasurementUnit(x);
                invoiceRow.measurementUnitSpecified = true;
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
            if (SpecialTaxMappings.IsVatableSpecialTaxItem(x))
            {
                var feeMapping = SpecialTaxMappings.GetFeeMapping(x.Description);
                if (feeMapping != null)
                {
                    invoiceRow.feesAmount = Math.Abs(x.Amount - (x.VATAmount ?? 0));
                    invoiceRow.feesAmountSpecified = true;
                    invoiceRow.feesPercentCategory = feeMapping.Code;
                    invoiceRow.feesPercentCategorySpecified = true;
                }
            }
            if (grouped.modifiers.Count > 0)
            {
                invoiceRow.deductionsAmount = grouped.modifiers.Sum(x => x.Amount) * -1;
                invoiceRow.deductionsAmountSpecified = true;
            }
            // Apply line-level mydataoverride from ftChargeItemCaseData
            if (x.ftChargeItemCaseData != null)
            {
                try
                {
                    var chargeItemData = JsonSerializer.Deserialize<ftChargeItemCaseDataPayload>(
                        JsonSerializer.Serialize(x.ftChargeItemCaseData),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (chargeItemData?.GR?.MyDataOverride?.InvoiceDetails != null)
                    {
                        ApplyInvoiceDetailOverride(invoiceRow, chargeItemData.GR.MyDataOverride.InvoiceDetails);
                    }
                }
                catch (JsonException)
                {
                    // ftChargeItemCaseData may contain data for other purposes, ignore deserialization errors
                }
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

        var vatNumber = GetAADEVAT(customer.CustomerVATId);
        var isDomestic = countryType == CountryType.GR;

        var party = new PartyType
        {
            vatNumber = vatNumber,
            country = countryType,
            branch = 0,
        };

        if (!isDomestic || receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsGR.HasTransportInformation))
        {
            party.name = customer.CustomerName;
        }

        var hasAddress = !string.IsNullOrEmpty(customer.CustomerZip) && !string.IsNullOrEmpty(customer.CustomerCity);
        if (hasAddress)
        {
            party.address = new AddressType
            {
                postalCode = customer.CustomerZip,
                city = customer.CustomerCity,
                street = !string.IsNullOrEmpty(customer.CustomerStreet) ? customer.CustomerStreet : null,
                number = !string.IsNullOrEmpty(customer.CustomerHouseNumber) ? customer.CustomerHouseNumber : (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsGR.HasTransportInformation) ? "0" : null)
            };
        }

        return party;
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

    /// <summary>
    /// Sets AADE-required fields for Invoice Type 8.6 (VOID/cancel) restaurant order:
    /// - tableAA (area/table number)
    /// - totalCancelDeliveryOrders = true
    /// Note: multipleConnectedMarks are set by the general flow in CreateInvoiceDocType
    /// using resolved receiptReferences.
    /// </summary>
    public static void SetInvoiceHeaderFieldsForVoid(InvoiceHeaderType invoiceHeader, ReceiptRequest receiptRequest)
    {
        // Validate cbPreviousReceiptReference is present and not empty
        var refObj = receiptRequest.cbPreviousReceiptReference;
        if (refObj == null)
        {
            throw new ArgumentException("cbPreviousReceiptReference must not be null or empty.", nameof(receiptRequest.cbPreviousReceiptReference));
        }

        refObj.Match(
            single =>
            {
                if (string.IsNullOrWhiteSpace(single))
                {
                    throw new ArgumentException("Single MARK value cannot be empty.", nameof(receiptRequest.cbPreviousReceiptReference));
                }
            },
            group =>
            {
                if (group == null || group.Length == 0)
                {
                    throw new ArgumentException("Group MARKs cannot be empty.", nameof(receiptRequest.cbPreviousReceiptReference));
                }
            }
        );

        // TableAA (mandatory for 8.6)
        if (receiptRequest.cbArea == null)
        {
            throw new ArgumentException("TableAA (cbArea) must be provided for restaurant order VOID (8.6).", nameof(receiptRequest.cbArea));
        }

        invoiceHeader.tableAA = Convert.ToString(receiptRequest.cbArea, CultureInfo.InvariantCulture);
        invoiceHeader.totalCancelDeliveryOrders = true;
        invoiceHeader.totalCancelDeliveryOrdersSpecified = true;
    }
    public string GetUid(AadeBookInvoiceType invoice) => BitConverter.ToString(SHA1.HashData(Encoding.UTF8.GetBytes($"{invoice.issuer.vatNumber}-{invoice.invoiceHeader.issueDate.ToString("yyyy-MM-dd")}-{invoice.issuer.branch}-{invoice.invoiceHeader.invoiceType.GetXmlEnumAttributeValueFromEnum() ?? ""}-{invoice.invoiceHeader.series}-{invoice.invoiceHeader.aa}"))).Replace("-", "");

    public (PaymentMethodsDoc? paymentMethodsDoc, AADEFactoryError? error) MapToPaymentMethodsDoc(ReceiptRequest receiptRequest, long invoiceMark, string? entityVatNumber = null)
    {
        try
        {
            foreach (var payItem in receiptRequest.cbPayItems)
            {
                payItem.Amount = Math.Round(payItem.Amount, 2);
                payItem.Quantity = Math.Round(payItem.Quantity, 2);
            }

            var paymentMethodDetails = GetPayments(receiptRequest);
            if (paymentMethodDetails == null || paymentMethodDetails.Count == 0)
            {
                throw new Exception("At least one payment method detail is required for SendPaymentsMethod.");
            }

            var paymentMethod = new PaymentMethodType
            {
                invoiceMark = invoiceMark,
                paymentMethodDetails = paymentMethodDetails.ToArray()
            };

            if (!string.IsNullOrEmpty(entityVatNumber))
            {
                paymentMethod.entityVatNumber = entityVatNumber;
            }

            var doc = new PaymentMethodsDoc
            {
                paymentMethods = [paymentMethod]
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

    public static string GeneratePaymentMethodPayload(PaymentMethodsDoc doc)
    {
        var xmlSerializer = new XmlSerializer(typeof(PaymentMethodsDoc));
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
