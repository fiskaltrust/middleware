using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.Interface;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.SAFT.CLI;
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

                    var configuration = JsonSerializer.Deserialize<ftCashBoxConfiguration>(content) ?? throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");
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
        public async Task Journal()
        {
            var cashBoxId = Guid.Parse("e117e4b5-88ea-4511-a134-e5408f3cfd4c");
            var accessToken = "BBNu3xCxDz9VKOTQJQATmCzj1zQRjeE25DW/F8hcqsk/Uc5hHc4m1lEgd2QDsWLpa6MRDHz+vLlQs0hCprWt9XY=";
            var configuration = await GetConfigurationAsync(cashBoxId, accessToken);
            var queue = configuration.ftQueues?.First() ?? throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");
            var bootstrapper = new QueueGRBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration ?? new Dictionary<string, object>());
            var signMethod = bootstrapper.RegisterForJournal();

            var receiptRequest = Example_RetailSales(cashBoxId);
            var result = await signMethod(System.Text.Json.JsonSerializer.Serialize(new ifPOS.v1.JournalRequest
            {
                ftJournalType = 0x4752_2000_0000_0001,
            }));
        }

        [Fact]
        public async Task Example_RetailSales_Tests()
        {
            var cashBoxId = Guid.Parse("e117e4b5-88ea-4511-a134-e5408f3cfd4c");
            var accessToken = "BBNu3xCxDz9VKOTQJQATmCzj1zQRjeE25DW/F8hcqsk/Uc5hHc4m1lEgd2QDsWLpa6MRDHz+vLlQs0hCprWt9XY=";
            var configuration = await GetConfigurationAsync(cashBoxId, accessToken);
            var queue = configuration.ftQueues?.First() ?? throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");
            var bootstrapper = new QueueGRBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration ?? new Dictionary<string, object>());
            var signMethod = bootstrapper.RegisterForSign();

            var receiptRequest = Example_RetailSales(cashBoxId);
            var exampleCashSalesResponse = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequest));
            var result = await SendIssueAsync(receiptRequest, System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!);
        }

        [Fact]
        public async Task Example_SalesInvoice_1_1_Tests()
        {
            var cashBoxId = Guid.Parse("e117e4b5-88ea-4511-a134-e5408f3cfd4c");
            var accessToken = "BBNu3xCxDz9VKOTQJQATmCzj1zQRjeE25DW/F8hcqsk/Uc5hHc4m1lEgd2QDsWLpa6MRDHz+vLlQs0hCprWt9XY=";
            var configuration = await GetConfigurationAsync(cashBoxId, accessToken);
            var queue = configuration.ftQueues?.First() ?? throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");
            var bootstrapper = new QueueGRBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration ?? new Dictionary<string, object>());
            var signMethod = bootstrapper.RegisterForSign();

            var receiptRequest = Example_SalesInvoice_1_1(cashBoxId);
            var exampleCashSalesResponse = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequest));
            var response = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!;
            var result = await SendIssueAsync(receiptRequest, response);
        }

        private async Task<string> SendIssueAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
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
            return await response.Content.ReadAsStringAsync();
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

        private static ReceiptRequest Example_SalesInvoice_1_1(Guid cashBoxId)
        {
            var chargeItems = new List<ChargeItem> {
                    AADEFactoryTests.CreateGoodNormalVATRateItem(description: "Product 1", amount: 89.20m, quantity: 1),
                    AADEFactoryTests.CreateGoodNormalVATRateItem(description: "Product 2", amount: 23.43m, quantity: 1),
                    AADEFactoryTests.CreateServiceNormalVATRateItem_WithWithHoldingTax(description: "Service Provision 1", netAmount: 461.93m, quantity: 1),
                    AADEFactoryTests.CreateGoodDiscountedVATRateItem(description: "Merchandise Product 1", amount: 12.30m, quantity: 1),
                    AADEFactoryTests.CreateGoodDiscountedVATRateItem(description: "Merchandise Product 2", amount: 113.43m, quantity: 1),
                };

            var i = 1;
            foreach (var chargeItem in chargeItems)
            {
                chargeItem.Position = i++;
                // Set fraction
                chargeItem.Amount = decimal.Round(chargeItem.Amount, 2, MidpointRounding.AwayFromZero);
                chargeItem.VATAmount = decimal.Round(chargeItem.VATAmount ?? 0.0m, 2, MidpointRounding.AwayFromZero);
            }

            var payItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = -92.39m,
                    Description = "VAT withholding (-20%)",
                    ftPayItemCase = 0x4752_2000_0000_0099
                },
                new PayItem
                {
                    Amount = chargeItems.Sum(x => x.Amount) -  92.39m,
                    Description = "Cash",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            };

            i = 1;
            foreach (var payItem in payItems)
            {
                payItem.Position = i++;
                // Set fraction
                payItem.Amount = decimal.Round(payItem.Amount, 2, MidpointRounding.AwayFromZero);
            }

            var receiptRequest = new ReceiptRequest
            {
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptAmount = chargeItems.Sum(x => x.Amount) - 92.39m,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbChargeItems = chargeItems,
                cbPayItems = payItems,
                ftCashBoxID = cashBoxId,
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = 0x4752_2000_0000_1001,
                cbCustomer = new MiddlewareCustomer
                {
                    CustomerVATId = "997671770",

                }
            };
            return receiptRequest;
        }

        private static ReceiptRequest Example_RetailSales(Guid cashBoxId)
        {
            var chargeItems = new List<ChargeItem>
            {
                AADEFactoryTests.CreateGoodNormalVATRateItem(description: "Merchandise Product 1", amount: 1.3m, quantity: 1),
                AADEFactoryTests.CreateGoodNormalVATRateItem(description: "Merchandise Product 2", amount: 1.0m, quantity: 1),
                AADEFactoryTests.CreateGoodNormalVATRateItem(description: "Merchandise Product 3", amount: 1.2m, quantity: 1),
                AADEFactoryTests.CreateGoodDiscountedVATRateItem(description: "Merchandise Product Discounted 1", amount: 0.5m, quantity: 1),
                AADEFactoryTests.CreateGoodDiscountedVATRateItem(description: "Merchandise Product Discounted 2", amount: 0.6m, quantity: 1)
            };
            var i = 1;
            foreach (var chargeItem in chargeItems)
            {
                chargeItem.Position = i++;
                // Set fraction
                chargeItem.Amount = decimal.Round(chargeItem.Amount, 2, MidpointRounding.AwayFromZero);
                chargeItem.VATAmount = decimal.Round(chargeItem.VATAmount ?? 0.0m, 2, MidpointRounding.AwayFromZero);
            }
            var payItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = chargeItems.Sum(x => x.Amount),
                    Description = "Card",
                    ftPayItemCase = 0x4752_2000_0000_0000 | (long) PayItemCases.DebitCardPayment,
                    ftPayItemCaseData = new PayItemCaseData
                    {
                        Provider = new PayItemCaseProviderVivaWallet
                        {
                            Action = "Sale",
                            Protocol = "VivaWallet",
                            ProtocolVersion = "1.0",
                            ProtocolRequest = new VivaWalletPayment
                            {
                                amount = (int) chargeItems.Sum(x => x.Amount) * 100,
                                cashRegisterId = "",
                                currencyCode = "EUR",
                                merchantReference = Guid.NewGuid().ToString(),
                                sessionId = "John015",
                                terminalId = "123456",
                                aadeProviderSignatureData = "4680AFE5D58088BF8C55F57A5B5DBB15936B51DE;;20241015153111;4600;9;1;10;16007793",
                                aadeProviderSignature = "MEUCIQCnUrakY9pemgdXIsYvbOahoBBadDa9DPaRS9ZtTTra8gIgIUp9LPaH/E+LRwTGJWeL+MZl5j5PtFcM+chiXTqeed4="
                            },
                            ProtocolResponse = new VivaPaymentSession
                            {
                                aadeTransactionId = "116430909552789552789"
                            }
                        }
                    }
                }
            };
            return new ReceiptRequest
            {
                Currency = Currency.EUR,
                cbReceiptAmount = chargeItems.Sum(x => x.Amount),
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbChargeItems = chargeItems,
                cbPayItems = payItems,
                ftCashBoxID = cashBoxId,
                ftPosSystemId = Guid.NewGuid(),
                cbTerminalID = "1",
                ftReceiptCase = 0x4752_2000_0000_0001 // posreceipt
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
                        VATAmount = 1.2m,
                        Amount = 6.2m,
                        VATRate = 24m,
                        Quantity = 1,
                        Description = "ChargeItem1"
                    },
                    new ChargeItem
                    {
                        Position = 2,
                        ftChargeItemCase = 0x4752_2000_0000_0013,
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
                        ftPayItemCase = 0x4752_2000_0000_0001,
                        Amount = 12.4m,
                        Description = "Cash"
                    }
                            ]
            };
        }
    }
}
