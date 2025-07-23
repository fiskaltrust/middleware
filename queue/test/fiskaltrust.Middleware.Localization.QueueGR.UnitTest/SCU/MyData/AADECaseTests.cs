using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData;
using fiskaltrust.Middleware.Localization.QueueGR.UnitTest;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.ifPOS.v2.Cases;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;
using fiskaltrust.Middleware.SCU.GR.MyData;

namespace fiskaltrust.Middleware.Localization.QueueGR.IntegrationTest.MyDataSCU
{
    [Trait("only", "local")]
    public class AADECaseTests
    {
        public const string CUSOMTER_VATNUMBER = "026883248";
        private readonly AADEFactory _aadeFactory;
        public static ReceiptRequest BaseRequest => new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid()
        };

        public static ChargeItem CreateChargeItem(int position, decimal amount, int vatRate, string description, ChargeItemCaseTypeOfService chargeItemCaseTypeOfService, ChargeItemCase chargeItemCase)
        {
            return new ChargeItem
            {
                Position = position,
                Amount = amount,
                VATRate = vatRate,
                VATAmount = decimal.Round(amount / (100M + vatRate) * vatRate, 2, MidpointRounding.ToEven),
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithTypeOfService(chargeItemCaseTypeOfService).WithVat(chargeItemCase),
                Quantity = 1,
                Description = description
            };
        }

        public static PayItem CreatePayItem(int position, decimal amount, string description, PayItemCase payItemCase)
        {
            return new PayItem
            {
                Position = position,
                Amount = amount,
                ftPayItemCase = ((PayItemCase) 0x4752_2000_0000_0000).WithCase(payItemCase),
                Quantity = 1,
                Description = description
            };
        }

        public static ReceiptRequest CreateWithCustomer(ReceiptCase receiptCase, List<ChargeItem> chargeItems, List<PayItem> payItems)
        {
            return new ReceiptRequest
            {
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(receiptCase),
                cbChargeItems = chargeItems,
                cbPayItems = payItems,
                cbCustomer = new MiddlewareCustomer
                {
                    CustomerVATId = CUSOMTER_VATNUMBER,
                    CustomerName = "Πελάτης A.E.",
                    CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                    CustomerCity = "Αθηνών",
                    CustomerCountry = "GR",
                },
                cbReceiptAmount = chargeItems.Sum(x => x.Amount)
            };
        }

