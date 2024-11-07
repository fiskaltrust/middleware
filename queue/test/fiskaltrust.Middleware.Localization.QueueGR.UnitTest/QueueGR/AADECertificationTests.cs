using System.Text;
using System.Xml.Serialization;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest
{
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
                    VatId = "997671771"
                }
            });
        }

        public ResponseDoc? GetResponse(string xmlContent)
        {
            var xmlSerializer = new XmlSerializer(typeof(ResponseDoc));
            using var stringReader = new StringReader(xmlContent);
            return xmlSerializer.Deserialize(stringReader) as ResponseDoc;
        }

        private async Task SendToMayData(string xml)
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
            if (ersult != null)
            {
                var data = ersult.response[0];
                if (data.statusCode.ToLower() == "success")
                {
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

        }

        [Fact]
        public async void AADECertificationExamples_A1_1_1p1()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_1_1p1(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item11);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_2);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_561_001);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_1_1p2()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_1_1p2(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item12);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_2);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_561_005);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_1_1p3()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_1_1p3(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item13);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_2);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_561_006);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }
      
        [Fact]
        public async Task AADECertificationExamples_A1_1_1p4()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_1_1p4(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item14);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_7);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_881_003);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_1_1p5()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_1_1p5(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item15);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_1_1p6()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_1_1p6(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item16);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_2);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_561_001);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async void AADECertificationExamples_A1_2_2p1()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_2_2p1(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item21);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_3);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_561_001);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_2_2p2()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_2_2p2(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item22);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_3);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_561_005);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_2_2p3()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_2_2p3(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item23);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_3);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_561_006);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_2_2p4()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_2_2p4(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item24);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_3);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_561_001);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_3_3p1()
        {
            await Task.Yield();
            throw new NotImplementedException("");
        }

        [Fact]
        public async Task AADECertificationExamples_A1_3_3p2()
        {
            await Task.Yield();
            throw new NotImplementedException("");
        }

        [Fact]
        public async Task AADECertificationExamples_A1_5_5p1()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_5_5p1(Guid.NewGuid()), ExampleResponse);
            //&invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item51);
            //invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_3);
            //invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_561_006);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_5_5p2()
        {
            await Task.Yield();
            throw new NotImplementedException("");
        }

        [Fact]
        public async Task AADECertificationExamples_A1_6_6p1()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_6_6p1(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item61);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_6);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_595);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_6_6p2()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_6_6p2(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item62);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_6);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_595);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_7_7p1()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_7_7p1(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item71);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_3);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_561_007);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_8_8p1()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_8_8p1(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item81);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_5);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_562);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_8_8p2()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_8_8p2(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item82);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification.Should().BeEmpty();
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_8_8p4()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_8_8p4(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item84);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_95);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationTypeSpecified.Should().BeFalse();
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_8_8p5()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A1_8_8p5(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item85);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_95);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationTypeSpecified.Should().BeFalse();
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A2_11_11p1()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A2_11_11p1(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item111);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_2);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_561_003);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A2_11_11p2()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A2_11_11p2(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item112);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_3);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_561_003);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A2_11_11p3()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A2_11_11p3(Guid.NewGuid()), ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item113);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_2);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_561_003);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
        }

        [Fact]
        public async Task AADECertificationExamples_A2_11_11p4()
        {
            await Task.Yield();
            throw new NotImplementedException("");
        }

        [Fact]
        public async Task AADECertificationExamples_A2_11_11p5()
        {
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(AADECertificationExamples.A2_11_1p5(Guid.NewGuid()), ExampleResponse);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item115);
        }

        public ReceiptResponse ExampleResponse => new ReceiptResponse
        {
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftCashBoxIdentification = "cashBoxIdentification",
            ftReceiptIdentification = "ft" + DateTime.UtcNow.Ticks.ToString("X"),
            ftReceiptMoment = DateTime.UtcNow,
            ftState = 0x4752_2000_0000_0000
        };
    }
}