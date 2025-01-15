using System.Net.Http.Json;
using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest
{
    public class FullTest()
    {
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
            var bootstrapper = new QueueGRBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration ?? new Dictionary<string, object>());
            return (bootstrapper, cashBoxId);
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
            var cashBoxId = Guid.Parse("e117e4b5-88ea-4511-a134-e5408f3cfd4c");
            var accessToken = "BBNu3xCxDz9VKOTQJQATmCzj1zQRjeE25DW/F8hcqsk/Uc5hHc4m1lEgd2QDsWLpa6MRDHz+vLlQs0hCprWt9XY=";
            var configuration = await GetConfigurationAsync(cashBoxId, accessToken);
            var queue = configuration.ftQueues?.First() ?? throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");
            var bootstrapper = new QueueGRBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration ?? new Dictionary<string, object>());
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
            var cashBoxId = Guid.Parse("e117e4b5-88ea-4511-a134-e5408f3cfd4c");
            var accessToken = "BBNu3xCxDz9VKOTQJQATmCzj1zQRjeE25DW/F8hcqsk/Uc5hHc4m1lEgd2QDsWLpa6MRDHz+vLlQs0hCprWt9XY=";
            var configuration = await GetConfigurationAsync(cashBoxId, accessToken);
            var queue = configuration.ftQueues?.First() ?? throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");
            var bootstrapper = new QueueGRBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration ?? new Dictionary<string, object>());
            var signMethod = bootstrapper.RegisterForSign();

            var ticks = DateTime.UtcNow.Ticks;
            var receiptRequest = ReceiptExamples.Example_RetailSales_100App2APp(cashBoxId);
            var raw = System.Text.Json.JsonSerializer.Serialize(receiptRequest);
            var exampleCashSalesResponse = await signMethod(raw);

            await StoreDataAsync("A11_1_Online_100", "A11_1_Online_100", ticks, bootstrapper, receiptRequest, System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!);
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
