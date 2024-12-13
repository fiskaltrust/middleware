using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.storage.serialization.V0;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueES.UnitTest
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

                    var configuration = JsonConvert.DeserializeObject<ftCashBoxConfiguration>(content);
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
            var cashBoxId = Guid.Parse("8e2e348b-0a37-45d6-8f22-0aaa82db44ea");
            var accessToken = "BMlOgJSEC1url4Nwd9QSc7rIXGfiEC65Afai4WZjPxbIIUIHykTnp96nryJsnsC98BYaY2jh+lZIbN06JF6LEtg=";

            var configuration = await GetConfigurationAsync(cashBoxId, accessToken);
            var queue = configuration.ftQueues.First();

            queue.Configuration["certificate"] = Convert.ToBase64String(await File.ReadAllBytesAsync("Certificates/Certificado_RPJ_A39200019_CERTIFICADO_ENTIDAD_PRUEBAS_4_Pre.p12"));
            queue.Configuration["certificatePassword"] = "1234";
            var bootstrapper = new QueueESBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration);
            var signMethod = bootstrapper.RegisterForSign();
            var journalMethod = bootstrapper.RegisterForJournal();
            {

                var initialOperationRequest = InitialOperation(cashBoxId);
                var initOperationResponse = await signMethod(System.Text.Json.JsonSerializer.Serialize(initialOperationRequest));
            }

            {
                var receiptRequestWrong = ExampleCashSales(cashBoxId);
                receiptRequestWrong.cbChargeItems.First().VATRate = 20;
                var exampleCashSalesResponseWrongString = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequestWrong));
                var exampleCashSalesResponseWrong = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponseWrongString)!;

                exampleCashSalesResponseWrong.ftState.Should().Match(x => (x & 0xFFFF_FFFF) == 0xEEEE_EEEE, $"ftState 0x{exampleCashSalesResponseWrong.ftState:X} should be == 0xEEEE_EEEE\n{exampleCashSalesResponseWrong.ftSignatures.Find(x => (x.ftSignatureType & 0xFFFF_FFFF) == 0x3000)?.Data ?? exampleCashSalesResponseWrongString}\n");
            }

            var receiptRequest = ExampleCashSales(cashBoxId);
            {
                var exampleCashSalesResponseString = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequest));
                var exampleCashSalesResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponseString)!;
                var errorItem = exampleCashSalesResponse.ftSignatures.Find(x => (x.ftSignatureType & 0xFFFF_FFFF) == 0x3000);
                exampleCashSalesResponse.ftState.Should().Match(x => (x & 0xFFFF_FFFF) < 0xEEEE_EEEE, $"ftState 0x{exampleCashSalesResponse.ftState:X} should be < 0xEEEE_EEEE\n{errorItem?.Data ?? exampleCashSalesResponseString}\n");
            }

            {
                var receiptRequestVoid = ExampleCashSales(cashBoxId);
                receiptRequestVoid.ftReceiptCase = receiptRequestVoid.ftReceiptCase | 0x0004_0000;
                receiptRequestVoid.cbPreviousReceiptReference = receiptRequest.cbReceiptReference;
                var exampleCashSalesVoidResponseString = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequestVoid));
                var exampleCashSalesVoidResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesVoidResponseString)!;
                var errorItemVoid = exampleCashSalesVoidResponse.ftSignatures.Find(x => (x.ftSignatureType & 0xFFFF_FFFF) == 0x3000);
                exampleCashSalesVoidResponse.ftState.Should().Match(x => (x & 0xFFFF_FFFF) < 0xEEEE_EEEE, $"ftState 0x{exampleCashSalesVoidResponse.ftState:X} should be < 0xEEEE_EEEE\n{errorItemVoid?.Data ?? exampleCashSalesVoidResponseString}\n");
            }

            receiptRequest = ExampleCashSales(cashBoxId);
            {
                var exampleCashSalesResponseString = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequest));
                var exampleCashSalesResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponseString)!;
                var errorItem = exampleCashSalesResponse.ftSignatures.Find(x => (x.ftSignatureType & 0xFFFF_FFFF) == 0x3000);
                exampleCashSalesResponse.ftState.Should().Match(x => (x & 0xFFFF_FFFF) < 0xEEEE_EEEE, $"ftState 0x{exampleCashSalesResponse.ftState:X} should be < 0xEEEE_EEEE\n{errorItem?.Data ?? exampleCashSalesResponseString}\n");
            }

            {
                var receiptRequestVoid = ExampleCashSales(cashBoxId);
                receiptRequestVoid.ftReceiptCase = receiptRequestVoid.ftReceiptCase | 0x0004_0000;
                receiptRequestVoid.cbPreviousReceiptReference = receiptRequest.cbReceiptReference;
                var exampleCashSalesVoidResponseString = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequestVoid));
                var exampleCashSalesVoidResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesVoidResponseString)!;
                var errorItemVoid = exampleCashSalesVoidResponse.ftSignatures.Find(x => (x.ftSignatureType & 0xFFFF_FFFF) == 0x3000);
                exampleCashSalesVoidResponse.ftState.Should().Match(x => (x & 0xFFFF_FFFF) < 0xEEEE_EEEE, $"ftState 0x{exampleCashSalesVoidResponse.ftState:X} should be < 0xEEEE_EEEE\n{errorItemVoid?.Data ?? exampleCashSalesVoidResponseString}\n");
            }

            var veriFactuString = await journalMethod(System.Text.Json.JsonSerializer.Serialize(new ifPOS.v1.JournalRequest
            {
                ftJournalType = 0x4752_2000_0000_0000
            }));
        }

        private static ReceiptRequest InitialOperation(Guid cashBoxId)
        {
            return new ReceiptRequest
            {
                ftCashBoxID = cashBoxId,
                ftReceiptCase = 0x4752_2000_0000_4001,
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
                ftReceiptCase = 0x4752_2000_0000_0000,
                cbTerminalID = "1",
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbReceiptMoment = DateTime.UtcNow,
                cbChargeItems =
                            [
                                new ChargeItem
                    {
                        Position = 1,
                        ftChargeItemCase = 0x4752_2000_0000_0013,
                        VATAmount = 1.30m,
                        Amount = 6.2m,
                        VATRate = 21m,
                        Quantity = 1,
                        Description = "ChargeItem1"
                    },
                    new ChargeItem
                    {
                        Position = 2,
                        ftChargeItemCase = 0x4752_2000_0000_0013,
                        VATAmount = 1.30m,
                        Amount = 6.2m,
                        VATRate = 21m,
                        Quantity = 1,
                        Description = "ChargeItem2"
                    }
                            ],
                cbPayItems =
                            [
                                new PayItem
                    {
                        ftPayItemCase = 0x4752_2000_0000_0001,
                        Amount = 12.4m,
                        Description = "Cash"
                    }
                            ]
            };
        }
    }
}
