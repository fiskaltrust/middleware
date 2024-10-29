﻿using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.myDataSCU;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE
{
    public class AADEFactory
    {
        private IncomeClassificationValueType GetIncomeClassificationValueType(ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0xF0) switch
        {
            _ => IncomeClassificationValueType.E3_561_007,
        };

        private IncomeClassificationCategoryType GetIncomeClassificationCategoryType(ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0xF0) switch
        {
            0x00 => IncomeClassificationCategoryType.category1_2,
            0x10 => IncomeClassificationCategoryType.category1_2,
            0x20 => IncomeClassificationCategoryType.category1_3,
            _ => IncomeClassificationCategoryType.category1_2,
        };

        private int GetVATCategory(ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0xF) switch
        {
            (long) ChargeItemCaseVat.NormalVatRate => MyDataVatCategory.VatRate24, // Normal 24%
            (long) ChargeItemCaseVat.DiscountedVatRate1 => MyDataVatCategory.VatRate13, // Discounted-1 13&
            (long) ChargeItemCaseVat.DiscountedVatRate2 => MyDataVatCategory.VatRate6, // Discounted-2 6%
            (long) ChargeItemCaseVat.SuperReducedVatRate1 => MyDataVatCategory.VatRate17, // Super reduced 1 17%
            (long) ChargeItemCaseVat.SuperReducedVatRate2 => MyDataVatCategory.VatRate9, // Super reduced 2 9%
            (long) ChargeItemCaseVat.ParkingVatRate => MyDataVatCategory.VatRate4, // Parking VAT 4%
            (long) ChargeItemCaseVat.NotTaxable => MyDataVatCategory.RegistrationsWithoutVat, // Not Taxable
            (long) ChargeItemCaseVat.ZeroVatRate => MyDataVatCategory.RegistrationsWithoutVat, // Zero
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

        private InvoiceType GetInvoiceType(ReceiptRequest receiptRequest) => receiptRequest.ftReceiptCase switch
        {
            _ => InvoiceType.Item111, // Retail - Simplified Invoice
        };

        public InvoicesDoc MapToInvoicesDoc(List<ftQueueItem> queueItems)
        {
            var receiptRequests = queueItems.Select(x => (receiptRequest: JsonSerializer.Deserialize<ReceiptRequest>(x.request)!, receiptResponse: JsonSerializer.Deserialize<ReceiptResponse>(x.response))).ToList();
            var actualReceiptRequests = receiptRequests.Where(x => x.receiptResponse != null && ((long) x.receiptResponse.ftState & 0xFF) == 0x00).Cast<(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)>().ToList();
            var doc = new InvoicesDoc
            {
                invoice = actualReceiptRequests.Select(x => MapToInvoiceResult(x.receiptRequest, x.receiptResponse)).ToArray()
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

        private AadeBookInvoiceType MapToInvoiceResult(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
        {
            var invoiceUid = receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "invoiceUid")?.Data;
            var invoiceMarkText = receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "invoiceMark")?.Data;
            if(int.TryParse(invoiceMarkText, out var invoiceMark))
            {

            }
            else
            {
                invoiceMark = -1;
            }
            var authenticationCode = receiptResponse.ftSignatures.FirstOrDefault(x => x.Caption == "authenticationCode")?.Data;

            var invoiceDetails = receiptRequest.cbChargeItems.Select(x => new InvoiceRowType
            {
                quantity = x.Quantity,
                lineNumber = (int) x.Position,
                vatAmount = x.VATAmount ?? 0.0m,
                netValue = x.Amount - (x.VATAmount ?? 0.0m),
                vatCategory = GetVATCategory(x),
                incomeClassification = [
                              new IncomeClassificationType {
                                        amount =x.Amount - (x.VATAmount ?? 0.0m),
                                        classificationCategory = GetIncomeClassificationCategoryType(x),
                                        classificationType = GetIncomeClassificationValueType(x),
                                        classificationTypeSpecified = true
                                    }
                          ]
            }).ToList();

            var incomeClassificationGroups = invoiceDetails.SelectMany(x => x.incomeClassification).GroupBy(x => (x.classificationCategory, x.classificationType)).Select(x => new IncomeClassificationType
            {
                amount = x.Sum(y => y.amount),
                classificationCategory = x.Key.classificationCategory,
                classificationType = x.Key.classificationType,
                classificationTypeSpecified = true
            }).ToList();
            var inv = new AadeBookInvoiceType
            {
                mark = invoiceMark,
                uid = invoiceUid ?? "",
                authenticationCode = authenticationCode ?? "",
                issuer = CreateIssuer(), // issuer from masterdataconfig
                paymentMethods = receiptRequest.cbPayItems.Select(x => new PaymentMethodDetailType
                {
                    type = GetPaymentType(x),
                    amount = x.Amount,
                    paymentMethodInfo = x.Description
                }).ToArray(),
                invoiceHeader = new InvoiceHeaderType
                {
                    series = "A",
                    aa = receiptResponse.ftQueueRow.ToString(),
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
                    totalWithheldAmount = 0.0m,
                    totalFeesAmount = 0.0m,
                    totalStampDutyAmount = 0.0m,
                    totalOtherTaxesAmount = 0.0m,
                    totalDeductionsAmount = 0.0m,
                    totalGrossValue = receiptRequest.cbChargeItems.Sum(x => x.Amount),
                    incomeClassification = incomeClassificationGroups.ToArray()
                }
            };
            return inv;
        }

        private AadeBookInvoiceType CreateInvoiceDocType(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
        {
            var invoiceDetails = receiptRequest.cbChargeItems.Select(x => new InvoiceRowType
            {
                quantity = x.Quantity,
                lineNumber = (int) x.Position,
                vatAmount = x.VATAmount ?? 0.0m,
                netValue = x.Amount - (x.VATAmount ?? 0.0m),
                vatCategory = GetVATCategory(x),
                incomeClassification = [
                              new IncomeClassificationType {
                                        amount =x.Amount - (x.VATAmount ?? 0.0m),
                                        classificationCategory = GetIncomeClassificationCategoryType(x),
                                        classificationType = GetIncomeClassificationValueType(x),
                                        classificationTypeSpecified = true
                                    }
                          ]
            }).ToList();

            var incomeClassificationGroups = invoiceDetails.SelectMany(x => x.incomeClassification).GroupBy(x => (x.classificationCategory, x.classificationType)).Select(x => new IncomeClassificationType
            {
                amount = x.Sum(y => y.amount),
                classificationCategory = x.Key.classificationCategory,
                classificationType = x.Key.classificationType,
                classificationTypeSpecified = true
            }).ToList();
            var inv = new AadeBookInvoiceType
            {
                issuer = CreateIssuer(), // issuer from masterdataconfig
                paymentMethods = receiptRequest.cbPayItems.Select(x => new PaymentMethodDetailType
                {
                    type = GetPaymentType(x),
                    amount = x.Amount,
                    paymentMethodInfo = x.Description
                }).ToArray(),
                invoiceHeader = new InvoiceHeaderType
                {
                    series = "A",
                    aa = receiptResponse.ftQueueRow.ToString(),
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
                    totalWithheldAmount = 0.0m,
                    totalFeesAmount = 0.0m,
                    totalStampDutyAmount = 0.0m,
                    totalOtherTaxesAmount = 0.0m,
                    totalDeductionsAmount = 0.0m,
                    totalGrossValue = receiptRequest.cbChargeItems.Sum(x => x.Amount),
                    incomeClassification = incomeClassificationGroups.ToArray()
                }
            };
            return inv;
        }

        private PartyType CreateIssuer()
        {
            return new PartyType
            {
                vatNumber = "997671771",
                country = CountryType.GR,
                branch = 1,
            };
        }

        public string GetUid(AadeBookInvoiceType invoice) => Encoding.UTF8.GetString(SHA1.HashData(Encoding.UTF8.GetBytes($"{invoice.issuer.vatNumber}-{invoice.invoiceHeader.issueDate}-{invoice.issuer.branch}-{invoice.invoiceHeader.invoiceType}-{invoice.invoiceHeader.series}-{invoice.invoiceHeader.aa}")));

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
