using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Xml.Serialization;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE.Models;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.myDataSCU;
using fiskaltrust.Middleware.Localization.QueueGR.Interface;
using fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using fiskaltrust.Middleware.Localization.QueueGR.Extensions; 

namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE;

public class AADEFactory
{
    private readonly MasterDataConfiguration _masterDataConfiguration;

    public AADEFactory(MasterDataConfiguration masterDataConfiguration)
    {
        _masterDataConfiguration = masterDataConfiguration;
    }

    public void ValidateReceiptRequest(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.cbChargeItems.Any(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.NotOwnSales)) && !receiptRequest.cbChargeItems.All(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.NotOwnSales)))
        {
            throw new Exception("It is not allowed to mix agency and non agency receipts.");
        }

        if (receiptRequest.cbChargeItems.Sum(x => x.Amount) != receiptRequest.cbPayItems.Sum(x => x.Amount))
        {
            throw new Exception("The sum of the charge items must be equal to the sum of the pay items.");
        }
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

    public InvoicesDoc MapToInvoicesDoc(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        ValidateReceiptRequest(receiptRequest);

        var inv = CreateInvoiceDocType(receiptRequest, receiptResponse);
        var doc = new InvoicesDoc
        {
            invoice = [inv]
        };
        return doc;
    }

    private AadeBookInvoiceType CreateInvoiceDocType(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
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
        //if (receiptRequest.IsSelfPricingOperation())
        //{

        //    var customer = receiptRequest.GetCustomerOrNull();
        //    issuer = new PartyType
        //    {
        //        vatNumber = customer?.CustomerVATId,
        //        country = CountryType.GR,
        //        branch = 0,
        //    };
        //}

        var inv = new AadeBookInvoiceType
        {
            issuer = issuer,
            paymentMethods = [.. paymentMethods],
            invoiceHeader = new InvoiceHeaderType
            {
                series = "0",
                aa = identification.ToString(),
                issueDate = receiptRequest.cbReceiptMoment,
                invoiceType = AADEMappings.GetInvoiceType(receiptRequest),
                selfPricing = receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsGR.IsSelfPricingOperation),
                selfPricingSpecified = receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsGR.IsSelfPricingOperation),
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
        inv.invoiceSummary.totalGrossValue = inv.invoiceSummary.totalNetValue + inv.invoiceSummary.totalVatAmount - inv.invoiceSummary.totalWithheldAmount + inv.invoiceSummary.totalFeesAmount + inv.invoiceSummary.totalStampDutyAmount + inv.invoiceSummary.totalOtherTaxesAmount - inv.invoiceSummary.totalDeductionsAmount;
        if (!string.IsNullOrEmpty(receiptRequest.cbPreviousReceiptReference))
        {
            inv.invoiceHeader.correlatedInvoices = [long.Parse(receiptRequest.cbPreviousReceiptReference)];
        }
        AddCounterpart(receiptRequest, inv);
        SetValuesIfExistent(receiptRequest, receiptResponse, inv);
        return inv;
    }

    private static List<InvoiceRowType> GetInvoiceDetails(ReceiptRequest receiptRequest)
    {
        var chargeItems = receiptRequest.GetGroupedChargeItems();

        return chargeItems.Select(grouped =>
        {
            var x = grouped.chargeItem;

            var vatAmount = x.GetVATAmount();
            var invoiceRow = new InvoiceRowType
            {
                quantity = receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) ? -x.Quantity : x.Quantity,
                lineNumber = (int) x.Position,
                vatAmount = receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) ? -vatAmount : vatAmount,
                netValue = receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) ? (-x.Amount - -vatAmount) : x.Amount - vatAmount,
                vatCategory = AADEMappings.GetVATCategory(x),
            };           
            if (x.ftChargeItemCase.IsNatureOfVat(ChargeItemCaseNatureOfVatGR.ExtemptEndOfClimateCrises))
            {
                invoiceRow.netValue = 0;
                invoiceRow.otherTaxesAmount = x.Amount;
                invoiceRow.otherTaxesAmountSpecified = true;
                invoiceRow.otherTaxesPercentCategory = 9;
                invoiceRow.otherTaxesPercentCategorySpecified = true;
                invoiceRow.incomeClassification = [];
                invoiceRow.vatCategory = 8;
            }
            else if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsGR.IsSelfPricingOperation))
            {
                if (invoiceRow.vatCategory == MyDataVatCategory.ExcludingVat)
                {
                    invoiceRow.vatExemptionCategorySpecified = true;
                    invoiceRow.vatExemptionCategory = 1;
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
                    if (invoiceRow.vatCategory == MyDataVatCategory.ExcludingVat)
                    {
                        invoiceRow.vatExemptionCategorySpecified = true;
                        invoiceRow.vatExemptionCategory = 1;
                    }
                    invoiceRow.incomeClassification = [AADEMappings.GetIncomeClassificationType(receiptRequest, x)];
                }
            }
            if (x.ftChargeItemCaseData != null)
            {
                var chargeItem = JsonSerializer.Deserialize<WithHoldingChargeItem>(JsonSerializer.Serialize(x.ftChargeItemCaseData));
                if (chargeItem != null && chargeItem.WithHoldingAmount != default && chargeItem.WithHoldingAmount != default)
                {
                    invoiceRow.withheldAmountSpecified = true;
                    invoiceRow.withheldAmount = chargeItem.WithHoldingAmount;
                    invoiceRow.withheldPercentCategory = 3;
                    invoiceRow.withheldPercentCategorySpecified = true;
                }
            }
            if(grouped.modifiers.Count > 0)
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

    private static List<PaymentMethodDetailType> GetPayments(ReceiptRequest receiptRequest)
    {
        // what is payitemcase 99?
        return receiptRequest.cbPayItems
            .Where(x => (int) x.ftPayItemCase.Case() != 0x99)
            .Where(x => !(x.ftPayItemCase.IsCase(PayItemCase.Grant) && x.ftPayItemCase.IsFlag(PayItemCaseFlags.Tip)) && !(x.ftPayItemCase.IsCase(PayItemCase.DebitCardPayment) && x.ftPayItemCase.IsFlag(PayItemCaseFlags.Tip))).Select(x =>
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
                var providerData = JsonSerializer.Deserialize<GenericPaymentPayload>(JsonSerializer.Serialize(x.ftPayItemCaseData));
                if (providerData != null && providerData.Provider != null && providerData.Provider.ProtocolRequest is JsonElement dat && dat.ValueKind == JsonValueKind.String)
                {
                    var app2AppApi = JsonSerializer.Deserialize<PayItemCaseDataApp2App>(JsonSerializer.Serialize(x.ftPayItemCaseData))!;
                    if (app2AppApi.Provider is PayItemCaseProviderVivaWalletApp2APp vivaAppToApp)
                    {
                        var requestUri = HttpUtility.ParseQueryString(new Uri(vivaAppToApp.ProtocolRequest).Query);
                        var responesUri = HttpUtility.ParseQueryString(new Uri(vivaAppToApp.ProtocolResponse).Query);
                        payment.transactionId = responesUri["aadeTransactionId"];

                        payment.ProvidersSignature = new ProviderSignatureType
                        {
                            Signature = requestUri["aadeProviderSignature"],
                            SigningAuthor = "viva.com", // need to be filled??
                        };
                    }
                }
                else if (providerData != null && providerData.Provider != null && providerData.Provider.ProtocolRequest is JsonElement datS && datS.ValueKind == JsonValueKind.Object)
                {
                    var providerCloudRestApi = JsonSerializer.Deserialize<PayItemCaseDataCloudApi>(JsonSerializer.Serialize(x.ftPayItemCaseData))!;
                    if (providerCloudRestApi.Provider is PayItemCaseProviderVivaWallet vivaPayment)
                    {

                        payment.transactionId = vivaPayment.ProtocolResponse?.aadeTransactionId;
                        payment.ProvidersSignature = new ProviderSignatureType
                        {
                            Signature = vivaPayment.ProtocolRequest?.aadeProviderSignature,
                            SigningAuthor = "viva.com", // need to be filled??
                        };
                    }
                }
            }
            return payment;
        }).ToList();
    }

    private void AddCounterpart(ReceiptRequest receiptRequest, AadeBookInvoiceType inv)
    {
        if (!receiptRequest.ContainsCustomerInfo())
        {
            if (AADEMappings.RequiresCustomerInfo(inv.invoiceHeader.invoiceType))
            {
                throw new Exception("Customer info is required for this invoice type");
            }
            return;
        }

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

            inv.counterpart = new PartyType
            {
                vatNumber = customer?.CustomerVATId,
                country = customer?.CustomerCountry == "GR" ? CountryType.GR : CountryType.AT,
                name = customer?.CustomerName,
                address = new AddressType
                {
                    //number = "0",
                    street = customer?.CustomerStreet,
                    city = customer?.CustomerCity,
                    postalCode = customer?.CustomerZip
                },
                branch = 0,
            };
        }
        else if (receiptRequest.HasNonEUCountryCode())
        {
            inv.counterpart = new PartyType
            {
                vatNumber = customer?.CustomerVATId,
                country = CountryType.GB,
                name = customer?.CustomerName,
                address = new AddressType
                {
                    street = customer?.CustomerStreet?.Replace(", United Kingdom", ""),
                    city = customer?.CustomerCity?.Replace(", United Kingdom", ""),
                    postalCode = customer?.CustomerZip
                },
                branch = 0,
            };
        }
    }

    private PartyType CreateIssuer()
    {
        var issuerVat = _masterDataConfiguration.Account.VatId ?? "112545020";
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
            branch = 0,
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
