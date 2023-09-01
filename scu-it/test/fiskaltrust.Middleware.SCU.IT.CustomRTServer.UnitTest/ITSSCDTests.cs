using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer.UnitTest
{
    public class ITSSCDTests
    {
        private readonly Guid _queueId = Guid.NewGuid();

        [Fact]
        public async Task PerformInitOperationAsync()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();

            var accountMasterData = new AccountMasterData
            {
                AccountId = Guid.NewGuid(),
                VatId = "12345688909"
            };

            var sut = new ScuBootstrapper
            {
                Id = Guid.NewGuid(),
                Configuration = new Dictionary<string, object>
                {
                    { "ServerUrl", "https://a3e3-88-116-45-202.ngrok-free.app/" },
                    { "Username", "0001ab05" },
                    { "Password", "admin" },
                    { "AccountMasterData", accountMasterData }
                }
            };
            sut.ConfigureServices(serviceCollection);


            var itsscd = serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();

            var processRequest = new ProcessRequest
            {
                ReceiptRequest = new ReceiptRequest
                {
                    ftReceiptCase = 0x4954_2000_0000_4001
                },
                ReceiptResponse = new ReceiptResponse
                {
                    ftCashBoxIdentification = "02020402",
                    ftQueueID = _queueId.ToString()
                }
            };

            _ = await itsscd.ProcessReceiptAsync(processRequest);
        }

        [Fact]
        public async Task PerformOutOfOperationAsync()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();

            var accountMasterData = new AccountMasterData
            {
                AccountId = Guid.NewGuid(),
                VatId = "19239239"
            };

            var sut = new ScuBootstrapper
            {
                Id = Guid.NewGuid(),
                Configuration = new Dictionary<string, object>
                {
                    { "ServerUrl", "https://a3e3-88-116-45-202.ngrok-free.app/" },
                    { "Username", "0001ab05" },
                    { "Password", "admin" },
                    { "AccountMasterData", accountMasterData }
                }
            };
            sut.ConfigureServices(serviceCollection);


            var itsscd = serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();

            var processRequest = new ProcessRequest
            {
                ReceiptRequest = new ReceiptRequest
                {
                    ftReceiptCase = 0x4954_2000_0000_4002
                },
                ReceiptResponse = new ReceiptResponse
                {
                    ftCashBoxIdentification = "02020402",
                    ftQueueID = _queueId.ToString()
                }
            };

            _ = await itsscd.ProcessReceiptAsync(processRequest);
        }

        [Fact]
        public async Task PerformZeroReceiptAsync()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();

            var accountMasterData = new AccountMasterData
            {
                AccountId = Guid.NewGuid(),
                VatId = "19239239"
            };

            var sut = new ScuBootstrapper
            {
                Id = Guid.NewGuid(),
                Configuration = new Dictionary<string, object>
                {
                    { "ServerUrl", "https://a3e3-88-116-45-202.ngrok-free.app/" },
                    { "Username", "0001ab05" },
                    { "Password", "admin" },
                    { "AccountMasterData", accountMasterData }
                }
            };
            sut.ConfigureServices(serviceCollection);


            var itsscd = serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();

            var processRequest = new ProcessRequest
            {
                ReceiptRequest = new ReceiptRequest
                {
                    ftReceiptCase = 0x4954_2000_0000_2000
                },
                ReceiptResponse = new ReceiptResponse
                {
                    ftCashBoxIdentification = "02020402",
                    ftQueueID = _queueId.ToString()
                }
            };

            _ = await itsscd.ProcessReceiptAsync(processRequest);
        }

        [Fact]
        public async Task GetRTInfoAsync_ShouldReturn_SerialNumber()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();

            var sut = new ScuBootstrapper
            {
                Id = Guid.NewGuid(),
                Configuration = new Dictionary<string, object>
                {
                    { "ServerUrl", "https://localhost:8000" }
                }
            };
            sut.ConfigureServices(serviceCollection);


            var itsscd = serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();

            _ = await itsscd.GetRTInfoAsync();
        }

        public static IEnumerable<object[]> rtNoHandleReceipts()
        {
            yield return new object[] { ITReceiptCases.PointOfSaleReceiptWithoutObligation0x0003 };
            yield return new object[] { ITReceiptCases.ECommerce0x0004 };
            yield return new object[] { ITReceiptCases.InvoiceUnknown0x1000 };
            yield return new object[] { ITReceiptCases.InvoiceB2C0x1001 };
            yield return new object[] { ITReceiptCases.InvoiceB2B0x1002 };
            yield return new object[] { ITReceiptCases.InvoiceB2G0x1003 };
            yield return new object[] { ITReceiptCases.OneReceipt0x2001 };
            yield return new object[] { ITReceiptCases.ShiftClosing0x2010 };
            yield return new object[] { ITReceiptCases.MonthlyClosing0x2012 };
            yield return new object[] { ITReceiptCases.YearlyClosing0x2013 };
            yield return new object[] { ITReceiptCases.ProtocolUnspecified0x3000 };
            yield return new object[] { ITReceiptCases.ProtocolTechnicalEvent0x3001 };
            yield return new object[] { ITReceiptCases.ProtocolAccountingEvent0x3002 };
            yield return new object[] { ITReceiptCases.InternalUsageMaterialConsumption0x3003 };
            yield return new object[] { ITReceiptCases.InitSCUSwitch0x4011 };
            yield return new object[] { ITReceiptCases.FinishSCUSwitch0x4012 };
        }

        public static IEnumerable<object[]> rtHandledReceipts()
        {
            yield return new object[] { ITReceiptCases.UnknownReceipt0x0000 };
            yield return new object[] { ITReceiptCases.PointOfSaleReceipt0x0001 };
            yield return new object[] { ITReceiptCases.PaymentTransfer0x0002 };
            yield return new object[] { ITReceiptCases.Protocol0x0005 };
        }

        [Theory]
        [MemberData(nameof(rtNoHandleReceipts))]
        public async Task ProcessAsync_Should_Do_Nothing(ITReceiptCases receiptCase)
        {
            var initOperationReceipt = $$"""
{
    "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
    "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
    "cbTerminalID": "00010001",
    "cbReceiptReference": "{{Guid.NewGuid()}}",
    "cbReceiptMoment": "{{DateTime.UtcNow.ToString("o")}}",
    "cbChargeItems": [],
    "cbPayItems": [],
    "ftReceiptCase": {{0x4954200000000000 | (long) receiptCase}},
    "ftReceiptCaseData": "",
    "cbUser": "Admin"
}
""";
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(initOperationReceipt);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();

            var sut = new ScuBootstrapper
            {
                Id = Guid.NewGuid(),
                Configuration = new Dictionary<string, object>
                {
                    { "ServerUrl", "https://localhost:8000" }
                }
            };
            sut.ConfigureServices(serviceCollection);


            var itsscd = serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();

            _ = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = receiptRequest,
                ReceiptResponse = new ReceiptResponse
                {
                    ftQueueID = Guid.NewGuid().ToString()
                }
            });

        }

        [Theory(Skip = "Currently not working since we don't have a certificate")]
        [MemberData(nameof(rtHandledReceipts))]
        public async Task ProcessAsync_Should_Do_Things(ITReceiptCases receiptCase)
        {
            var initOperationReceipt = $$"""
{
    "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
    "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
    "cbTerminalID": "00010001",
    "cbReceiptReference": "{{Guid.NewGuid()}}",
    "cbReceiptMoment": "{{DateTime.UtcNow.ToString("o")}}",
    "cbChargeItems": [],
    "cbPayItems": [],
    "ftReceiptCase": {{0x4954200000000000 | (long) receiptCase}},
    "ftReceiptCaseData": "",
    "cbUser": "Admin"
}
""";
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(initOperationReceipt);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();

            var sut = new ScuBootstrapper
            {
                Id = Guid.NewGuid(),
                Configuration = new Dictionary<string, object>
                {
                    { "ServerUrl", "https://localhost:8000" }
                }
            };
            sut.ConfigureServices(serviceCollection);


            var itsscd = serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();

            _ = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = receiptRequest,
                ReceiptResponse = new ReceiptResponse
                {
                    ftQueueID = Guid.NewGuid().ToString()
                }
            });

        }
    }
}