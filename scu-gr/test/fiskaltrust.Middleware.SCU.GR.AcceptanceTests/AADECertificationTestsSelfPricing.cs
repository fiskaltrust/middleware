//using System.Runtime.CompilerServices;
//using System.Text.Json;
//using fiskaltrust.ifPOS.v2;
//using fiskaltrust.ifPOS.v2.Cases;
//using FluentAssertions.Execution;
//using Xunit;

//namespace fiskaltrust.Middleware.SCU.GR.IntegrationTest.MyDataSCU
//{

//    [Trait("only", "local")]
//    public class AADECertificationTestsSelfPricing
//    {
//        private async Task ValidateMyData(ReceiptRequest receiptRequest, [CallerMemberName] string caller = "")
//        {
//            using var scope = new AssertionScope();
//            await ExecuteMiddleware(receiptRequest, caller);
//        }

//        private async Task ExecuteMiddleware(ReceiptRequest receiptRequest, string caller)
//        {
//            (var bootstrapper, var cashBoxId) = await Initialization.InitializeQueueGRBootstrapperAsync();
//            receiptRequest.ftCashBoxID = cashBoxId;
//            var signMethod = bootstrapper.RegisterForSign();
//            var ticks = DateTime.UtcNow.Ticks;
//            var exampleCashSalesResponse = await signMethod(JsonSerializer.Serialize(receiptRequest));
//            var receiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!;
//            if (((long) receiptResponse.ftState & 0x0000_0000_0000_FFFF) != 0x0000)
//            {
//                throw new Exception(exampleCashSalesResponse);
//            }
//            await StoreDataAsync(caller, caller, ticks, bootstrapper, receiptRequest, receiptResponse);
//        }

//        public async Task StoreDataAsync(string folder, string casename, long ticks, QueueGRBootstrapper bootstrapper, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            var result = await Initialization.SendIssueAsync(receiptRequest, receiptResponse);
//            var pdfdata = await new HttpClient().GetAsync(result?.DocumentURL + "?format=pdf");
//            var pngdata = await new HttpClient().GetAsync(result?.DocumentURL + "?format=png");

//            var journalMethod = bootstrapper.RegisterForJournal();
//            var xmlData = await journalMethod(JsonSerializer.Serialize(new ifPOS.v1.JournalRequest
//            {
//                ftJournalType = 0x4752_2000_0000_0001,
//                From = ticks
//            }));
//            var baseFolder = Path.Combine("C:\\temp", "viva_aade_certification_examples_selfpricing");
//            var folderPath = Path.Combine(baseFolder, folder);
//            Directory.CreateDirectory(Path.Combine(baseFolder, folder));
//            File.WriteAllText(Path.Combine(folderPath, casename + ".receiptrequest.json"), JsonSerializer.Serialize(receiptRequest, new JsonSerializerOptions
//            {
//                WriteIndented = true
//            }));
//            File.WriteAllText(Path.Combine(folderPath, casename + ".receiptresponse.json"), JsonSerializer.Serialize(receiptResponse, new JsonSerializerOptions
//            {
//                WriteIndented = true
//            }));
//            File.WriteAllBytes(Path.Combine(folderPath, casename + ".receipt.pdf"), await pdfdata.Content.ReadAsByteArrayAsync());
//            File.WriteAllBytes(Path.Combine(folderPath, casename + ".receipt.png"), await pngdata.Content.ReadAsByteArrayAsync());
//            File.WriteAllText(Path.Combine(folderPath, casename + "_aade.xml"), xmlData);
//        }

//        [Fact]
//        public async void JOurnal()
//        {
//            (var bootstrapper, var cashBoxId) = await Initialization.InitializeQueueGRBootstrapperAsync();
//            var journalMethod = bootstrapper.RegisterForJournal();
//            var xmlData = await journalMethod(JsonSerializer.Serialize(new ifPOS.v1.JournalRequest
//            {
//                ftJournalType = 0x4752_2000_0000_0001,
//                From = 0
//            }));
//        }

