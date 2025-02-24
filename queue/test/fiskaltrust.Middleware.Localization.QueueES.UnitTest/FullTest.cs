using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueES.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
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

        [Fact, Trait("only", "local")]
        public async Task FullTestsVeriFactu()
        {
            var cashbox = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync("Certificates/VeriFactu/cashbox.json"))!;
            var cashBoxId = Guid.Parse(cashbox["cashBoxId"]);
            var accessToken = cashbox["accessToken"];

            var configuration = await GetConfigurationAsync(cashBoxId, accessToken);
            var queue = configuration.ftQueues.First();
            var scu = configuration.ftSignaturCreationDevices.First();

            var bootstrapper = new QueueESBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration, new v2.Configuration.PackageConfiguration
            {
                Configuration = scu.Configuration,
                Id = scu.Id,
                Package = scu.Package,
                Url = scu.Url.ToList(),
                Version = scu.Version
            });
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

                exampleCashSalesResponseWrong.ftState.Should().Match((State x) => x.IsState(State.Error), $"ftState 0x{exampleCashSalesResponseWrong.ftState:X} should be == 0xEEEE_EEEE\n{exampleCashSalesResponseWrong.ftSignatures.Find(x => x.ftSignatureType.Type() == (SignatureTypeES) 0x3000)?.Data ?? exampleCashSalesResponseWrongString}\n");
            }

            var receiptRequest = ExampleCashSales(cashBoxId);
            {
                var exampleCashSalesResponseString = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequest));
                var exampleCashSalesResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponseString)!;
                var errorItem = exampleCashSalesResponse.ftSignatures.Find(x => x.ftSignatureType.Type() == (SignatureTypeES) 0x3000);
                exampleCashSalesResponse.ftState.Should().Match((State x) => (long) x.State() < 0xEEEE_EEEE, $"ftState 0x{exampleCashSalesResponse.ftState:X} should be < 0xEEEE_EEEE\n{errorItem?.Data ?? exampleCashSalesResponseString}\n");
            }

            {
                var receiptRequestVoid = ExampleCashSales(cashBoxId);
                receiptRequestVoid.ftReceiptCase = (ReceiptCase) ((long) receiptRequestVoid.ftReceiptCase | 0x0004_0000);
                receiptRequestVoid.cbPreviousReceiptReference = receiptRequest.cbReceiptReference;
                var exampleCashSalesVoidResponseString = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequestVoid));
                var exampleCashSalesVoidResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesVoidResponseString)!;
                var errorItemVoid = exampleCashSalesVoidResponse.ftSignatures.Find(x => x.ftSignatureType.Type() == (SignatureTypeES) 0x3000);
                exampleCashSalesVoidResponse.ftState.Should().Match((State x) => (long) x.State() < 0xEEEE_EEEE, $"ftState 0x{exampleCashSalesVoidResponse.ftState:X} should be < 0xEEEE_EEEE\n{errorItemVoid?.Data ?? exampleCashSalesVoidResponseString}\n");
            }

            receiptRequest = ExampleCashSales(cashBoxId);
            {
                var exampleCashSalesResponseString = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequest));
                var exampleCashSalesResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponseString)!;
                var errorItem = exampleCashSalesResponse.ftSignatures.Find(x => x.ftSignatureType.Type() == (SignatureTypeES) 0x3000);
                exampleCashSalesResponse.ftState.Should().Match((State x) => (long) x.State() < 0xEEEE_EEEE, $"ftState 0x{exampleCashSalesResponse.ftState:X} should be < 0xEEEE_EEEE\n{errorItem?.Data ?? exampleCashSalesResponseString}\n");
            }

            {
                var receiptRequestVoid = ExampleCashSales(cashBoxId);
                receiptRequestVoid.ftReceiptCase = (ReceiptCase) ((long) receiptRequestVoid.ftReceiptCase | 0x0004_0000);
                receiptRequestVoid.cbPreviousReceiptReference = receiptRequest.cbReceiptReference;
                var exampleCashSalesVoidResponseString = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequestVoid));
                var exampleCashSalesVoidResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesVoidResponseString)!;
                var errorItemVoid = exampleCashSalesVoidResponse.ftSignatures.Find(x => x.ftSignatureType.Type() == (SignatureTypeES) 0x3000);
                exampleCashSalesVoidResponse.ftState.Should().Match((State x) => (long) x.State() < 0xEEEE_EEEE, $"ftState 0x{exampleCashSalesVoidResponse.ftState:X} should be < 0xEEEE_EEEE\n{errorItemVoid?.Data ?? exampleCashSalesVoidResponseString}\n");
            }

            var veriFactuString = await journalMethod(System.Text.Json.JsonSerializer.Serialize(new ifPOS.v1.JournalRequest
            {
                ftJournalType = 0x4752_2000_0000_0000
            }));
        }


        // hacks:
        // cashboxidentification maxlen 20
        // cbreceiptreference maxlen 20
        [Fact, Trait("only", "local")]
        public async Task FullTestsTicketBAIAraba()
        {
            var cashbox = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync("Certificates/Araba/cashbox.json"))!;
            var cashBoxId = Guid.Parse(cashbox["cashBoxId"]);
            var accessToken = cashbox["accessToken"];

            var configuration = await GetConfigurationAsync(cashBoxId, accessToken);
            var queue = configuration.ftQueues.First();
            var scu = configuration.ftSignaturCreationDevices.First();

            var bootstrapper = new QueueESBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration, new v2.Configuration.PackageConfiguration
            {
                Configuration = scu.Configuration,
                Id = scu.Id,
                Package = scu.Package,
                Url = scu.Url.ToList(),
                Version = scu.Version
            });
            var signMethod = bootstrapper.RegisterForSign();
            var journalMethod = bootstrapper.RegisterForJournal();
            {

                var initialOperationRequest = InitialOperation(cashBoxId);
                var initOperationResponse = await signMethod(System.Text.Json.JsonSerializer.Serialize(initialOperationRequest));
            }

            var receiptRequest = ExampleCashSalesTicketBAIHacks(cashBoxId);
            {
                var exampleCashSalesResponseString = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequest));
                var exampleCashSalesResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponseString)!;
                var errorItem = exampleCashSalesResponse.ftSignatures.Find(x => x.ftSignatureType.Type() == (SignatureTypeES) 0x3000);
                exampleCashSalesResponse.ftState.Should().Match((State x) => (long) x.State() < 0xEEEE_EEEE, $"ftState 0x{exampleCashSalesResponse.ftState:X} should be < 0xEEEE_EEEE\n{errorItem?.Data ?? exampleCashSalesResponseString}\n");
            }

            // {
            //     var receiptRequestVoid = ExampleCashSales(cashBoxId);
            //     receiptRequestVoid.ftReceiptCase = receiptRequestVoid.ftReceiptCase | 0x0004_0000;
            //     receiptRequestVoid.cbPreviousReceiptReference = receiptRequest.cbReceiptReference;
            //     var exampleCashSalesVoidResponseString = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequestVoid));
            //     var exampleCashSalesVoidResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesVoidResponseString)!;
            //     var errorItemVoid = exampleCashSalesVoidResponse.ftSignatures.Find(x => (x.ftSignatureType & 0xFFFF_FFFF) == 0x3000);
            //     exampleCashSalesVoidResponse.ftState.Should().Match(x => (x & 0xFFFF_FFFF) < 0xEEEE_EEEE, $"ftState 0x{exampleCashSalesVoidResponse.ftState:X} should be < 0xEEEE_EEEE\n{errorItemVoid?.Data ?? exampleCashSalesVoidResponseString}\n");
            // }

            // receiptRequest = ExampleCashSales(cashBoxId);
            // {
            //     var exampleCashSalesResponseString = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequest));
            //     var exampleCashSalesResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponseString)!;
            //     var errorItem = exampleCashSalesResponse.ftSignatures.Find(x => (x.ftSignatureType & 0xFFFF_FFFF) == 0x3000);
            //     exampleCashSalesResponse.ftState.Should().Match(x => (x & 0xFFFF_FFFF) < 0xEEEE_EEEE, $"ftState 0x{exampleCashSalesResponse.ftState:X} should be < 0xEEEE_EEEE\n{errorItem?.Data ?? exampleCashSalesResponseString}\n");
            // }

            // {
            //     var receiptRequestVoid = ExampleCashSales(cashBoxId);
            //     receiptRequestVoid.ftReceiptCase = receiptRequestVoid.ftReceiptCase | 0x0004_0000;
            //     receiptRequestVoid.cbPreviousReceiptReference = receiptRequest.cbReceiptReference;
            //     var exampleCashSalesVoidResponseString = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequestVoid));
            //     var exampleCashSalesVoidResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesVoidResponseString)!;
            //     var errorItemVoid = exampleCashSalesVoidResponse.ftSignatures.Find(x => (x.ftSignatureType & 0xFFFF_FFFF) == 0x3000);
            //     exampleCashSalesVoidResponse.ftState.Should().Match(x => (x & 0xFFFF_FFFF) < 0xEEEE_EEEE, $"ftState 0x{exampleCashSalesVoidResponse.ftState:X} should be < 0xEEEE_EEEE\n{errorItemVoid?.Data ?? exampleCashSalesVoidResponseString}\n");
            // }

            // var veriFactuString = await journalMethod(System.Text.Json.JsonSerializer.Serialize(new ifPOS.v1.JournalRequest
            // {
            //     ftJournalType = 0x4752_2000_0000_0000
            // }));
        }


        // hacks:
        // cashboxidentification maxlen 20
        // cbreceiptreference maxlen 20
        [Fact, Trait("only", "local")]
        public async Task FullTestsTicketBAIGipuzkoa()
        {
            var cashbox = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync("Certificates/Gipuzkoa/cashbox.json"))!;
            var cashBoxId = Guid.Parse(cashbox["cashBoxId"]);
            var accessToken = cashbox["accessToken"];

            var configuration = await GetConfigurationAsync(cashBoxId, accessToken);
            var queue = configuration.ftQueues.First();
            var scu = configuration.ftSignaturCreationDevices.First();

            var bootstrapper = new QueueESBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration, new v2.Configuration.PackageConfiguration
            {
                Configuration = scu.Configuration,
                Id = scu.Id,
                Package = scu.Package,
                Url = scu.Url.ToList(),
                Version = scu.Version
            });
            var signMethod = bootstrapper.RegisterForSign();
            var journalMethod = bootstrapper.RegisterForJournal();
            {

                var initialOperationRequest = InitialOperation(cashBoxId);
                var initOperationResponse = await signMethod(System.Text.Json.JsonSerializer.Serialize(initialOperationRequest));
            }

            var receiptRequest = ExampleCashSalesTicketBAIHacks(cashBoxId);
            {
                var exampleCashSalesResponseString = await signMethod(ExampleCashSalesGipuzkoaHacks(cashBoxId));
                var exampleCashSalesResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponseString)!;
                var errorItem = exampleCashSalesResponse.ftSignatures.Find(x => x.ftSignatureType.Type() == (SignatureTypeES) 0x3000);
                exampleCashSalesResponse.ftState.Should().Match((State x) => (long) x.State() < 0xEEEE_EEEE, $"ftState 0x{exampleCashSalesResponse.ftState:X} should be < 0xEEEE_EEEE\n{errorItem?.Data ?? exampleCashSalesResponseString}\n");
            }

            // {
            //     var receiptRequestVoid = ExampleCashSales(cashBoxId);
            //     receiptRequestVoid.ftReceiptCase = receiptRequestVoid.ftReceiptCase | 0x0004_0000;
            //     receiptRequestVoid.cbPreviousReceiptReference = receiptRequest.cbReceiptReference;
            //     var exampleCashSalesVoidResponseString = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequestVoid));
            //     var exampleCashSalesVoidResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesVoidResponseString)!;
            //     var errorItemVoid = exampleCashSalesVoidResponse.ftSignatures.Find(x => (x.ftSignatureType & 0xFFFF_FFFF) == 0x3000);
            //     exampleCashSalesVoidResponse.ftState.Should().Match(x => (x & 0xFFFF_FFFF) < 0xEEEE_EEEE, $"ftState 0x{exampleCashSalesVoidResponse.ftState:X} should be < 0xEEEE_EEEE\n{errorItemVoid?.Data ?? exampleCashSalesVoidResponseString}\n");
            // }

            // receiptRequest = ExampleCashSales(cashBoxId);
            // {
            //     var exampleCashSalesResponseString = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequest));
            //     var exampleCashSalesResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponseString)!;
            //     var errorItem = exampleCashSalesResponse.ftSignatures.Find(x => (x.ftSignatureType & 0xFFFF_FFFF) == 0x3000);
            //     exampleCashSalesResponse.ftState.Should().Match(x => (x & 0xFFFF_FFFF) < 0xEEEE_EEEE, $"ftState 0x{exampleCashSalesResponse.ftState:X} should be < 0xEEEE_EEEE\n{errorItem?.Data ?? exampleCashSalesResponseString}\n");
            // }

            // {
            //     var receiptRequestVoid = ExampleCashSales(cashBoxId);
            //     receiptRequestVoid.ftReceiptCase = receiptRequestVoid.ftReceiptCase | 0x0004_0000;
            //     receiptRequestVoid.cbPreviousReceiptReference = receiptRequest.cbReceiptReference;
            //     var exampleCashSalesVoidResponseString = await signMethod(System.Text.Json.JsonSerializer.Serialize(receiptRequestVoid));
            //     var exampleCashSalesVoidResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesVoidResponseString)!;
            //     var errorItemVoid = exampleCashSalesVoidResponse.ftSignatures.Find(x => (x.ftSignatureType & 0xFFFF_FFFF) == 0x3000);
            //     exampleCashSalesVoidResponse.ftState.Should().Match(x => (x & 0xFFFF_FFFF) < 0xEEEE_EEEE, $"ftState 0x{exampleCashSalesVoidResponse.ftState:X} should be < 0xEEEE_EEEE\n{errorItemVoid?.Data ?? exampleCashSalesVoidResponseString}\n");
            // }

            // var veriFactuString = await journalMethod(System.Text.Json.JsonSerializer.Serialize(new ifPOS.v1.JournalRequest
            // {
            //     ftJournalType = 0x4752_2000_0000_0000
            // }));
        }

        private static ReceiptRequest InitialOperation(Guid cashBoxId)
        {
            return new ReceiptRequest
            {
                ftCashBoxID = cashBoxId,
                ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_4001,
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
                ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0000,
                cbTerminalID = "1",
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbReceiptMoment = DateTime.UtcNow,
                cbChargeItems =
                            [
                                new ChargeItem
                    {
                        Position = 1,
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
                        VATAmount = 1.30m,
                        Amount = 6.2m,
                        VATRate = 21m,
                        Quantity = 1,
                        Description = "ChargeItem1"
                    },
                    new ChargeItem
                    {
                        Position = 2,
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
                        VATAmount = 1.30m,
                        Amount = 6.2m,
                        VATRate = 21m,
                        Quantity = 1,
                        Description = "ChargeItem2"
                    },
                                        new ChargeItem
                    {
                        Position = 2,
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
                        VATAmount = .83m,
                        Amount = 1m,
                        VATRate = 21m,
                        Quantity = 1,
                        Description = "ChargeItem2"
                    }
                            ],
                cbPayItems =
                            [
                                new PayItem
                    {
                        ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                        Amount = 12.4m,
                        Description = "Cash"
                    }
                            ]
            };
        }

        private static string ExampleCashSalesGipuzkoaHacks(Guid cashBoxId)
        {
            var request = System.Text.Json.JsonSerializer.Deserialize<ReceiptRequest>("""
            {"ftCashBoxID":"52db0330-be54-46af-8d95-29f8390d13d9",
            "cbTerminalID":"",
            "ftPosSystemId":"7b5955e3-4944-4ff3-8df9-46166b70132a",
            "cbReceiptReference":"23-1736259319003",
            "cbArea":"0",
            "cbUser":"Chef",
            "cbCustomer":null,
            "ftReceiptCase":4995371596056100865,
            "cbChargeItems":[{"Amount":0.2,"Description":"Artikel2","ProductNumber":"2","Quantity":1,"VATRate":21,"ftChargeItemCase":4995371596056100867,"Moment":"2025-01-15T12:26:19.781Z","Position":1,"VATAmount":0.03}],
            "cbPayItems":[{"Position":1,"Quantity":1,"Moment":"2025-01-15T12:26:21.997Z","Description":"Cash","Amount":0.2,"ftPayItemCase":4995371596056100865}],
            "cbReceiptAmount":0.2,
            "cbReceiptMoment":"2025-01-15T12:26:24.463Z"}
            """)!;
            request.cbReceiptReference = string.Concat(Guid.NewGuid().ToString().Take(5));
            request.ftCashBoxID = cashBoxId;
            return System.Text.Json.JsonSerializer.Serialize(request);
        }
        private static ReceiptRequest ExampleCashSalesTicketBAIHacks(Guid cashBoxId)
        {
            return new ReceiptRequest
            {
                ftCashBoxID = cashBoxId,
                ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0000,
                cbTerminalID = "1",
                cbReceiptReference = string.Concat(Guid.NewGuid().ToString().Take(5)),
                cbReceiptMoment = DateTime.UtcNow,
                cbChargeItems =
                            [
                                new ChargeItem
                    {
                        Position = 1,
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
                        VATAmount = 1.3m,
                        Amount = 6.2m,
                        VATRate = 21m,
                        Quantity = 1,
                        Description = "ChargeItem1"
                    },
                    new ChargeItem
                    {
                        Position = 2,
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
                        VATAmount = 1.3m,
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
                        ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                        Amount = 12.4m,
                        Description = "Cash"
                    }
                            ]
            };
        }
    }
}
