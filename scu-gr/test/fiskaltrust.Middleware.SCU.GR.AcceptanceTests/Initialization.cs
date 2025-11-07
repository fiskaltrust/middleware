//using System.Net.Http.Json;
//using System.Text.Json;
//using System.Text.Json.Serialization;
//using fiskaltrust.ifPOS.v2;
//using fiskaltrust.Middleware.Localization.QueueGR.UnitTest;
//using Microsoft.Extensions.Logging;

//namespace fiskaltrust.Middleware.SCU.GR.IntegrationTest.MyDataSCU
//{
//    public class ftCashBoxConfiguration
//    {
//        [JsonPropertyName("helpers")]
//        public List<PackageConfiguration>? helpers { get; set; }

//        [JsonPropertyName("ftCashBoxId")]
//        public Guid ftCashBoxId { get; private set; }

//        [JsonPropertyName("ftSignaturCreationDevices")]
//        public List<PackageConfiguration>? ftSignaturCreationDevices { get; set; }

//        [JsonPropertyName("ftQueues")]
//        public List<PackageConfiguration>? ftQueues { get; set; }

//        [JsonPropertyName("PackTimeStampage")]
//        public long TimeStamp { get; set; }
//    }
//    public class PackageConfiguration
//    {
//        [JsonPropertyName("Id")]
//        public Guid Id { get; set; }

//        [JsonPropertyName("Package")]
//        public string Package { get; set; }

//        [JsonPropertyName("Version")]
//        public string Version { get; set; }

//        [JsonPropertyName("Configuration")]
//        public Dictionary<string, object>? Configuration { get; set; }

//        [JsonPropertyName("Url")]
//        public List<string>? Url { get; set; }

//        public PackageConfiguration()
//        {
//            Id = Guid.Empty;
//            Package = string.Empty;
//            Version = string.Empty;
//            Configuration = null;
//            Url = null;
//        }
//    }

//    public class Initialization
//    {
//        public static async Task<ftCashBoxConfiguration> GetConfigurationAsync(Guid cashBoxId, string accessToken)
//        {
//            using (var httpClient = new HttpClient())
//            {
//                httpClient.BaseAddress = new Uri("https://helipad-sandbox.fiskaltrust.cloud");
//                httpClient.DefaultRequestHeaders.Clear();
//                httpClient.DefaultRequestHeaders.Add("cashboxid", cashBoxId.ToString());
//                httpClient.DefaultRequestHeaders.Add("accesstoken", accessToken);
//                var result = await httpClient.GetAsync("api/configuration");
//                var content = await result.Content.ReadAsStringAsync();
//                if (result.IsSuccessStatusCode)
//                {
//                    if (string.IsNullOrEmpty(content))
//                    {
//                        throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");
//                    }

//                    var configuration = Newtonsoft.Json.JsonConvert.DeserializeObject<ftCashBoxConfiguration>(content) ?? throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");
//                    configuration.TimeStamp = DateTime.UtcNow.Ticks;
//                    return configuration;
//                }
//                else
//                {
//                    throw new Exception($"{content}");
//                }
//            }
//        }

//        public static async Task<(QueueGRBootstrapper bootstrapper, Guid cashBoxId)> InitializeQueueGRBootstrapperAsync()
//        {
//            var cashBoxId = Guid.Parse(Constants.CASHBOX_CERTIFICATION_ID);
//            var accessToken = Constants.CASHBOX_CERTIFICATION_ACCESSTOKEN;
//            var configuration = await Initialization.GetConfigurationAsync(cashBoxId, accessToken);
//            var queue = configuration.ftQueues?.First() ?? throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");
//            var bootstrapper = new QueueGRBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration ?? new Dictionary<string, object>(), null!);
//            return (bootstrapper, cashBoxId);
//        }

//        public static async Task<IssueResponse?> SendIssueAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {

//            receiptRequest.cbReceiptAmount = Math.Abs(receiptRequest.cbReceiptAmount ?? 0.0m);
//            foreach (var chargeItem in receiptRequest.cbChargeItems)
//            {
//                chargeItem.Amount = Math.Abs(chargeItem.Amount);
//            }
//            foreach (var payItem in receiptRequest.cbPayItems)
//            {
//                payItem.Amount = Math.Abs(payItem.Amount);
//            }

//            var client = new HttpClient();
//            var request = new HttpRequestMessage(HttpMethod.Post, "https://possystem-api-sandbox.fiskaltrust.eu/v2/issue");
//            var cashBoxId = Guid.Parse(Constants.CASHBOX_CERTIFICATION_ID);
//            var accessToken = Constants.CASHBOX_CERTIFICATION_ACCESSTOKEN;
//            request.Headers.Add("x-cashbox-id", cashBoxId.ToString());
//            request.Headers.Add("x-cashbox-accesstoken", accessToken);
//            var data = JsonSerializer.Serialize(new
//            {
//                ReceiptRequest = receiptRequest,
//                ReceiptResponse = receiptResponse
//            });
//            request.Headers.Add("x-operation-id", Guid.NewGuid().ToString());
//            var content = new StringContent(data, null, "application/json");
//            request.Content = content;
//            var response = await client.SendAsync(request);
//            return await response.Content.ReadFromJsonAsync<IssueResponse>();
//        }

