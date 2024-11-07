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
using Org.BouncyCastle.Asn1.IsisMtt.X509;

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

        /// <summary>
        /// E3_106 Intrinsic production of fixed assets -Self - deliveries - Inventory destruction / Goods
        /// E3_205 Intrinsic reproduction of fixed assets -Self - production - Destruction of inventories / Raw materials and other materials
        /// E3_210 Proprietary assets - Self - production - Inventory destruction / Products and production in progress
        /// E3_305 Intrinsic reproduction of fixed assets -Self - production - Destruction of inventories / Raw materials and other materials
        /// E3_310 Proprietary assets - Self - production - Inventory destruction / Products and production in progress
        /// E3_318 Proprietary production of fixed assets -Self - deliveries - Inventory losses / Production costs
        /// E3_561_001 Sales of goods and services Wholesale - Professionals
        /// E3_561_002 Sales of goods and services Wholesale under article 39a par 5 of the VAT Code(Law 2859 / 2000)
        /// E3_561_003 Sales of goods and services Retail - Private customers
        /// E3_561_004 Retail sales of goods and services under article 39a par 5 of the VAT Code(Law 2859 / 2000)
        /// E3_561_005 Foreign sales of goods and services Intra - Community sales
        /// E3_561_006 Foreign sales of goods and services Third countries
        /// E3_561_007 Sales of goods and services Other
        /// E3_562 Other ordinary income
        /// E3_563 Interest on loans and related income
        /// E3_564 Credit and exchange rate differences
        /// E3_565 Revenue from participations
        /// E3_566 Gains on disposal of non - current assets
        /// E3_567 Gains from reversal of provisions and impairments
        /// E3_568 Gains from measurement at fair value
        /// E3_570 Unusual income and gains
        /// E3_595 Expenditure on own - account production
        /// E3_596 Subsidies - Grants
        /// E3_597 Grants - Grants for investment purposes - Covering expenditure
        /// E3_880_001 Wholesale sales of fixed assets
        /// E3_880_002 Retail sales of fixed assets
        /// E3_880_003 Foreign sales of fixed assets Intra-Community sales
        /// E3_880_004 Sales of Foreign Fixed Assets Third Countries
        /// E3_881_001 Sales for third party accounts Wholesale
        /// E3_881_002 Sales for third party accounts Retail
        /// E3_881_003 Sales for third party accounts Abroad Intra - Community
        /// E3_881_004 Sales for foreign account Third Countries Third Countries
        /// E3_598_001 Sales of goods subject to VAT
        /// E3_598_003 Sales on behalf of farmers through an agricultural cooperative, etc.
        /// </summary>
        private IncomeClassificationValueType GetIncomeClassificationValueType(ReceiptRequest receiptRequest, ChargeItem chargeItem)
        {
            if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x3005)
            {
                return IncomeClassificationValueType.E3_562;
            }
            if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x3003)
            {
                return IncomeClassificationValueType.E3_595;
            }

            if (chargeItem.IsAgencyBusiness())
            {
                if (receiptRequest.IsReceiptOperation())
                {
                    if ((chargeItem.ftChargeItemCase & 0xFF00) == NatureExemptions.EndOfClimateCrisesNature)
                    {
                        return IncomeClassificationValueType.E3_881_001;
                    }

                    if (receiptRequest.HasGreeceCountryCode())
                    {
                        return IncomeClassificationValueType.E3_881_002;
                    }
                    else if (receiptRequest.HasEUCountryCode())
                    {
                        return IncomeClassificationValueType.E3_881_003;
                    }
                    else
                    {
                        return IncomeClassificationValueType.E3_881_004;

                    }
                }
                else
                {
                    if (receiptRequest.HasGreeceCountryCode())
                    {
                        return IncomeClassificationValueType.E3_881_001;
                    }
                    else if (receiptRequest.HasEUCountryCode())
                    {
                        return IncomeClassificationValueType.E3_881_003;
                    }
                    else
                    {
                        throw new Exception("Agency business with non EU customer is not supported");
                    }
                }
            }

            if (receiptRequest.IsInvoiceOperation())
            {
                if (receiptRequest.HasGreeceCountryCode())
                {
                    return (chargeItem.ftChargeItemCase & 0xF0) switch
                    {
                        0x00 => IncomeClassificationValueType.E3_561_001,
                        0x10 => IncomeClassificationValueType.E3_561_001,
                        0x20 => IncomeClassificationValueType.E3_561_001,
                        _ => IncomeClassificationValueType.E3_561_007,
                    };
                }
                else if (receiptRequest.HasEUCountryCode())
                {
                    return IncomeClassificationValueType.E3_561_005;
                }
                else
                {
                    return IncomeClassificationValueType.E3_561_006;
                }
            }
            else if (receiptRequest.IsReceiptOperation())
            {
                return (chargeItem.ftChargeItemCase & 0xF0) switch
                {
                    0x00 => IncomeClassificationValueType.E3_561_003,
                    0x10 => IncomeClassificationValueType.E3_561_003,
                    0x20 => IncomeClassificationValueType.E3_561_003,
                    _ => IncomeClassificationValueType.E3_561_007,
                };
            }

            if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x3006)
            {
                return (chargeItem.ftChargeItemCase & 0xF0) switch
                {
                    0x00 => IncomeClassificationValueType.E3_561_001,
                    _ => IncomeClassificationValueType.E3_561_007,
                };
            }
            return (chargeItem.ftChargeItemCase & 0xF0) switch
            {
                0x00 => IncomeClassificationValueType.E3_561_003,
                0x10 => IncomeClassificationValueType.E3_561_003,
                0x20 => IncomeClassificationValueType.E3_561_003,
                _ => IncomeClassificationValueType.E3_561_007,
            };
        }

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
        private IncomeClassificationCategoryType GetIncomeClassificationCategoryType(ReceiptRequest receiptRequest, ChargeItem chargeItem)
        {
            if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x3003)
            {
                return IncomeClassificationCategoryType.category1_6;
            }

            if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x3005)
            {
                return IncomeClassificationCategoryType.category1_5;
            }

            return (chargeItem.ftChargeItemCase & 0xF0) switch
            {
                0x00 => IncomeClassificationCategoryType.category1_2,
                0x10 => IncomeClassificationCategoryType.category1_2,
                0x20 => IncomeClassificationCategoryType.category1_3,
                0x60 => IncomeClassificationCategoryType.category1_7,
                _ => IncomeClassificationCategoryType.category1_2,
            };
        }

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
                if (receiptRequest.IsRefund())
                {
                    return InvoiceType.Item51;
                }

                if (receiptRequest.IsInvoiceB2COperation() && !receiptRequest.ContainsCustomerInfo())
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
                if (receiptRequest.cbChargeItems.Any(x => (x.ftChargeItemCase & 0xF0) == 0x90))
                {
                    return InvoiceType.Item15;
                }
                if (receiptRequest.cbChargeItems.Any(x => x.IsAgencyBusiness()))
                {
                    return InvoiceType.Item14;
                }
                else if (receiptRequest.IsInvoiceOperation() && receiptRequest.cbChargeItems.All(x => (x.ftChargeItemCase & 0xF0) == 0x20))
                {
                    if (!string.IsNullOrEmpty(receiptRequest.cbPreviousReceiptReference))
                    {
                        return InvoiceType.Item24;
                    }

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
                    if (!string.IsNullOrEmpty(receiptRequest.cbPreviousReceiptReference))
                    {
                        return InvoiceType.Item16;
                    }

                    if (receiptRequest.HasGreeceCountryCode())
                    {
                        return InvoiceType.Item11;
                    }
                    else if (receiptRequest.HasEUCountryCode())
                    {
                        return InvoiceType.Item12;
                    }
                    else
                    {
                        return InvoiceType.Item13;
                    }
                }
            }


            if (receiptRequest.IsReceiptOperation())
            {
                if (receiptRequest.cbChargeItems.Any(x => x.IsAgencyBusiness()))
                {
                    if (receiptRequest.cbChargeItems.Any(x => (x.ftChargeItemCase & 0xFF00) == NatureExemptions.EndOfClimateCrisesNature))
                    {
                        return InvoiceType.Item82;
                    }

                    return InvoiceType.Item115;
                }
                else if (receiptRequest.cbReceiptAmount < 100m)
                {
                    return InvoiceType.Item113;
                }
                else if (receiptRequest.cbChargeItems.All(x => (x.ftChargeItemCase & 0xF0) == 0x20))
                {
                    return InvoiceType.Item112;
                }
                else
                {
                    return InvoiceType.Item111;
                }
            }

            if (receiptRequest.ftReceiptCase == 0x4752_2000_0000_3003)
            {
                if (receiptRequest.cbChargeItems.All(x => (x.ftChargeItemCase & 0xF0) == 0x20))
                {
                    return InvoiceType.Item62;
                }
                else
                {
                    return InvoiceType.Item61;
                }
            }

            return receiptRequest.ftReceiptCase switch
            {
                0x4752_2000_0100_3004 => InvoiceType.Item85, // POS Refund
                0x4752_2000_0000_3004 => InvoiceType.Item84, // POS Receipt
                0x4752_2000_0000_3005 => InvoiceType.Item81, // rent
                0x4752_2000_0000_3006 => InvoiceType.Item71, // rent
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
                var vatAmount = x.VATAmount ?? 0.0m;
                var invoiceRow = new InvoiceRowType
                {
                    quantity = receiptRequest.IsRefund() ? -x.Quantity : x.Quantity,
                    lineNumber = (int) x.Position,
                    vatAmount = receiptRequest.IsRefund() ? -vatAmount : vatAmount,
                    netValue = receiptRequest.IsRefund() ? (-x.Amount - -vatAmount) : x.Amount - vatAmount,
                    vatCategory = GetVATCategory(x),
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
                    if (invoiceRow.vatCategory == MyDataVatCategory.ExcludingVat)
                    {
                        invoiceRow.vatExemptionCategorySpecified = true;
                        invoiceRow.vatExemptionCategory = 1;
                    }

                    if (receiptRequest.cbChargeItems.Any(x => (x.ftChargeItemCase & 0xF0) == 0x90) && (x.ftChargeItemCase & 0xF0) != 0x90)
                    {
                        invoiceRow.invoiceDetailType = 2;
                        invoiceRow.invoiceDetailTypeSpecified = true;
                        invoiceRow.incomeClassification = [
                            new IncomeClassificationType {
                                                amount = invoiceRow.netValue,
                                                classificationCategory = GetIncomeClassificationCategoryType(receiptRequest, x),
                                                classificationType = GetIncomeClassificationValueType(receiptRequest, x),
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
                    else
                    {
                        if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x3004)
                        {
                            invoiceRow.incomeClassification = [
                              new IncomeClassificationType {
                                        amount = invoiceRow.netValue,
                                        classificationCategory = IncomeClassificationCategoryType.category1_95
                                    }
                          ];
                        }
                        else
                        {
                            invoiceRow.incomeClassification = [
                                new IncomeClassificationType {
                                amount = invoiceRow.netValue,
                                classificationCategory = GetIncomeClassificationCategoryType(receiptRequest, x),
                                classificationType = GetIncomeClassificationValueType(receiptRequest, x),
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

            var paymentMethods = receiptRequest.cbPayItems.Where(x => (x.ftPayItemCase & ((long) 0xFF)) != 0x99).Select(x =>
            {
                var payment = new PaymentMethodDetailType
                {
                    type = GetPaymentType(x),
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
                    totalNetValue = invoiceDetails.Sum(x => x.netValue),
                    totalVatAmount = invoiceDetails.Sum(x => x.vatAmount),
                    totalWithheldAmount = invoiceDetails.Sum(x => x.withheldAmount),
                    totalFeesAmount = invoiceDetails.Sum(x => x.feesAmount),
                    totalStampDutyAmount = invoiceDetails.Sum(x => x.stampDutyAmount),
                    totalOtherTaxesAmount = invoiceDetails.Sum(x => x.otherTaxesAmount),
                    totalDeductionsAmount = invoiceDetails.Sum(x => x.deductionsAmount),
                    totalGrossValue = invoiceDetails.Sum(x => x.netValue) + invoiceDetails.Sum(x => x.vatAmount) + invoiceDetails.Sum(x => x.otherTaxesAmount),
                    incomeClassification = incomeClassificationGroups.ToArray(),
                    expensesClassification = expensesClassificationGroups.ToArray(),
                },
            };
            if (!string.IsNullOrEmpty(receiptRequest.cbPreviousReceiptReference))
            {
                inv.invoiceHeader.correlatedInvoices = [long.Parse(receiptRequest.cbPreviousReceiptReference)];
            }
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
