﻿using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Xml.Serialization;
using Azure.Core;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData.Models;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData;

public class AADEFactory
{
    private const string VIVA_FISCAL_PROVIDER_ID = "126";

    private readonly MasterDataConfiguration _masterDataConfiguration;

    public AADEFactory(MasterDataConfiguration masterDataConfiguration)
    {
        _masterDataConfiguration = masterDataConfiguration;
    }

    public InvoicesDoc MapToInvoicesDoc(List<ftQueueItem> queueItems)
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
        return doc;
    }

    public InvoicesDoc MapToInvoicesDoc(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, List<(ReceiptRequest, ReceiptResponse)>? receiptReferences = null)
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
        MyDataAADEValidation.ValidateReceiptRequest(receiptRequest);

        var inv = CreateInvoiceDocType(receiptRequest, receiptResponse, receiptReferences);
        var doc = new InvoicesDoc
        {
            invoice = [inv]
        };
        return doc;
    }

    private AadeBookInvoiceType CreateInvoiceDocType(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, List<(ReceiptRequest, ReceiptResponse)>? receiptReferences = null)
    {
        var invoiceDetails = GetInvoiceDetails(receiptRequest);
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
        var issuer = CreateIssuer();
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
                totalWithheldAmount = invoiceDetails.Sum(x => x.withheldAmount),
                totalFeesAmount = invoiceDetails.Sum(x => x.feesAmount),
                totalStampDutyAmount = invoiceDetails.Sum(x => x.stampDutyAmount),
                totalOtherTaxesAmount = invoiceDetails.Sum(x => x.otherTaxesAmount),
                totalDeductionsAmount = invoiceDetails.Sum(x => x.deductionsAmount),
                incomeClassification = [.. incomeClassificationGroups],
                expensesClassification = [.. expensesClassificationGroups],
            }
        };

        if (paymentMethods?.Count > 0)
        {
            inv.paymentMethods = [.. paymentMethods];
        }

        if (receiptRequest.cbPreviousReceiptReference is not null && receiptReferences?.Count > 0)
        {
            if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
            {
                inv.invoiceHeader.correlatedInvoices = receiptReferences.Select(x => GetInvoiceMark(x.Item2)).ToArray();
            }
            else
            {
                inv.invoiceHeader.multipleConnectedMarks = receiptReferences.Select(x => GetInvoiceMark(x.Item2)).ToArray();
            }
        }

        if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.Order0x3004))
        {
            inv.invoiceHeader.tableAA = receiptRequest.cbArea?.ToString();
        }

        if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.DeliveryNote0x0005))
        {
            var result = receiptRequest.GetCustomerOrNull();
            if (result != null)
            {
                inv.invoiceHeader.otherDeliveryNoteHeader = new OtherDeliveryNoteHeaderType
                {
                    deliveryAddress = new AddressType
                    {
                        street = result.CustomerStreet,
                        city = result.CustomerCity,
                        postalCode = result.CustomerZip
                    },
                    loadingAddress = new AddressType
                    {
                        street = _masterDataConfiguration.Outlet.Street,
                        city = _masterDataConfiguration.Outlet.City,
                        postalCode = _masterDataConfiguration.Outlet.Zip,
                        number = _masterDataConfiguration.Outlet.LocationId,
                    }
                };
            }
        }

        inv.invoiceSummary.totalGrossValue = inv.invoiceSummary.totalNetValue + inv.invoiceSummary.totalVatAmount - inv.invoiceSummary.totalWithheldAmount + inv.invoiceSummary.totalFeesAmount + inv.invoiceSummary.totalStampDutyAmount + inv.invoiceSummary.totalOtherTaxesAmount - inv.invoiceSummary.totalDeductionsAmount;

        if (receiptRequest.ContainsCustomerInfo())
        {
            AddCounterpart(receiptRequest, inv);
        }
        SetValuesIfExistent(receiptRequest, receiptResponse, inv);
        return inv;
    }

    private static List<InvoiceRowType> GetInvoiceDetails(ReceiptRequest receiptRequest)
    {
        var chargeItems = receiptRequest.GetGroupedChargeItems();
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
                vatCategory = AADEMappings.GetVATCategory(x),
            };

            if (AADEMappings.GetInvoiceType(receiptRequest) == InvoiceType.Item86)
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

            if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.Order0x3004))
            {
                // Todo change
                invoiceRow.itemDescr = x.Description;
                invoiceRow.measurementUnit = 1;
                invoiceRow.measurementUnitSpecified = true;
            }

            if (x.ftChargeItemCase.IsVat(ChargeItemCase.NotTaxable) || x.ftChargeItemCase.IsVat(ChargeItemCase.ZeroVatRate))
            {
                if (x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.Voucher))
                {
                    invoiceRow.vatExemptionCategorySpecified = true;
                    invoiceRow.vatExemptionCategory = 27;
                }
                else
                {
                    invoiceRow.vatExemptionCategorySpecified = true;
                    var exemptionCategory = AADEMappings.GetVatExemptionCategory(x);
                    if (exemptionCategory.HasValue)
                    {
                        invoiceRow.vatExemptionCategorySpecified = true;
                        invoiceRow.vatExemptionCategory = exemptionCategory.Value;
                    }
                    else
                    {
                        throw new Exception($"The VAT exemption for the given Nature 0x{x.ftChargeItemCase.NatureOfVat():x}  is not supported.");
                    }
                }
            }
            else if (x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.Voucher))
            {
                invoiceRow.recType = 6;
            }
            else if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsGR.IsSelfPricingOperation))
            {
                if (invoiceRow.vatCategory == MyDataVatCategory.ExcludingVat)
                {
                    invoiceRow.vatExemptionCategorySpecified = true;
                    invoiceRow.vatExemptionCategory = MyDataVatExemptionCategory.GeneralExemption;
                }

                if (receiptRequest.cbChargeItems.Any(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.Receivable)))
                {
                    // original line as follows:
                    // if (receiptRequest.cbChargeItems.Any(x => (x.ftChargeItemCase & 0xF0) == 0x90) && (x.ftChargeItemCase & 0xF0) != 0x90)
                    // I've left the logic the same but I don't think it's meant that way. there are two different x that shadow each other.
                    if (receiptRequest.cbChargeItems.Any(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.Receivable) && !x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.Receivable)))
                    {
                        invoiceRow.invoiceDetailType = 2;
                        invoiceRow.invoiceDetailTypeSpecified = true;
                        invoiceRow.incomeClassification = [];
                        invoiceRow.expensesClassification = [
                           new ExpensesClassificationType {
                                                        amount = invoiceRow.netValue,
                                                        classificationCategorySpecified = true,

                                                        classificationCategory = ExpensesClassificationCategoryType.category2_9
                                                    }
                            ];
                    }
                    else if (x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.Receivable))
                    {
                        invoiceRow.invoiceDetailType = 1;
                        invoiceRow.invoiceDetailTypeSpecified = true;
                        invoiceRow.expensesClassification = [];
                    }
                }
                else
                {
                    invoiceRow.expensesClassification = [
                        new ExpensesClassificationType {
                                                amount = invoiceRow.netValue,
                                                classificationCategorySpecified = true,
                                                classificationType = ExpensesClassificationTypeClassificationType.E3_102_001,
                                                classificationTypeSpecified = true,
                                                classificationCategory = ExpensesClassificationCategoryType.category2_1
                                            },
                        new ExpensesClassificationType {
                                                amount = invoiceRow.netValue,
                                                classificationType = ExpensesClassificationTypeClassificationType.VAT_361,
                                                classificationTypeSpecified = true
                                            },
                    ];
                }
            }
            else if (receiptRequest.ftReceiptCase.Case() == ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003)
            {
                invoiceRow.incomeClassification = [];
            }
            else
            {
                // same as above
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
            if (x.ftChargeItemCaseData != null)
            {
                var chargeItem = JsonSerializer.Deserialize<WithHoldingChargeItem>(JsonSerializer.Serialize(x.ftChargeItemCaseData), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (chargeItem != null && chargeItem.WithHoldingAmount != default && chargeItem.WithHoldingAmount != default)
                {
                    invoiceRow.withheldAmountSpecified = true;
                    invoiceRow.withheldAmount = chargeItem.WithHoldingAmount;
                    invoiceRow.withheldPercentCategory = 3;
                    invoiceRow.withheldPercentCategorySpecified = true;
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

    private static void SetValuesIfExistent(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, AadeBookInvoiceType inv)
    {
        if (receiptResponse.ftSignatures.Count > 0)
        {
            var invoiceUid = receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "invoiceUid")?.Data;
            var invoiceMarkText = receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "invoiceMark")?.Data;
            var authenticationCode = receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "authenticationCode")?.Data;
            var qrCode = receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "authenticationCode")?.Data;
            if (long.TryParse(invoiceMarkText, out var invoiceMark))
            {
                inv.uid = invoiceUid;
                inv.authenticationCode = authenticationCode;
                inv.mark = invoiceMark;
                inv.markSpecified = true;
                inv.qrCodeUrl = $"https://receipts-sandbox.fiskaltrust.eu/{receiptResponse.ftQueueID}/{receiptResponse.ftQueueItemID}";
            }
            else
            {
                invoiceMark = -1;
            }

            if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.LateSigning))
            {
                inv.transmissionFailureSpecified = true;
                inv.transmissionFailure = 1;
            }
            var transmissionFailure1 = receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "Transmission Failure_1")?.Data;
            if (transmissionFailure1 != null)
            {

            }

            var transmissionFailure2 = receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "Transmission Failure_2")?.Data;
            if (transmissionFailure2 != null)
            {
                inv.transmissionFailureSpecified = true;
                inv.transmissionFailure = 2;
            }
        }
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

    private void AddCounterpart(ReceiptRequest receiptRequest, AadeBookInvoiceType inv)
    {
        var customer = receiptRequest.GetCustomerOrNull();
        if (receiptRequest.HasGreeceCountryCode())
        {
            if (customer?.CustomerVATId?.StartsWith("EL") == true)
            {
                inv.counterpart = new PartyType
                {
                    vatNumber = customer?.CustomerVATId.Replace("EL", ""),
                    country = CountryType.GR,
                    branch = 0,
                };
            }
            else if (customer?.CustomerVATId?.StartsWith("GR") == true)
            {
                inv.counterpart = new PartyType
                {
                    vatNumber = customer?.CustomerVATId.Replace("GR", ""),
                    country = CountryType.GR,
                    branch = 0,
                };
            }
            else
            {
                inv.counterpart = new PartyType
                {
                    vatNumber = customer?.CustomerVATId,
                    country = CountryType.GR,
                    branch = 0,
                };
            }

            if (receiptRequest.ftReceiptCase.Case() == ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003 || inv.invoiceHeader.invoiceType == InvoiceType.Item14 || inv.invoiceHeader.invoiceType == InvoiceType.Item71)
            {
                inv.counterpart.address = new AddressType
                {
                    street = customer?.CustomerStreet,
                    city = customer?.CustomerCity,
                    postalCode = customer?.CustomerZip
                };
            }
        }
        else if (receiptRequest.HasEUCountryCode())
        {
            throw new Exception("Inter-Community invoices are not supported");
        }
        else if (receiptRequest.HasNonEUCountryCode())
        {
            throw new Exception("Intra-Community invoices are not supported");
        }
    }

    private PartyType CreateIssuer()
    {
        var issuerVat = _masterDataConfiguration?.Account?.VatId ?? "112545020";
        var branch = 0;
        if (!string.IsNullOrEmpty(_masterDataConfiguration?.Outlet?.LocationId) && int.TryParse(_masterDataConfiguration?.Outlet?.LocationId, out var locationId))
        {
            branch = locationId;
        }
        if (issuerVat?.StartsWith("EL") == true)
        {
            issuerVat = issuerVat.Replace("EL", "");
        }
        else if (issuerVat?.StartsWith("GR") == true)
        {
            issuerVat = issuerVat.Replace("GR", "");
        }
        return new PartyType
        {
            vatNumber = issuerVat,
            country = CountryType.GR,
            branch = branch
        };
    }

    public string GetUid(AadeBookInvoiceType invoice) => BitConverter.ToString(SHA1.HashData(Encoding.UTF8.GetBytes($"{invoice.issuer.vatNumber}-{invoice.invoiceHeader.issueDate.ToString("yyyy-MM-dd")}-{invoice.issuer.branch}-{invoice.invoiceHeader.invoiceType.GetXmlEnumAttributeValueFromEnum() ?? ""}-{invoice.invoiceHeader.series}-{invoice.invoiceHeader.aa}"))).Replace("-", "");

    public string GenerateInvoicePayload(InvoicesDoc doc)
    {
        var xmlSerializer = new XmlSerializer(typeof(InvoicesDoc));
        using var stringWriter = new StringWriter();
        xmlSerializer.Serialize(stringWriter, doc);
        var xmlContent = stringWriter.ToString();
        return xmlContent;
    }
}
