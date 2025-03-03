using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest;

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

    [Fact]
    public async Task FullTests()
    {
        var cashBoxId = Guid.Parse("3b88c673-025c-4358-ab7f-4234e4c1a068");
        var accessToken = "BPYu7kfJa64JcdbAdF9/AJNNHjxQpqRMQu0QKTwcN8tar9hoYH89fE/AztAiOo8u/Prr+h96DhMqcp1TEzlelR8=";

        var configuration = await GetConfigurationAsync(cashBoxId, accessToken);
        var queue = configuration.ftQueues?.First() ?? throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");

        var bootstrapper = new QueuePTBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration ?? new Dictionary<string, object>());
        var signMethod = bootstrapper.RegisterForSign();

        //var initialOperationRequest = InitialOperation(cashBoxId);
        //var initOperationResponse = await signMethod(System.Text.Json.JsonSerializer.Serialize(initialOperationRequest));

        var receiptRequest = ExampleCashSales(cashBoxId);
        var exampleCashSalesResponse = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequest));
        var issueRequest = new
        {
            ReceiptRequest = receiptRequest,
            ReceiptResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)
        };
        var dd = System.Text.Json.JsonSerializer.Serialize(issueRequest);
    }

    private static ReceiptRequest InitialOperation(Guid cashBoxId)
    {
        return new ReceiptRequest
        {
            ftCashBoxID = cashBoxId,
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_4001,
            cbTerminalID = "1",
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = [],
            cbPayItems = []
        };
    }

    private static ReceiptRequest ExampleCashSales(Guid cashBoxId)
    {
        return new ReceiptRequest
        {
            ftCashBoxID = cashBoxId,
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_0000,
            cbTerminalID = "1",
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems =
                        [
                            new ChargeItem
                {
                    Position = 1,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                    VATAmount = 1.2m,
                    Amount = 6.2m,
                    VATRate = 24m,
                    Quantity = 1,
                    Description = "ChargeItem1"
                },
                new ChargeItem
                {
                    Position = 2,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                    VATAmount = 1.2m,
                    Amount = 6.2m,
                    VATRate = 24m,
                    Quantity = 1,
                    Description = "ChargeItem2"
                }
                        ],
            cbPayItems =
                        [
                            new PayItem
                {
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                    Amount = 12.4m,
                    Description = "Cash"
                }
                        ]
        };
    }
}
