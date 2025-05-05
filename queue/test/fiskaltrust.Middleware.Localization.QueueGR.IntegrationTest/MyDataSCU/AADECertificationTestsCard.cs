using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData;
using fiskaltrust.Middleware.Localization.QueueGR.UnitTest;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace fiskaltrust.Middleware.Localization.QueueGR.IntegrationTest.MyDataSCU
{
    [Trait("only", "local")]
    public class AADECertificationTestsCard
    {
        private readonly ITestOutputHelper _output;
        private readonly AADEFactory _aadeFactory;

        public AADECertificationTestsCard(ITestOutputHelper output)
        {
            _output = output;
            _aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
            {
                Account = new storage.V0.MasterData.AccountMasterData
                {
                    VatId = "112545020"
                }
            });
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

        private async Task ValidateMyData(ReceiptRequest receiptRequest, InvoiceType expectedInvoiceType, IncomeClassificationCategoryType expectedCategory, [CallerMemberName] string caller = "")
        {
            using var scope = new AssertionScope();
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(receiptRequest, ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(expectedInvoiceType);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(expectedCategory);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationTypeSpecified.Should().BeFalse();
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
            await ExecuteMiddleware(receiptRequest, caller);
        }

        private async Task ValidateMyData(ReceiptRequest receiptRequest, InvoiceType expectedInvoiceType, IncomeClassificationCategoryType expectedCategory, IncomeClassificationValueType expectedValueType, [CallerMemberName] string caller = "")
        {
            var payment = await SendPayRequest(receiptRequest.cbPayItems[0]);
            receiptRequest.cbPayItems[0] = payment!.ftPayItems[0];
            using var scope = new AssertionScope();
            var invoiceDoc = _aadeFactory.MapToInvoicesDoc(receiptRequest, ExampleResponse);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(expectedInvoiceType);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(expectedCategory);
            invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(expectedValueType);
            var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            await SendToMayData(xml);
            await ExecuteMiddleware(receiptRequest, caller);
        }

#pragma warning disable
        private async Task ExecuteMiddleware(ReceiptRequest receiptRequest, string caller)
        {
            (var bootstrapper, var cashBoxId) = await Initialization.InitializeQueueGRBootstrapperAsync();
            receiptRequest.ftCashBoxID = cashBoxId;
            var signMethod = bootstrapper.RegisterForSign();
            var ticks = DateTime.UtcNow.Ticks;
            var exampleCashSalesResponse = await signMethod(JsonSerializer.Serialize(receiptRequest));
            await StoreDataAsync(caller, caller, ticks, bootstrapper, receiptRequest, JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!);
        }


        public async Task StoreDataAsync(string folder, string casename, long ticks, QueueGRBootstrapper bootstrapper, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
        {
            var result = await Initialization.SendIssueAsync(receiptRequest, receiptResponse);
            var pdfdata = await new HttpClient().GetAsync(result?.DocumentURL + "?format=pdf");
            var pngdata = await new HttpClient().GetAsync(result?.DocumentURL + "?format=png");

            var journalMethod = bootstrapper.RegisterForJournal();
            var xmlData = await journalMethod(JsonSerializer.Serialize(new ifPOS.v1.JournalRequest
            {
                ftJournalType = 0x4752_2000_0000_0001,
                From = ticks
            }));
            var baseFolder = Path.Combine("C:\\temp", "viva_aade_certification_examples_card");
            var folderPath = Path.Combine(baseFolder, folder);
            Directory.CreateDirectory(Path.Combine(baseFolder, folder));
            File.WriteAllText(Path.Combine(folderPath, casename + ".receiptrequest.json"), JsonSerializer.Serialize(receiptRequest, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
            File.WriteAllText(Path.Combine(folderPath, casename + ".receiptresponse.json"), JsonSerializer.Serialize(receiptResponse, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
            File.WriteAllBytes(Path.Combine(folderPath, casename + ".receipt.pdf"), await pdfdata.Content.ReadAsByteArrayAsync());
            File.WriteAllBytes(Path.Combine(folderPath, casename + ".receipt.png"), await pngdata.Content.ReadAsByteArrayAsync());
            File.WriteAllText(Path.Combine(folderPath, casename + "_aade.xml"), xmlData);
        }

        [Fact]
        public async void AADECertificationExamples_A1_1_1p1()
        {
            var receiptRequest = AADECertificationExamplesCard.A1_1_1p1();
            await ValidateMyData(receiptRequest, InvoiceType.Item11, IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_001);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_1_1p4()
        {
            var receiptRequest = AADECertificationExamplesCard.A1_1_1p4();
            await ValidateMyData(receiptRequest, InvoiceType.Item14, IncomeClassificationCategoryType.category1_7, IncomeClassificationValueType.E3_881_001);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_1_1p6()
        {
            var receiptRequest = AADECertificationExamplesCard.A1_1_1p6();
            await ValidateMyData(receiptRequest, InvoiceType.Item16, IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_001);
        }

        [Fact]
        public async void AADECertificationExamples_A1_2_2p1()
        {
            var receiptRequest = AADECertificationExamplesCard.A1_2_2p1();
            await ValidateMyData(receiptRequest, InvoiceType.Item21, IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_001);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_2_2p4()
        {
            var receiptRequest = AADECertificationExamplesCard.A1_2_2p4();
            await ValidateMyData(receiptRequest, InvoiceType.Item24, IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_001);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_5_5p1()
        {

            //var invoiceOriginal = _aadeFactory.MapToInvoicesDoc(AADECertificationExamplesCard.A1_1_1p1(), ExampleResponse);
            //var marker = await SendToMayData(_aadeFactory.GenerateInvoicePayload(invoiceOriginal));

            var creditnote = AADECertificationExamplesCard.A1_5_5p1();
            creditnote.cbPreviousReceiptReference = "400001941996088";
            await Task.Delay(1000);
            //var invoiceDoc = _aadeFactory.MapToInvoicesDoc(creditnote, ExampleResponse);
            //using var assertionScope = new AssertionScope();
            //invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item51);
            //invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_2);
            //invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_561_001);
            //var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
            //await SendToMayData(xml);
            var payment = await Initialization.SendPayRequest(creditnote.cbPayItems[0]);
            creditnote.cbPayItems[0] = payment!.ftPayItems[0];
            await ExecuteMiddleware(creditnote, "AADECertificationExamples_A1_5_5p1");
        }

        [Fact]
        public async Task AADECertificationExamples_A1_5_5p2()
        {
            var receiptRequest = AADECertificationExamplesCard.A1_5_5p2();
            await ValidateMyData(receiptRequest, InvoiceType.Item52, IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_001);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_8_8p4()
        {
            var receiptRequest = AADECertificationExamplesCard.A1_8_8p4();
            var payment = await Initialization.SendPayRequest(receiptRequest.cbPayItems[0]);
            receiptRequest.cbPayItems[0] = payment!.ftPayItems[0];
            await ValidateMyData(receiptRequest, InvoiceType.Item84, IncomeClassificationCategoryType.category1_95);
        }

        [Fact]
        public async Task AADECertificationExamples_A1_8_8p5()
        {
            var receiptRequest = AADECertificationExamplesCard.A1_8_8p5();
            var payment = await Initialization.SendPayRequestGetOperationId(receiptRequest.cbPayItems[0]);

            var refund = await Initialization.SendRefundRequest(payment.sessionid, receiptRequest.cbPayItems[0]);
            receiptRequest.cbPayItems[0] = refund!.ftPayItems[0];
            receiptRequest.cbPayItems[0].Amount = -receiptRequest.cbPayItems[0].Amount;
            await ValidateMyData(receiptRequest, InvoiceType.Item85, IncomeClassificationCategoryType.category1_95);
        }

        [Fact]
        public async Task AADECertificationExamples_A2_11_11p1()
        {
            var receiptRequest = AADECertificationExamplesCard.A2_11_11p1();
            await ValidateMyData(receiptRequest, InvoiceType.Item111, IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_003);
        }

        [Fact]
        public async Task AADECertificationExamples_A2_11_11p2()
        {
            var receiptRequest = AADECertificationExamplesCard.A2_11_11p2();
            await ValidateMyData(receiptRequest, InvoiceType.Item112, IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_003);
        }


        [Fact]
        public async Task AADECertificationExamples_A2_11_11p4()
        {
            var receiptRequest = AADECertificationExamplesCard.A2_11_11p4();
            await ValidateMyData(receiptRequest, InvoiceType.Item114, IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_001);
        }

        [Fact]
        public async Task AADECertificationExamples_A2_11_11p5()
        {
            var receiptRequest = AADECertificationExamplesCard.A2_11_1p5();
            await ValidateMyData(receiptRequest, InvoiceType.Item115, IncomeClassificationCategoryType.category1_7, IncomeClassificationValueType.E3_881_003);
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