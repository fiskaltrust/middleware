using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using fiskaltrust.Middleware.Localization.QueueGR.UnitTest;
using fiskaltrust.ifPOS.v2.Cases;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace fiskaltrust.Middleware.SCU.GR.IntegrationTest.MyDataSCU
{

    [Trait("only", "local")]
    public class AADECertificationTests
    {
        private readonly ITestOutputHelper _output;
        private readonly AADEFactory _aadeFactory;

        public AADECertificationTests(ITestOutputHelper output)
        {
            _output = output;
            _aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
            {
                Account = new storage.V0.MasterData.AccountMasterData
                {
                    VatId = "112545020"
                }
            }, "https://test.receipts.example.com");
        }

        public ResponseDoc? GetResponse(string xmlContent)
        {
            var xmlSerializer = new XmlSerializer(typeof(ResponseDoc));
            using var stringReader = new StringReader(xmlContent);
            return xmlSerializer.Deserialize(stringReader) as ResponseDoc;
        }

        private async Task<string?> SendToMayData(string xml)
        {
            var httpClient = new HttpClient()
            {
                BaseAddress = new Uri("https://mydataapidev.aade.gr/")
            };
            httpClient.DefaultRequestHeaders.Add("aade-user-id", "user11111111");
            httpClient.DefaultRequestHeaders.Add("ocp-apim-subscription-key", "41291863a36d552c4d7fc8195d427dd3");

            var response = await httpClient.PostAsync("/myDataProvider/SendInvoices", new StringContent(xml, Encoding.UTF8, "application/xml"));
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to send data to myData API: " + content);
            }

            var ersult = GetResponse(content);
            var marker = "";
            if (ersult != null)
            {
                var data = ersult.response[0];
                if (data.statusCode.ToLower() == "success")
                {
                    for (var i = 0; i < data.ItemsElementName.Length; i++)
                    {
                        if (data.ItemsElementName[i] == ItemsChoiceType.qrUrl)
                        {

                        }
                        else if (data.ItemsElementName[i] == ItemsChoiceType.invoiceMark)
                        {
                            marker = data.Items[i].ToString();

                        }
                    }
                    _output.WriteLine(content);
                }
                else
                {
                    _output.WriteLine(xml);

                    _output.WriteLine(content);
                    throw new Exception("Error" + content);
                }
            }
            else
            {
                _output.WriteLine(xml);

                _output.WriteLine(content);
                throw new Exception("Invalid response" + content);
            }
            return marker;
        }

        private async Task ValidateMyData(ReceiptRequest receiptRequest, InvoiceType expectedInvoiceType, [CallerMemberName] string caller = "")
        {
            using var scope = new AssertionScope();
            (var invoiceDoc, var error) = _aadeFactory.MapToInvoicesDoc(receiptRequest, ExampleResponse);
            invoiceDoc!.invoice[0].invoiceHeader.invoiceType.Should().Be(expectedInvoiceType);
            invoiceDoc!.invoice[0].invoiceSummary.incomeClassification.Should().BeEmpty();
            var xml = AADEFactory.GenerateInvoicePayload(invoiceDoc!);
            await SendToMayData(xml);
            Console.WriteLine(caller);
        }

        private async Task ValidateMyData(ReceiptRequest receiptRequest, InvoiceType expectedInvoiceType, IncomeClassificationCategoryType expectedCategory, IncomeClassificationValueType expectedValueType, [CallerMemberName] string caller = "")
        {
            using var scope = new AssertionScope();
            (var invoiceDoc, var error) = _aadeFactory.MapToInvoicesDoc(receiptRequest, ExampleResponse);
            invoiceDoc!.invoice[0].invoiceHeader.invoiceType.Should().Be(expectedInvoiceType);
            invoiceDoc!.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(expectedCategory);
            invoiceDoc!.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(expectedValueType);
            var xml = AADEFactory.GenerateInvoicePayload(invoiceDoc!);
            await SendToMayData(xml);
            Console.WriteLine(caller);
        }

        /// <summary>
        /// fiskaltrust/market-gr#325: a 1.1 invoice that is also a delivery note, with
        /// packingsDeclarations supplied via mydataoverride (Viva's request shape, wrapped in an array).
        /// </summary>
        [Fact]
        public async Task SalesInvoice_1_1_DeliveryNote_WithPackingsDeclarationsOverride_IsAcceptedByMyData()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptAmount = 124m,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbChargeItems =
                [
                    new ChargeItem
                    {
                        Position = 1,
                        Amount = 124,
                        VATRate = 24,
                        VATAmount = 24,
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
                        Quantity = 1,
                        Description = "Line item 1"
                    }
                ],
                cbPayItems =
                [
                    new PayItem
                    {
                        Amount = 124m,
                        Description = "Μετρητά",
                        ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                    }
                ],
                ftPosSystemId = Guid.NewGuid(),
                // Invoice with the GR HasTransportInformation flag (0x0400_0000), as in Viva's request
                // (which additionally sets the handwritten flag; not relevant here).
                ftReceiptCase = (ReceiptCase) 0x4752_2000_0400_1001,
                cbCustomer = new MiddlewareCustomer
                {
                    CustomerVATId = AADECertificationExamples.CUSOMTER_VATNUMBER,
                    CustomerName = "Πελάτης A.E.",
                    CustomerStreet = "Κηφισίας 12",
                    CustomerZip = "12345",
                    CustomerCity = "Αθηνών",
                    CustomerCountry = "GR",
                },
                ftReceiptCaseData = new
                {
                    GR = new
                    {
                        mydataoverride = new
                        {
                            invoice = new
                            {
                                invoiceHeader = new
                                {
                                    invoiceType = "1.1",
                                    withoutDigitalTransportTracking = true,
                                    dispatchDate = DateTime.UtcNow,
                                    dispatchTime = DateTime.UtcNow,
                                    movePurpose = 1,
                                    isDeliveryNote = true,
                                    otherDeliveryNoteHeader = new
                                    {
                                        loadingAddress = new { street = "ARKTINOY9", number = "229", postalCode = "11635", city = "ΑΘΗΝΑ9" },
                                        deliveryAddress = new { street = "ODOD1", number = "2", postalCode = "19200", city = "ATHINA" },
                                        startShippingBranch = 0,
                                        completeShippingBranch = 0
                                    }
                                },
                                packingsDeclarations = new[]
                                {
                                    new
                                    {
                                        Packages = new[]
                                        {
                                            new { packagingType = 2, quantity = 1 },
                                            new { packagingType = 4, quantity = 3 }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // Delivery notes need the issuer name and address, which come from master data.
            var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
            {
                Account = new storage.V0.MasterData.AccountMasterData { VatId = "112545020", AccountName = "Εκδότης Α.Ε." },
                Outlet = new storage.V0.MasterData.OutletMasterData { LocationId = "0", Street = "Λεωφόρος Βουλιαγμένης", Zip = "11636", City = "Αθηνών" }
            }, "https://test.receipts.example.com");

            (var invoiceDoc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, ExampleResponse);
            error.Should().BeNull();
            invoiceDoc!.invoice[0].PackingsDeclarations.Should().ContainSingle();

            var xml = AADEFactory.GenerateInvoicePayload(invoiceDoc);
            _output.WriteLine(xml);
            var mark = await SendToMayData(xml);
            mark.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task AADECertificationExamples_A1_1_1p2()
        {
            var receiptRequest = AADECertificationExamples.A1_1_1p2();
            await ValidateMyData(receiptRequest, InvoiceType.Item12, IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_005);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_1_1p3()
        {
            var receiptRequest = AADECertificationExamples.A1_1_1p3();
            await ValidateMyData(receiptRequest, InvoiceType.Item13, IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_006);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_1_1p5()
        {
            var receiptRequest = AADECertificationExamples.A1_1_1p5_1();
            await ValidateMyData(receiptRequest, InvoiceType.Item15, IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_001);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_1_1p5_2()
        {
            var receiptRequest = AADECertificationExamples.A1_1_1p5_2();
            await ValidateMyData(receiptRequest, InvoiceType.Item15, IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_001);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_2_2p2()
        {
            var receiptRequest = AADECertificationExamples.A1_2_2p2();
            await ValidateMyData(receiptRequest, InvoiceType.Item22, IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_005);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_2_2p3()
        {
            var receiptRequest = AADECertificationExamples.A1_2_2p3();
            await ValidateMyData(receiptRequest, InvoiceType.Item23, IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_006);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_3_3p1()
        {
            var receiptRequest = AADECertificationExamples.A1_3_3p1();
            await ValidateMyData(receiptRequest, InvoiceType.Item31);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_3_3p2()
        {
            var receiptRequest = AADECertificationExamples.A1_3_3p2();
            await ValidateMyData(receiptRequest, InvoiceType.Item32);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_6_6p1()
        {
            var receiptRequest = AADECertificationExamples.A1_6_6p1();
            await ValidateMyData(receiptRequest, InvoiceType.Item61, IncomeClassificationCategoryType.category1_6, IncomeClassificationValueType.E3_595);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_6_6p2()
        {
            var receiptRequest = AADECertificationExamples.A1_6_6p2();
            await ValidateMyData(receiptRequest, InvoiceType.Item62, IncomeClassificationCategoryType.category1_6, IncomeClassificationValueType.E3_595);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_7_7p1()
        {
            var receiptRequest = AADECertificationExamples.A1_7_7p1();
            await ValidateMyData(receiptRequest, InvoiceType.Item71, IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_007);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_8_8p1()
        {
            var receiptRequest = AADECertificationExamples.A1_8_8p1();
            await ValidateMyData(receiptRequest, InvoiceType.Item81, IncomeClassificationCategoryType.category1_5, IncomeClassificationValueType.E3_562);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_8_8p2()
        {
            var receiptRequest = AADECertificationExamples.A1_8_8p2();
            await ValidateMyData(receiptRequest, InvoiceType.Item82);
        }

        [Fact]
        public async Task AADECertificationExamples_A2_11_11p3()
        {
            var receiptRequest = AADECertificationExamples.A2_11_11p3();
            await ValidateMyData(receiptRequest, InvoiceType.Item113, IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_003);
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