//        public static async Task<PayResponse?> SendRefundRequest(string operationId, PayItem payItem)
//        {
//            var client = new HttpClient();
//            var request = new HttpRequestMessage(HttpMethod.Post, "https://possystem-api-sandbox.fiskaltrust.eu/v2/pay");
//            var cashBoxId = Guid.Parse(Constants.CASHBOX_CERTIFICATION_ID);
//            var accessToken = Constants.CASHBOX_CERTIFICATION_ACCESSTOKEN;
//            request.Headers.Add("x-cashbox-id", cashBoxId.ToString());
//            request.Headers.Add("x-cashbox-accesstoken", accessToken);
//            request.Headers.Add("x-operation-id", Guid.NewGuid().ToString());
//            var content = new StringContent("{\r\n   " +
//                "\"Action\": \"refund\"," +
//                "\"Protocol\": \"viva_eft_pos\"," +
//                "\"cbPayItem\": {" +
//                    $"\"{nameof(PayItem.MoneyBarcode)}\": \"{operationId}\",\r\n        " +
//                    $"\"Position\": {payItem.Position},\r\n        " +
//                    $"\"Quantity\": {payItem.Quantity},\r\n        " +
//                    $"\"Description\": \"{payItem.Description}\",\r\n        " +
//                    $"\"Amount\": {Math.Abs(payItem.Amount)},\r\n        " +
//                    $"\"ftPayItemCase\": {payItem.ftPayItemCase}\r\n    " +
//                    "},\r\n    \"cbTerminalId\": \"16009303\"\r\n}", null, "application/json");
//            request.Content = content;
//            var response = await client.SendAsync(request);
//            if (!response.IsSuccessStatusCode)
//            {
//                throw new Exception(await response.Content.ReadAsStringAsync());
//            }
//            return await response.Content.ReadFromJsonAsync<PayResponse>();
//        }


//        public static async Task<(PayResponse?, string sessionid)> SendPayRequestGetOperationId(PayItem payItem)
//        {
//            var operationId = Guid.NewGuid().ToString();
//            var client = new HttpClient();
//            var request = new HttpRequestMessage(HttpMethod.Post, "https://possystem-api-sandbox.fiskaltrust.eu/v2/pay");
//            var cashBoxId = Guid.Parse(Constants.CASHBOX_CERTIFICATION_ID);
//            var accessToken = Constants.CASHBOX_CERTIFICATION_ACCESSTOKEN;
//            request.Headers.Add("x-cashbox-id", cashBoxId.ToString());
//            request.Headers.Add("x-cashbox-accesstoken", accessToken);
//            request.Headers.Add("x-operation-id", operationId);
//            var content = new StringContent("{\r\n   " +
//                "\"Action\": \"payment\"," +
//                "\"Protocol\": \"viva_eft_pos\"," +
//                "\"cbPayItem\": {" +
//                    $"\"Position\": {payItem.Position},\r\n        " +
//                    $"\"Quantity\": {payItem.Quantity},\r\n        " +
//                    $"\"Description\": \"{payItem.Description}\",\r\n        " +
//                    $"\"Amount\": {Math.Abs(payItem.Amount)},\r\n        " +
//                    $"\"ftPayItemCase\": {payItem.ftPayItemCase}\r\n    " +
//                    "},\r\n    \"cbTerminalId\": \"16009303\"\r\n}", null, "application/json");
//            request.Content = content;
//            var response = await client.SendAsync(request);
//            return (await response.Content.ReadFromJsonAsync<PayResponse>(), operationId);
//        }

//        public static async Task<PayResponse?> SendPayRequest(PayItem payItem)
//        {
//            var client = new HttpClient();
//            var request = new HttpRequestMessage(HttpMethod.Post, "https://possystem-api-sandbox.fiskaltrust.eu/v2/pay");
//            var cashBoxId = Guid.Parse(Constants.CASHBOX_CERTIFICATION_ID);
//            var accessToken = Constants.CASHBOX_CERTIFICATION_ACCESSTOKEN;
//            request.Headers.Add("x-cashbox-id", cashBoxId.ToString());
//            request.Headers.Add("x-cashbox-accesstoken", accessToken);
//            request.Headers.Add("x-operation-id", Guid.NewGuid().ToString());
//            var content = new StringContent("{\r\n   " +
//                "\"Action\": \"payment\"," +
//                "\"Protocol\": \"viva_eft_pos_instore\"," +
//                "\"cbPayItem\": {" +
//                    $"\"Position\": {payItem.Position},\r\n        " +
//                    $"\"Quantity\": {payItem.Quantity},\r\n        " +
//                    $"\"Description\": \"{payItem.Description}\",\r\n        " +
//                    $"\"Amount\": {Math.Abs(payItem.Amount)},\r\n        " +
//                    $"\"ftPayItemCase\": {payItem.ftPayItemCase}\r\n    " +
//                    "},\r\n    \"cbTerminalId\": \"16009303\"\r\n}", null, "application/json");
//            request.Content = content;
//            var response = await client.SendAsync(request);
//            if (!response.IsSuccessStatusCode)
//            {
//                throw new Exception(await response.Content.ReadAsStringAsync());
//            }
//            return await response.Content.ReadFromJsonAsync<PayResponse>();
//        }
//    }
//}