//        [Fact]
//        public async void AADECertificationExamples_A1_1_1p1()
//        {
//            var receiptRequest = AADECertificationExamplesSelfPricing.A1_1_1p1();
//            await ValidateMyData(receiptRequest, InvoiceType.Item11, IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_001);
//        }

//        [Fact]
//        public async Task AADECertificationExamples_A1_1_1p4()
//        {
//            var receiptRequest = AADECertificationExamplesSelfPricing.A1_1_1p4();
//            await ValidateMyData(receiptRequest, InvoiceType.Item14, IncomeClassificationCategoryType.category1_7, IncomeClassificationValueType.E3_881_003);
//        }

//        [Fact]
//        public async Task AADECertificationExamples_A1_1_1p5()
//        {
//            var receiptRequest = AADECertificationExamplesSelfPricing.A1_1_1p5_1();
//            await ValidateMyData(receiptRequest, InvoiceType.Item15, IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_001);
//        }

//        [Fact]
//        public async Task AADECertificationExamples_A1_1_1p5_2()
//        {
//            var receiptRequest = AADECertificationExamplesSelfPricing.A1_1_1p5_2();
//            await ValidateMyData(receiptRequest, InvoiceType.Item15, IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_001);
//        }

//        [Fact]
//        public async Task AADECertificationExamples_A1_1_1p6()
//        {
//            var receiptRequest = AADECertificationExamplesSelfPricing.A1_1_1p6();
//            await ValidateMyData(receiptRequest, InvoiceType.Item16, IncomeClassificationCategoryType.category1_2, IncomeClassificationValueType.E3_561_001);
//        }

//        [Fact]
//        public async void AADECertificationExamples_A1_2_2p1()
//        {
//            var receiptRequest = AADECertificationExamplesSelfPricing.A1_2_2p1();
//            await ValidateMyData(receiptRequest, InvoiceType.Item21, IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_001);
//        }

//        [Fact]
//        public async Task AADECertificationExamples_A1_2_2p4()
//        {
//            var receiptRequest = AADECertificationExamplesSelfPricing.A1_2_2p4();
//            await ValidateMyData(receiptRequest, InvoiceType.Item24, IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_001);
//        }

//        [Fact]
//        public async Task AADECertificationExamples_A1_5_5p1()
//        {

//            //var invoiceOriginal = _aadeFactory.MapToInvoicesDoc(AADECertificationExamplesSelfPricing.A1_1_1p1(), ExampleResponse);
//            //var marker = await SendToMayData(_aadeFactory.GenerateInvoicePayload(invoiceOriginal));

//            var creditnote = AADECertificationExamplesSelfPricing.A1_5_5p1();
//            creditnote.cbPreviousReceiptReference = "400001942899521";
//            await Task.Delay(1000);
//            //var invoiceDoc = _aadeFactory.MapToInvoicesDoc(creditnote, ExampleResponse);
//            //using var assertionScope = new AssertionScope();
//            //invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item51);
//            //invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_2);
//            //invoiceDoc.invoice[0].invoiceSummary.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_561_001);
//            //var xml = _aadeFactory.GenerateInvoicePayload(invoiceDoc);
//            //await SendToMayData(xml);

//            await ExecuteMiddleware(creditnote, "AADECertificationExamples_A1_5_5p1");
//        }

//        [Fact]
//        public async Task AADECertificationExamples_A1_5_5p2()
//        {
//            var receiptRequest = AADECertificationExamplesSelfPricing.A1_5_5p2();
//            await ValidateMyData(receiptRequest, InvoiceType.Item52, IncomeClassificationCategoryType.category1_3, IncomeClassificationValueType.E3_561_001);
//        }

//        public ReceiptResponse ExampleResponse => new ReceiptResponse
//        {
//            ftQueueID = Guid.NewGuid(),
//            ftQueueItemID = Guid.NewGuid(),
//            ftQueueRow = 1,
//            ftCashBoxIdentification = "cashBoxIdentification",
//            ftReceiptIdentification = "ft" + DateTime.UtcNow.Ticks.ToString("X"),
//            ftReceiptMoment = DateTime.UtcNow,
//            ftState = (State) 0x4752_2000_0000_0000
//        };
//    }
//}