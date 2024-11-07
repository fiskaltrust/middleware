using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE.Models;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.myDataSCU;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

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
        if (receiptRequest.cbChargeItems.Any(x => x.IsAgencyBusiness()) && !receiptRequest.cbChargeItems.All(x => x.IsAgencyBusiness()))
        {
            throw new Exception("It is not allowed to mix agency and non agency receipts.");
        }
        
        if(receiptRequest.cbChargeItems.Sum(x => x.Amount) != receiptRequest.cbPayItems.Sum(x => x.Amount))
        {
            throw new Exception("The sum of the charge items must be equal to the sum of the pay items.");
        }
    }

    public InvoicesDoc MapToInvoicesDoc(List<ftQueueItem> queueItems)
    {
        var receiptRequests = queueItems.Where(x => !string.IsNullOrEmpty(x.request) && !string.IsNullOrEmpty(x.response)).Select(x => (receiptRequest: JsonSerializer.Deserialize<ReceiptRequest>(x.request)!, receiptResponse: JsonSerializer.Deserialize<ReceiptResponse>(x.response))).ToList();
        var actualReceiptRequests = receiptRequests.Where(x => x.receiptResponse != null && ((long) x.receiptResponse.ftState & 0xFF) == 0x00).Cast<(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)>().ToList();
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

        var expensesClassificationGroups = invoiceDetails.Where(x => x.expensesClassification != null).SelectMany(x => x.expensesClassification).Where(x => x.classificationTypeSpecified).GroupBy(x => (x.classificationCategory, x.classificationType)).Select(x => new ExpensesClassificationType
        {
            amount = x.Sum(y => y.amount),
            classificationCategory = x.Key.classificationCategory,
            classificationType = x.Key.classificationType,
            classificationTypeSpecified = true
        }).ToList();
        expensesClassificationGroups.AddRange(invoiceDetails.Where(x => x.expensesClassification != null).SelectMany(x => x.expensesClassification).Where(x => !x.classificationTypeSpecified).GroupBy(x => x.classificationCategory).Select(x => new ExpensesClassificationType
        {
            amount = x.Sum(y => y.amount),
            classificationCategorySpecified = true,
            classificationCategory = x.Key,
        }).ToList());

        var identification = long.Parse(receiptResponse.ftReceiptIdentification.Replace("ft", "").Split("#")[0], System.Globalization.NumberStyles.HexNumber);
        var paymentMethods = GetPayments(receiptRequest);
        var inv = new AadeBookInvoiceType
        {
            issuer = CreateIssuer(),
            paymentMethods = [.. paymentMethods],
            invoiceHeader = new InvoiceHeaderType
            {
                series = "0",
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
        inv.invoiceSummary.totalGrossValue = inv.invoiceSummary.totalNetValue + inv.invoiceSummary.totalVatAmount + inv.invoiceSummary.totalWithheldAmount + inv.invoiceSummary.totalFeesAmount + inv.invoiceSummary.totalStampDutyAmount + inv.invoiceSummary.totalOtherTaxesAmount - inv.invoiceSummary.totalDeductionsAmount;
        if (!string.IsNullOrEmpty(receiptRequest.cbPreviousReceiptReference))
        {
            inv.invoiceHeader.correlatedInvoices = [long.Parse(receiptRequest.cbPreviousReceiptReference)];
        }
        AddCounterpart(receiptRequest, inv);
        SetValuesIfExistent(receiptResponse, inv);
        return inv;
    }

    private static List<InvoiceRowType> GetInvoiceDetails(ReceiptRequest receiptRequest)
    {
        return receiptRequest.cbChargeItems.Select(x =>
        {
            var vatAmount = x.GetVATAmount();
            var invoiceRow = new InvoiceRowType
            {
                quantity = receiptRequest.IsRefund() ? -x.Quantity : x.Quantity,
                lineNumber = (int) x.Position,
                vatAmount = receiptRequest.IsRefund() ? -vatAmount : vatAmount,
                netValue = receiptRequest.IsRefund() ? (-x.Amount - -vatAmount) : x.Amount - vatAmount,
                vatCategory = AADEMappings.GetVATCategory(x),
            };

            if ((x.ftChargeItemCase & 0xFF00) == NatureExemptions.EndOfClimateCrisesNature)
            {
                invoiceRow.netValue = 0;
                invoiceRow.otherTaxesAmount = x.Amount;
                invoiceRow.otherTaxesAmountSpecified = true;
                invoiceRow.otherTaxesPercentCategory = 9;
                invoiceRow.otherTaxesPercentCategorySpecified = true;
                invoiceRow.incomeClassification = [];
                invoiceRow.vatCategory = 8;
            }
            else
            {
                if (receiptRequest.cbChargeItems.Any(x => (x.ftChargeItemCase & 0xF0) == 0x90))
                {
                    if (receiptRequest.cbChargeItems.Any(x => (x.ftChargeItemCase & 0xF0) == 0x90) && (x.ftChargeItemCase & 0xF0) != 0x90)
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
                    else if ((x.ftChargeItemCase & 0xF0) == 0x90)
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
                if (chargeItem != null)
                {
                    invoiceRow.withheldAmountSpecified = true;
                    invoiceRow.withheldAmount = chargeItem.WithHoldingAmount;
                    invoiceRow.withheldPercentCategory = 3;
                    invoiceRow.withheldPercentCategorySpecified = true;
                }
            }
            return invoiceRow;
        }).ToList();
    }

    private static void SetValuesIfExistent(ReceiptResponse receiptResponse, AadeBookInvoiceType inv)
    {
        if (receiptResponse.ftSignatures.Count > 0)
        {
            var invoiceUid = receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "invoiceUid")?.Data;
            var invoiceMarkText = receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "invoiceMark")?.Data;
            var authenticationCode = receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "authenticationCode")?.Data;
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
        }
    }

    private static List<PaymentMethodDetailType> GetPayments(ReceiptRequest receiptRequest)
    {
        return receiptRequest.cbPayItems.Where(x => (x.ftPayItemCase & ((long) 0xFF)) != 0x99).Select(x =>
        {
            var payment = new PaymentMethodDetailType
            {
                type = AADEMappings.GetPaymentType(x),
                amount = receiptRequest.IsRefund() ? -x.Amount : x.Amount,
                paymentMethodInfo = x.Description,
            };
            if (x.ftPayItemCaseData != null)
            {
                var provider = JsonSerializer.Deserialize<PayItemCaseData>(JsonSerializer.Serialize(x.ftPayItemCaseData))!;
                if (provider.Provider is PayItemCaseProviderVivaWallet vivaPayment)
                {

                    payment.transactionId = vivaPayment.ProtocolResponse?.aadeTransactionId;
                    payment.ProvidersSignature = new ProviderSignatureType
                    {
                        Signature = vivaPayment.ProtocolRequest?.aadeProviderSignature,
                        SigningAuthor = "viva.com", // need to be filled??
                    };
                }
            }
            return payment;
        }).ToList();
    }

    private static void AddCounterpart(ReceiptRequest receiptRequest, AadeBookInvoiceType inv)
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
        if (receiptRequest.HasGreeceCustomer())
        {
            inv.counterpart = new PartyType
            {
                vatNumber = customer?.CustomerVATId,
                country = CountryType.GR,
                branch = 0,
            };
        }
        else if (receiptRequest.HasEUCustomer())
        {
            inv.counterpart = new PartyType
            {
                vatNumber = customer?.CustomerVATId,
                country = CountryType.AT,
                name = customer?.CustomerName,
                address = new AddressType
                {
                    street = customer?.CustomerStreet,
                    city = customer?.CustomerCity,
                    postalCode = customer?.CustomerZip
                },
                branch = 0,
            };
        }
        else if (receiptRequest.HasNonEUCustomer())
        {
            inv.counterpart = new PartyType
            {
                vatNumber = customer?.CustomerVATId,
                country = CountryType.US,
                name = customer?.CustomerName,
                address = new AddressType
                {
                    street = customer?.CustomerStreet,
                    city = customer?.CustomerCity,
                    postalCode = customer?.CustomerZip
                },
                branch = 0,
            };
        }
    }

    private PartyType CreateIssuer()
    {
        return new PartyType
        {
            vatNumber = _masterDataConfiguration.Account.VatId,
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
