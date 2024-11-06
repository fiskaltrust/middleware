using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.myDataSCU;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.SAFT.CLI;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE
{
    public class WithHoldingChargeItem
    {
        public decimal WithHoldingPercentage { get; set; }
        public decimal WithHoldingAmount { get; set; }
    }

    public class AADEFactory
    {
        private readonly MasterDataConfiguration _masterDataConfiguration;

        public AADEFactory(MasterDataConfiguration masterDataConfiguration)
        {
            _masterDataConfiguration = masterDataConfiguration;
        }

        private IncomeClassificationValueType GetIncomeClassificationValueTypeForInvoice(ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0xF0) switch
        {
            0x00 => IncomeClassificationValueType.E3_561_001,
            0x10 => IncomeClassificationValueType.E3_561_001,
            0x20 => IncomeClassificationValueType.E3_561_001,
            _ => IncomeClassificationValueType.E3_561_007,
        };

        private IncomeClassificationValueType GetIncomeClassificationValueTypeForPrivate(ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0xF0) switch
        {
            0x00 => IncomeClassificationValueType.E3_561_003,
            0x10 => IncomeClassificationValueType.E3_561_003,
            0x20 => IncomeClassificationValueType.E3_561_003,
            _ => IncomeClassificationValueType.E3_561_007,
        };

        /// <summary>
        /// The following income classifications belong to myDATA API
        /// 
        /// category1_1 => Revenue from Sales of Goods (+ / -)
        /// category1_2 => Revenue from Sales of Products (+ / -)
        /// category1_3 => Revenue from Sales of Services (+ / -)
        /// category1_4 => Proceeds from Sale of Assets (+ / -)
        /// category1_5 => Other income/profit (+ / -)
        /// category1_6 => Self-delivery / Self-use (+ / -)
        /// category1_7 => Revenue for third parties (+ / -)
        /// category1_8 => Revenue from previous years (+ / -)
        /// category1_9 => Deferred income (+ / -)
        /// category1_10 => Other revenue adjustment entries (+ / -)
        /// category1_95 => Other revenue Information (+ / -)
        /// category3 => Movement        
        /// </summary>

        private IncomeClassificationCategoryType GetIncomeClassificationCategoryType(ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0xF0) switch
        {
            0x00 => IncomeClassificationCategoryType.category1_2,
            0x10 => IncomeClassificationCategoryType.category1_2,
            0x20 => IncomeClassificationCategoryType.category1_3,
            _ => IncomeClassificationCategoryType.category1_2,
        };

        private int GetVATCategory(ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0x0F) switch
        {
            (long) ChargeItemCaseVat.NormalVatRate => MyDataVatCategory.VatRate24, // Normal 24%
            (long) ChargeItemCaseVat.DiscountedVatRate1 => MyDataVatCategory.VatRate13, // Discounted-1 13&
            (long) ChargeItemCaseVat.DiscountedVatRate2 => MyDataVatCategory.VatRate6, // Discounted-2 6%
            (long) ChargeItemCaseVat.SuperReducedVatRate1 => MyDataVatCategory.VatRate17, // Super reduced 1 17%
            (long) ChargeItemCaseVat.SuperReducedVatRate2 => MyDataVatCategory.VatRate9, // Super reduced 2 9%
            (long) ChargeItemCaseVat.ParkingVatRate => MyDataVatCategory.VatRate4, // Parking VAT 4%
            (long) ChargeItemCaseVat.NotTaxable => MyDataVatCategory.RegistrationsWithoutVat, // Not Taxable
            (long) ChargeItemCaseVat.ZeroVatRate => MyDataVatCategory.ExcludingVat, // Zero
            _ => throw new Exception($"The VAT type {chargeItem.ftChargeItemCase & 0xF} of ChargeItem with the case {chargeItem.ftChargeItemCase} is not supported."),
        };

        private int GetPaymentType(PayItem payItem) => (payItem.ftPayItemCase & 0xF) switch
        {
            (long) PayItemCases.UnknownPaymentType => MyDataPaymentMethods.Cash,
            (long) PayItemCases.CashPayment => MyDataPaymentMethods.Cash,
            (long) PayItemCases.NonCash => MyDataPaymentMethods.Cash,
            (long) PayItemCases.CrossedCheque => MyDataPaymentMethods.Cheque,
            (long) PayItemCases.DebitCardPayment => MyDataPaymentMethods.PosEPos,
            (long) PayItemCases.CreditCardPayment => MyDataPaymentMethods.PosEPos,
            (long) PayItemCases.VoucherPaymentCouponVoucherByMoneyValue => -1,
            (long) PayItemCases.OnlinePayment => MyDataPaymentMethods.WebBanking,
            (long) PayItemCases.LoyaltyProgramCustomerCardPayment => -1,
            (long) PayItemCases.AccountsReceivable => -1,
            (long) PayItemCases.SEPATransfer => -1,
            (long) PayItemCases.OtherBankTransfer => -1,
            (long) PayItemCases.TransferToCashbookVaultOwnerEmployee => -1,
            (long) PayItemCases.InternalMaterialConsumption => -1,
            (long) PayItemCases.Grant => -1,
            (long) PayItemCases.TicketRestaurant => -1,
            _ => throw new Exception($"The Payment type {payItem.ftPayItemCase & 0xF} of PayItem with the case {payItem.ftPayItemCase} is not supported."),
        };

        public static InvoiceType GetInvoiceType(ReceiptRequest receiptRequest)
        {
            if (receiptRequest.IsInvoiceOperation())
            {
                if(receiptRequest.IsInvoiceB2COperation() && !receiptRequest.ContainsCustomerInfo())
                {
                    // in this case we don't know the customer so we cannot add the VAT. The invoice is handled as a Μη Αντικριζόμενα operation ( non facing)
                    if (receiptRequest.cbChargeItems.All(x => (x.ftChargeItemCase & 0xF0) == 0x20))
                    {
                        return InvoiceType.Item112;
                    }
                    else
                    {
                        return InvoiceType.Item111;
                    }
                }

                if (receiptRequest.cbChargeItems.Any(x => x.IsAgencyBusiness()))
                {
                    return InvoiceType.Item14;
                }
                else if (receiptRequest.IsInvoiceOperation() && receiptRequest.cbChargeItems.All(x => (x.ftChargeItemCase & 0xF0) == 0x20))
                {
                    if (receiptRequest.HasEUCustomer())
                    {
                        return InvoiceType.Item22;
                    }
                    else if (receiptRequest.HasNonEUCustomer())
                    {
                        return InvoiceType.Item23;
                    }
                    else
                    {
                        return InvoiceType.Item21;
                    }
                }
                else
                {
                    if (receiptRequest.HasEUCustomer())
                    {
                        return InvoiceType.Item12;
                    }
                    else if (receiptRequest.HasNonEUCustomer())
                    {
                        return InvoiceType.Item13;
                    }
                    else
                    {
                        return InvoiceType.Item11;
                    }
                }
            }


            if (receiptRequest.IsReceiptOperation())
            {
                if (receiptRequest.cbChargeItems.All(x => (x.ftChargeItemCase & 0xF0) == 0x20))
                {
                    return InvoiceType.Item112;
                }
                else
                {
                    return InvoiceType.Item111;
                }
            }

            return receiptRequest.ftReceiptCase switch
            {
                0x4752_2000_0000_3004 => InvoiceType.Item84, // POS Receipt
                _ => throw new Exception("Unknown type of receipt " + receiptRequest.ftReceiptCase)
            };
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
            var inv = CreateInvoiceDocType(receiptRequest, receiptResponse);
            var doc = new InvoicesDoc
            {
                invoice = [inv]
            };
            return doc;
        }

        private AadeBookInvoiceType CreateInvoiceDocType(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
        {
            var totalWithholdAmount = 0m;
            var invoiceDetails = receiptRequest.cbChargeItems.Select(x =>
            {
                var invoiceRow = new InvoiceRowType
                {
                    quantity = x.Quantity,
                    lineNumber = (int) x.Position,
                    vatAmount = x.VATAmount ?? 0.0m,
                    netValue = x.Amount - (x.VATAmount ?? 0.0m),
                    vatCategory = GetVATCategory(x),
                };

                if (invoiceRow.vatCategory == MyDataVatCategory.ExcludingVat)
                {
                    invoiceRow.vatExemptionCategorySpecified = true;
                    invoiceRow.vatExemptionCategory = 1;
                }
                if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x3004)
                {
                    invoiceRow.incomeClassification = [
                      new IncomeClassificationType {
                                        amount = x.Amount -  (x.VATAmount ?? 0.0m),
                                        classificationCategory = IncomeClassificationCategoryType.category1_95
                                    }
                  ];
                }
                else
                {
                    if (x.IsAgencyBusiness())
                    {
                        invoiceRow.incomeClassification = [
                           new IncomeClassificationType {
                                            amount = x.Amount -  (x.VATAmount ?? 0.0m),
                                            classificationCategory = IncomeClassificationCategoryType.category1_7,
                                            classificationType = receiptRequest.HasEUCustomer() ? IncomeClassificationValueType.E3_881_003 : IncomeClassificationValueType.E3_881_001,
                                            classificationTypeSpecified = true
                                        }
                       ];
                    }
                    else
                    {
                        if (receiptRequest.HasEUCustomer())
                        {
                            invoiceRow.incomeClassification = [
                                new IncomeClassificationType {
                                amount = x.Amount -  (x.VATAmount ?? 0.0m),
                                classificationCategory = GetIncomeClassificationCategoryType(x),
                                classificationType = IncomeClassificationValueType.E3_561_005,
                                classificationTypeSpecified = true
                            }
                            ];
                        }
                        else if (receiptRequest.HasNonEUCustomer())
                        {
                            invoiceRow.incomeClassification = [
                                new IncomeClassificationType {
                                amount = x.Amount -  (x.VATAmount ?? 0.0m),
                                classificationCategory = GetIncomeClassificationCategoryType(x),
                                classificationType = IncomeClassificationValueType.E3_561_006,
                                classificationTypeSpecified = true
                            }
                            ];
                        }
                        else
                        {
                            invoiceRow.incomeClassification = [
                                           new IncomeClassificationType {
                                        amount = x.Amount -  (x.VATAmount ?? 0.0m),
                                        classificationCategory = GetIncomeClassificationCategoryType(x),
                                        classificationType = receiptRequest.IsInvoiceOperation() ? GetIncomeClassificationValueTypeForInvoice(x) :  GetIncomeClassificationValueTypeForPrivate(x),
                                        classificationTypeSpecified = true
                                    }
                                       ];
                        }
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
                        totalWithholdAmount += chargeItem.WithHoldingAmount;
                    }
                }
                return invoiceRow;
            }).ToList();

            var incomeClassificationGroups = invoiceDetails.SelectMany(x => x.incomeClassification).Where(x => x.classificationTypeSpecified).GroupBy(x => (x.classificationCategory, x.classificationType)).Select(x => new IncomeClassificationType
            {
                amount = x.Sum(y => y.amount),
                classificationCategory = x.Key.classificationCategory,
                classificationType = x.Key.classificationType,
                classificationTypeSpecified = true
            }).ToList();
            incomeClassificationGroups.AddRange(invoiceDetails.SelectMany(x => x.incomeClassification).Where(x => !x.classificationTypeSpecified).GroupBy(x => x.classificationCategory).Select(x => new IncomeClassificationType
            {
                amount = x.Sum(y => y.amount),
                classificationCategory = x.Key,
            }).ToList());

            var identification = long.Parse(receiptResponse.ftReceiptIdentification.Replace("ft", "").Split("#")[0], System.Globalization.NumberStyles.HexNumber);

            var paymentMethods = receiptRequest.cbPayItems.Where(x => (x.ftPayItemCase & ((long) 0xFF)) != 0x99).Select(x =>
            {
                var payment = new PaymentMethodDetailType
                {
                    type = GetPaymentType(x),
                    amount = x.Amount,
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
            }).ToArray();

            var withholdingItems = receiptRequest.cbPayItems.Where(x => (x.ftPayItemCase & ((long) 0xFF)) == 0x99).ToList();

            var inv = new AadeBookInvoiceType
            {
                issuer = CreateIssuer(), // issuer from masterdataconfig
                paymentMethods = paymentMethods,
                invoiceHeader = new InvoiceHeaderType
                {
                    series = "0",
                    aa = identification.ToString(),
                    issueDate = receiptRequest.cbReceiptMoment,
                    invoiceType = GetInvoiceType(receiptRequest),
                    currency = CurrencyType.EUR,
                    currencySpecified = true
                },
                invoiceDetails = invoiceDetails.ToArray(),
                invoiceSummary = new InvoiceSummaryType
                {
                    totalNetValue = receiptRequest.cbChargeItems.Sum(x => x.Amount - (x.VATAmount ?? 0.0m)),
                    totalVatAmount = receiptRequest.cbChargeItems.Sum(x => x.VATAmount ?? 0.0m),
                    totalWithheldAmount = totalWithholdAmount,
                    totalFeesAmount = 0.0m,
                    totalStampDutyAmount = 0.0m,
                    totalOtherTaxesAmount = 0.0m,
                    totalDeductionsAmount = 0.0m,
                    totalGrossValue = receiptRequest.cbChargeItems.Sum(x => x.Amount) - totalWithholdAmount,
                    incomeClassification = incomeClassificationGroups.ToArray()
                },
            };
            if (receiptRequest.cbCustomer != null)
            {
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
                }
                else
                {
                    invoiceMark = -1;
                }
            }
            return inv;
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

        public string GetUid(AadeBookInvoiceType invoice) => BitConverter.ToString(SHA1.HashData(Encoding.UTF8.GetBytes($"{invoice.issuer.vatNumber}-{invoice.invoiceHeader.issueDate.ToString("yyyy-MM-dd")}-{invoice.issuer.branch}-{GetInvoiceType(invoice.invoiceHeader.invoiceType)}-{invoice.invoiceHeader.series}-{invoice.invoiceHeader.aa}"))).Replace("-", "");

        public string GetInvoiceType(InvoiceType invoiceType)
        {
            return invoiceType switch
            {
                InvoiceType.Item11 => "1.1",
                InvoiceType.Item111 => "11.1",
                _ => "11.1",
            };
        }

        public string GenerateInvoicePayload(InvoicesDoc doc)
        {
            var xmlSerializer = new XmlSerializer(typeof(InvoicesDoc));
            using var stringWriter = new StringWriter();
            xmlSerializer.Serialize(stringWriter, doc);
            var xmlContent = stringWriter.ToString();
            return xmlContent;
        }
    }
}
