using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.GR.MyData;
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