using System.Net.Http.Json;
using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest
{
    [Trait("only", "local")]
    public class FullTest()
    {
        private readonly MyDataSCU _myDataSCU = new MyDataSCU("", "", "https://mydataapidev.aade.gr/", "https://receipts-sandbox.fiskaltrust.eu", new MasterDataConfiguration
        {
            Account = new AccountMasterData
            {
                VatId = "112545020"
            },
        });

        public async Task<ftCashBoxConfiguration> GetConfigurationAsync(Guid cashBoxId, string accessToken)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri("https://helipad-sandbox.fiskaltrust.cloud");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("cashboxid", cashBoxId.ToString());
                httpClient.DefaultRequestHeaders.Add("accesstoken", accessToken);
                var result = await httpClient.GetAsync("api/configuration");
                var content = await result.Content.ReadAsStringAsync();
                if (result.IsSuccessStatusCode)
                {
                    if (string.IsNullOrEmpty(content))
                    {
                        throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");
                    }

                    var configuration = Newtonsoft.Json.JsonConvert.DeserializeObject<ftCashBoxConfiguration>(content) ?? throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");
                    configuration.TimeStamp = DateTime.UtcNow.Ticks;
                    return configuration;
                }
                else
                {
                    throw new Exception($"{content}");
                }
            }
        }

        public async Task<(QueueGRBootstrapper bootstrapper, Guid cashBoxId)> InitializeQueueGRBootstrapperAsync()
        {
            var cashBoxId = Guid.Parse("e117e4b5-88ea-4511-a134-e5408f3cfd4c");
            var accessToken = "BBNu3xCxDz9VKOTQJQATmCzj1zQRjeE25DW/F8hcqsk/Uc5hHc4m1lEgd2QDsWLpa6MRDHz+vLlQs0hCprWt9XY=";
            var configuration = await GetConfigurationAsync(cashBoxId, accessToken);
            var queue = configuration.ftQueues?.First() ?? throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");

            var bootstrapper = new QueueGRBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration ?? new Dictionary<string, object>(), _myDataSCU);
            return (bootstrapper, cashBoxId);
        }

        [Fact]
        public async Task Example_RetailSales_Tests2()
        {
            (var bootstrapper, var cashBoxId) = await InitializeQueueGRBootstrapperAsync();
            var signMethod = bootstrapper.RegisterForSign();
            var receiptRequest = Examples.A1_WithDiscountCase();
            receiptRequest.ftCashBoxID = cashBoxId;
            var exampleCashSalesResponse = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequest));
            var d = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!;
            d.ftState.IsState(State.Success).Should().BeTrue(string.Join(Environment.NewLine, d.ftSignatures.Select(x => x.Data)));
        }

        [Fact]
        public async Task ExampleRquests_()
        {
            (var bootstrapper, var cashBoxId) = await InitializeQueueGRBootstrapperAsync();
            var signMethod = bootstrapper.RegisterForSign();
            var receiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(File.ReadAllText("C:\\GitHub\\middleware\\queue\\test\\fiskaltrust.Middleware.Localization.QueueGR.IntegrationTest\\Examples\\MultiAfterCommaDigits.json"))!;
            receiptRequest.ftCashBoxID = cashBoxId;
            var exampleCashSalesResponse = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequest));
            var d = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!;

            var xml = new AADEFactory(new MasterDataConfiguration
            {
                Account = new AccountMasterData
                {
                    VatId = "112545020"
                },
            }).MapToInvoicesDoc(receiptRequest, new ReceiptResponse
            {
                ftCashBoxIdentification = cashBoxId.ToString(),
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "ft#123-1233",
                ftReceiptMoment = DateTime.UtcNow,
                ftState = 0
            });
            d.ftState.IsState(State.Success).Should().BeTrue(string.Join(Environment.NewLine, d.ftSignatures.Select(x => x.Data)));
        }


        [Fact]
        public async Task ExampleRquests_MutliChargeItems()
        {
            (var bootstrapper, var cashBoxId) = await InitializeQueueGRBootstrapperAsync();
            var signMethod = bootstrapper.RegisterForSign();
            var receiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(File.ReadAllText("C:\\GitHub\\middleware\\queue\\test\\fiskaltrust.Middleware.Localization.QueueGR.IntegrationTest\\Examples\\MutliChargeItems.json"))!;
            receiptRequest.ftCashBoxID = cashBoxId;
            var exampleCashSalesResponse = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequest));
            var d = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!;
            var aadeFactory = new AADEFactory(new MasterDataConfiguration
            {
                Account = new AccountMasterData
                {
                    VatId = "112545020"
                },
            });

            var xml = aadeFactory.MapToInvoicesDoc(receiptRequest, new ReceiptResponse
            {
                ftCashBoxIdentification = cashBoxId.ToString(),
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "ft123#1233",
                ftReceiptMoment = DateTime.UtcNow,
                ftState = 0
            });

            var data = aadeFactory.GenerateInvoicePayload(xml); 

            d.ftState.IsState(State.Success).Should().BeTrue(string.Join(Environment.NewLine, d.ftSignatures.Select(x => x.Data)));
        }


        [Fact]
        public async Task Example_RetailSales_TestsRefund()
        {
            (var bootstrapper, var cashBoxId) = await InitializeQueueGRBootstrapperAsync();
            var signMethod = bootstrapper.RegisterForSign();
            var receiptRequest = Examples.A2_ReceiptRefund();
            receiptRequest.ftCashBoxID = cashBoxId;
            var exampleCashSalesResponse = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequest));
            var d = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!;
            d.ftState.IsState(State.Success).Should().BeTrue(string.Join(Environment.NewLine, d.ftSignatures.Select(x => x.Data)));
        }


        [Fact]
        public async Task Example_RetailSales_Tests()
        {
            (var bootstrapper, var cashBoxId) = await InitializeQueueGRBootstrapperAsync();
            var signMethod = bootstrapper.RegisterForSign();
            var receiptRequest = ReceiptExamples.Example_RetailSales(cashBoxId);
            var exampleCashSalesResponse = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequest));
            var result = await SendIssueAsync(receiptRequest, System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!);
        }

        [Fact]
        public async Task Example_RetailSales_Error2_Tests()
        {
            (var bootstrapper, var cashBoxId) = await InitializeQueueGRBootstrapperAsync();
            var signMethod = bootstrapper.RegisterForSign();
            var ticks = DateTime.UtcNow.Ticks;
            var receiptRequest = ReceiptExamples.Example_RetailSales(cashBoxId);
            var exampleCashSalesResponse = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequest));
            await StoreDataAsync("A11_1_offline_2", "A11_1_offline_2", ticks, bootstrapper, receiptRequest, System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!);
        }

        [Fact]
        public async Task Example_RetailSales_LateSigning_Tests()
        {
            (var bootstrapper, var cashBoxId) = await InitializeQueueGRBootstrapperAsync();
            var signMethod = bootstrapper.RegisterForSign();
            var ticks = DateTime.UtcNow.Ticks;
            var receiptRequest = ReceiptExamples.Example_RetailSales(cashBoxId);
            receiptRequest.ftReceiptCase = receiptRequest.ftReceiptCase.WithFlag(ReceiptCaseFlags.LateSigning);
            var exampleCashSalesResponse = await signMethod(JsonSerializer.Serialize(receiptRequest));
            await StoreDataAsync("A11_1_offline_1", "A11_1_offline_1", ticks, bootstrapper, receiptRequest, System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!);
        }

        [Fact]
        public async Task Example_SalesInvoice_1_1_Tests()
        {
            (var bootstrapper, var cashBoxId) = await InitializeQueueGRBootstrapperAsync();
            var signMethod = bootstrapper.RegisterForSign();
            var ticks = DateTime.UtcNow.Ticks;
            var receiptRequest = ReceiptExamples.Example_SalesInvoice_1_1(cashBoxId);
            var exampleCashSalesResponse = await signMethod(JsonSerializer.Serialize(receiptRequest));
            await StoreDataAsync("A1_1", "A1_1", ticks, bootstrapper, receiptRequest, System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!);
        }

        [Fact]
        public async Task Example_SalesInvoice_1_1_Tests_nowithholding()
        {
            (var bootstrapper, var cashBoxId) = await InitializeQueueGRBootstrapperAsync();
            var signMethod = bootstrapper.RegisterForSign();
            var ticks = DateTime.UtcNow.Ticks;
            var receiptRequest = ReceiptExamples.Example_SalesInvoice_1_1(cashBoxId);
            var exampleCashSalesResponse = await signMethod(JsonSerializer.Serialize(receiptRequest));
            await StoreDataAsync("A1_1", "A1_1", ticks, bootstrapper, receiptRequest, System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!);
        }

        [Fact]
        public async Task Example_POSReceipt_Tests()
        {
            (var bootstrapper, var cashBoxId) = await InitializeQueueGRBootstrapperAsync();
            var signMethod = bootstrapper.RegisterForSign();
            var ticks = DateTime.UtcNow.Ticks;
            var receiptRequest = ReceiptExamples.ExamplePosReceipt(cashBoxId);
            var exampleCashSalesResponse = await signMethod(JsonSerializer.Serialize(receiptRequest));
            await StoreDataAsync("A1_8_4", "A1_8_4", ticks, bootstrapper, receiptRequest, System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!);
        }

        [Fact]
        public async Task Example_POSReceipt_Testss_A11_1_Online_100()
        {
            var (cashBoxId, queue) = await GetQueue();
            var bootstrapper = new QueueGRBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration ?? new Dictionary<string, object>(), null!);
            var signMethod = bootstrapper.RegisterForSign();

            var ticks = DateTime.UtcNow.Ticks;
            var receiptRequest = ReceiptExamples.Example_RetailSales_100(cashBoxId);
            var raw = System.Text.Json.JsonSerializer.Serialize(receiptRequest);
            var exampleCashSalesResponse = await signMethod(raw);

            await StoreDataAsync("A11_1_Online_100", "A11_1_Online_100", ticks, bootstrapper, receiptRequest, System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!);
        }

        [Fact]
        public async Task Example_POSReceipt_Testss_A11_1_Online_100_App2App()
        {
            var (cashBoxId, queue) = await GetQueue();
            var bootstrapper = new QueueGRBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration ?? new Dictionary<string, object>(), null!);
            var signMethod = bootstrapper.RegisterForSign();

            var ticks = DateTime.UtcNow.Ticks;
            var receiptRequest = ReceiptExamples.Example_RetailSales_100App2APp(cashBoxId);
            var raw = System.Text.Json.JsonSerializer.Serialize(receiptRequest);
            var exampleCashSalesResponse = await signMethod(raw);

            await StoreDataAsync("A11_1_Online_100", "A11_1_Online_100", ticks, bootstrapper, receiptRequest, System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!);
        }

        private async Task<(Guid cashBoxId, PackageConfiguration queue)> GetQueue()
        {
            var cashBoxId = Guid.Parse("e117e4b5-88ea-4511-a134-e5408f3cfd4c");
            var accessToken = "BBNu3xCxDz9VKOTQJQATmCzj1zQRjeE25DW/F8hcqsk/Uc5hHc4m1lEgd2QDsWLpa6MRDHz+vLlQs0hCprWt9XY=";
            var configuration = await GetConfigurationAsync(cashBoxId, accessToken);
            var queue = configuration.ftQueues?.First() ?? throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");
            return (cashBoxId, queue);
        }

        public async Task StoreDataAsync(string folder, string casename, long ticks, QueueGRBootstrapper bootstrapper, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
        {
            var result = await SendIssueAsync(receiptRequest, receiptResponse);

            var pdfdata = await new HttpClient().GetAsync(result?.DocumentURL + "?format=pdf");

            var journalMethod = bootstrapper.RegisterForJournal();
            var xmlData = await journalMethod(System.Text.Json.JsonSerializer.Serialize(new ifPOS.v1.JournalRequest
            {
                ftJournalType = 0x4752_2000_0000_0001,
                From = ticks
            }));
            Directory.CreateDirectory("C:\\temp\\viva_examples\\" + folder);
            File.WriteAllBytes($"C:\\temp\\viva_examples\\{folder}\\{casename}.receipt.pdf", await pdfdata.Content.ReadAsByteArrayAsync());
            File.WriteAllText($"C:\\temp\\viva_examples\\{folder}\\{casename}_aade.xml", xmlData);
        }

        private async Task<IssueResponse?> SendIssueAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://possystem-api-sandbox.fiskaltrust.eu/v2/issue");
            request.Headers.Add("x-cashbox-id", "e117e4b5-88ea-4511-a134-e5408f3cfd4c");
            request.Headers.Add("x-cashbox-accesstoken", "BBNu3xCxDz9VKOTQJQATmCzj1zQRjeE25DW/F8hcqsk/Uc5hHc4m1lEgd2QDsWLpa6MRDHz+vLlQs0hCprWt9XY=");
            var data = JsonSerializer.Serialize(new
            {
                ReceiptRequest = receiptRequest,
                ReceiptResponse = receiptResponse
            });
            request.Headers.Add("x-operation-id", Guid.NewGuid().ToString());
            var content = new StringContent(data, null, "application/json");
            request.Content = content;
            var response = await client.SendAsync(request);
            return await response.Content.ReadFromJsonAsync<IssueResponse>();
        }
    }
}