        public AADECaseTests()
        {
            _aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
            {
                Account = new storage.V0.MasterData.AccountMasterData
                {
                    VatId = "112545020"
                }
            });
        }

        private void ValidateMyData(ReceiptRequest receiptRequest, InvoiceType expectedInvoiceType, IncomeClassificationCategoryType expectedCategory, IncomeClassificationValueType? expectedValueType)
        {
            using var scope = new AssertionScope();
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(receiptRequest, ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(expectedInvoiceType);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(expectedCategory);
            if (expectedValueType != null)
            {
                invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationTypeSpecified.Should().BeTrue();
                invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(expectedValueType);
            }
        }

        [Fact]
        public void AADECertificationExamples_A1_1_1p1()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2C0x1001),
                cbChargeItems = [CreateChargeItem(1, 100, 24, "Line item 1", ChargeItemCaseTypeOfService.Delivery, ChargeItemCase.NormalVatRate)],
                cbPayItems = [CreatePayItem(1, 100, "Κάρτα", PayItemCase.DebitCardPayment)],
                cbCustomer = new MiddlewareCustomer
                {
                    CustomerVATId = CUSOMTER_VATNUMBER,
                    CustomerName = "Πελάτης A.E.",
                    CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                    CustomerCity = "Αθηνών",
                    CustomerCountry = "GR",
                }
            };
            ValidateMyData(receiptRequest, InvoiceType.Item11, IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_001);
        }

        [Fact]
        public void AADECertificationExamples_A1_1_1p4()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2C0x1001),
                cbChargeItems = [CreateChargeItem(1, 100, 24, "Line item 1", ChargeItemCaseTypeOfService.NotOwnSales, ChargeItemCase.NormalVatRate)],
                cbPayItems = [CreatePayItem(1, 100, "Κάρτα", PayItemCase.DebitCardPayment)],
                cbCustomer = new MiddlewareCustomer
                {
                    CustomerVATId = CUSOMTER_VATNUMBER,
                    CustomerName = "Πελάτης A.E.",
                    CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                    CustomerCity = "Αθηνών",
                    CustomerCountry = "GR",
                }
            };
            ValidateMyData(receiptRequest, InvoiceType.Item14, IncomeClassificationCategoryType.category1_7, IncomeClassificationValueType.E3_881_001);
        }

        [Fact]
        public void AADECertificationExamples_A1_1_1p6()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2C0x1001),
                cbChargeItems = [CreateChargeItem(1, 100, 24, "Line item 1", ChargeItemCaseTypeOfService.Delivery, ChargeItemCase.NormalVatRate)],
                cbPayItems = [CreatePayItem(1, 100, "Κάρτα", PayItemCase.DebitCardPayment)],
                cbPreviousReceiptReference = "400001941223252",
                cbCustomer = new MiddlewareCustomer
                {
                    CustomerVATId = CUSOMTER_VATNUMBER,
                    CustomerName = "Πελάτης A.E.",
                    CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                    CustomerCity = "Αθηνών",
                    CustomerCountry = "GR",
                }
            };
            ValidateMyData(receiptRequest, InvoiceType.Item16, IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_001);
        }

        [Fact]
        public void AADECertificationExamples_A1_2_2p1()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2C0x1001),
                cbChargeItems = [CreateChargeItem(1, 100, 24, "Line item 1", ChargeItemCaseTypeOfService.OtherService, ChargeItemCase.NormalVatRate)],
                cbPayItems = [CreatePayItem(1, 100, "Κάρτα", PayItemCase.DebitCardPayment)],
                cbCustomer = new MiddlewareCustomer
                {
                    CustomerVATId = CUSOMTER_VATNUMBER,
                    CustomerName = "Πελάτης A.E.",
                    CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                    CustomerCity = "Αθηνών",
                    CustomerCountry = "GR",
                }
            };
            ValidateMyData(receiptRequest, InvoiceType.Item21, IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_001);
        }

        [Fact]
        public void AADECertificationExamples_A1_2_2p4()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2C0x1001),
                cbChargeItems = [CreateChargeItem(1, 100, 24, "Line item 1", ChargeItemCaseTypeOfService.OtherService, ChargeItemCase.NormalVatRate)],
                cbPayItems = [CreatePayItem(1, 100, "Κάρτα", PayItemCase.DebitCardPayment)],
                cbPreviousReceiptReference = "400001941223252",
                cbCustomer = new MiddlewareCustomer
                {
                    CustomerVATId = CUSOMTER_VATNUMBER,
                    CustomerName = "Πελάτης A.E.",
                    CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                    CustomerCity = "Αθηνών",
                    CustomerCountry = "GR",
                }
            };
            ValidateMyData(receiptRequest, InvoiceType.Item24, IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_001);
        }

        [Fact]
        public void AADECertificationExamples_A2_11_11p1()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
                cbChargeItems = [CreateChargeItem(1, 100, 24, "Line item 1", ChargeItemCaseTypeOfService.Delivery, ChargeItemCase.NormalVatRate)],
                cbPayItems = [CreatePayItem(1, 100, "Κάρτα", PayItemCase.DebitCardPayment)]
            };
            ValidateMyData(receiptRequest, InvoiceType.Item111, IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_003);
        }

        [Fact]
        public void AADECertificationExamples_A2_11_11p2()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
                cbChargeItems = [CreateChargeItem(1, 100, 24, "Line item 1", ChargeItemCaseTypeOfService.OtherService, ChargeItemCase.NormalVatRate)],
                cbPayItems = [CreatePayItem(1, 100, "Κάρτα", PayItemCase.DebitCardPayment)]
            };
            ValidateMyData(receiptRequest, InvoiceType.Item112, IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_003);
        }

        [Fact]
        public void AADECertificationExamples_A2_11_11p4()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001).WithFlag(ReceiptCaseFlags.Refund),
                cbChargeItems = [CreateChargeItem(1, -100, 24, "Line item 1", ChargeItemCaseTypeOfService.Delivery, ChargeItemCase.NormalVatRate)],
                cbPayItems = [CreatePayItem(1, -100, "Κάρτα", PayItemCase.DebitCardPayment)]
            };
            ValidateMyData(receiptRequest, InvoiceType.Item114, IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_003);
        }

        [Fact]
        public  void AADECertificationExamples_A2_11_11p5()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
                cbChargeItems = [CreateChargeItem(1, 100, 24, "Line item 1", ChargeItemCaseTypeOfService.NotOwnSales, ChargeItemCase.NormalVatRate)],
                cbPayItems = [CreatePayItem(1, 100, "Κάρτα", PayItemCase.DebitCardPayment)]
            };
            ValidateMyData(receiptRequest, InvoiceType.Item115, IncomeClassificationCategoryType.category1_7, IncomeClassificationValueType.E3_881_002);
        }

        public ReceiptResponse ExampleResponse => new ReceiptResponse
        {
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftCashBoxIdentification = "cashBoxIdentification",
            ftReceiptIdentification = "ft" + DateTime.UtcNow.Ticks.ToString("X"),
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x4752_2000_0000_0000
        };
    }
}