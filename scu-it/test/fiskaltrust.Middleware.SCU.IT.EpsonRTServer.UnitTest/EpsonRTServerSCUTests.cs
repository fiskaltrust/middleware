using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.EpsonRTServer;
using fiskaltrust.Middleware.SCU.IT.EpsonRTServer.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer.UnitTest
{
    public class EpsonRTServerSCUTests
    {
        private const string Token = "99SEA004010FISK00011234520260702" + "0743" + "0001" + "000000000";

        private static RtServerResponse Ok(params (string key, string value)[] addInfo) => new()
        {
            Success = true,
            Code = "0",
            Status = "OK",
            AddInfo = addInfo.ToDictionary(x => x.key, x => x.value, StringComparer.OrdinalIgnoreCase)
        };

        private static RtServerResponse Error(int code, string status) => new() { Success = false, Code = code.ToString(), Status = status };

        private static Mock<IEpsonRTServerClient> CreateClientMock()
        {
            var client = new Mock<IEpsonRTServerClient>();
            client.Setup(x => x.CreateTokenAsync(It.IsAny<string>())).ReturnsAsync(Ok(("token", Token)));
            client.Setup(x => x.GetServerInfoAsync()).ReturnsAsync(Ok(("rtSerialNumber", "99SEA004010")));
            client.Setup(x => x.GetServerTimeAsync()).ReturnsAsync(Ok(("srtUtcOffset", "2")));
            client.Setup(x => x.GetFiscalInformationAsync(It.IsAny<string>())).ReturnsAsync(Ok(("zRepNumber", "743"), ("recNumber", "1"), ("dailyAmount", "0.00")));
            return client;
        }

        private static EpsonRTServerSCU CreateScu(Mock<IEpsonRTServerClient> client, out EpsonRTServerConfiguration configuration)
        {
            configuration = new EpsonRTServerConfiguration
            {
                ServerUrl = "https://localhost",
                SendReceiptsSync = true,
                ServiceFolder = Path.Combine(Path.GetTempPath(), "epsonrtserver-tests", Guid.NewGuid().ToString())
            };
            Directory.CreateDirectory(configuration.ServiceFolder!);
            var queue = new EpsonRTServerCommunicationQueue(Guid.NewGuid(), client.Object, NullLogger<EpsonRTServerCommunicationQueue>.Instance, configuration);
            return new EpsonRTServerSCU(Guid.NewGuid(), NullLogger<EpsonRTServerSCU>.Instance, configuration, client.Object, queue);
        }

        private static ProcessRequest SaleProcessRequest(Guid? queueId = null) => new()
        {
            ReceiptRequest = new ReceiptRequest
            {
                ftReceiptCase = 0x0001,
                cbReceiptMoment = new DateTime(2026, 7, 2, 12, 0, 0),
                cbChargeItems = new[] { new ChargeItem { Amount = 1.00m, Quantity = 1, Description = "TEST", VATRate = 22m, ftChargeItemCase = 0x3 } },
                cbPayItems = new[] { new PayItem { Amount = 1.00m, Quantity = 1, Description = "CONTANTE", ftPayItemCase = 0x0 } }
            },
            ReceiptResponse = new ReceiptResponse
            {
                ftQueueID = (queueId ?? Guid.NewGuid()).ToString(),
                ftCashBoxIdentification = "FISK0001",
                ftSignatures = Array.Empty<SignaturItem>()
            }
        };

        // IT country prefix 0x4954 + InitialOperationReceipt0x4001 (only the low 16 bits are actually checked
        // by IsInitialOperationReceipt(), but this mirrors the real ftReceiptCase seen on the wire).
        private static ProcessRequest InitOperationProcessRequest(Guid? queueId = null) => new()
        {
            ReceiptRequest = new ReceiptRequest
            {
                ftReceiptCase = 5283883447184539649, // 0x4954_2000_0000_4001
                cbReceiptMoment = new DateTime(2026, 7, 2, 12, 0, 0),
                cbChargeItems = Array.Empty<ChargeItem>(),
                cbPayItems = Array.Empty<PayItem>()
            },
            ReceiptResponse = new ReceiptResponse
            {
                ftQueueID = (queueId ?? Guid.NewGuid()).ToString(),
                ftCashBoxIdentification = "FISK0001",
                ftSignatures = Array.Empty<SignaturItem>()
            }
        };

        /// <summary>Builds a fake createReport/tillMap raw response containing the given till ids (nested tillCountN elements, matching ParseTillIds).</summary>
        private static RtServerResponse TillMapResponse(params string[] tillIds)
        {
            var addInfo = string.Concat(tillIds.Select((id, i) => $"<tillCount{i + 1} tillId=\"{id}\"/>"));
            return new RtServerResponse
            {
                Success = true,
                Code = "0",
                Status = "OK",
                RawResponse = $"<response success=\"true\" code=\"0\" status=\"OK\"><addInfo>{addInfo}</addInfo></response>"
            };
        }

        [Fact]
        public async Task ProcessReceiptAsync_Should_Record_Local_Time_From_Utc_Moment_Summer()
        {
            var sentDocuments = new List<string>();
            var client = CreateClientMock(); // GetServerTimeAsync -> srtUtcOffset "2" (summer)
            client.Setup(x => x.CreateReceiptAsync(It.IsAny<string>()))
                .Callback<string>(sentDocuments.Add)
                .ReturnsAsync(Ok(("fingerPrint", "ignored")));
            var scu = CreateScu(client, out _);

            var request = SaleProcessRequest();
            request.ReceiptRequest.cbReceiptMoment = new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

            var result = await scu.ProcessReceiptAsync(request);

            using (new AssertionScope())
            {
                // 12:00 UTC + srtUtcOffset 2 => 14:00 local, sent to the device without a timezone designator.
                sentDocuments.Should().ContainSingle().Which.Should().Contain("dateTime=\"20260702T140000\"");
                result.ReceiptResponse.ftSignatures.Should().Contain(
                    x => x.Caption == "<rt-doc-moment>" && x.Data == "2026-07-02 14:00:00");
            }
        }

        [Fact]
        public async Task ProcessReceiptAsync_Should_Record_Local_Time_From_Utc_Moment_Winter()
        {
            var sentDocuments = new List<string>();
            var client = CreateClientMock();
            client.Setup(x => x.GetServerTimeAsync()).ReturnsAsync(Ok(("srtUtcOffset", "1"))); // winter
            client.Setup(x => x.CreateReceiptAsync(It.IsAny<string>()))
                .Callback<string>(sentDocuments.Add)
                .ReturnsAsync(Ok(("fingerPrint", "ignored")));
            var scu = CreateScu(client, out _);

            var request = SaleProcessRequest();
            request.ReceiptRequest.cbReceiptMoment = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

            var result = await scu.ProcessReceiptAsync(request);

            using (new AssertionScope())
            {
                // 12:00 UTC + srtUtcOffset 1 => 13:00 local.
                sentDocuments.Should().ContainSingle().Which.Should().Contain("dateTime=\"20260115T130000\"");
                result.ReceiptResponse.ftSignatures.Should().Contain(
                    x => x.Caption == "<rt-doc-moment>" && x.Data == "2026-01-15 13:00:00");
            }
        }

        [Fact]
        public async Task ProcessReceiptAsync_Sale_Should_Advance_Chain_On_Acceptance()
        {
            var sentDocuments = new List<string>();
            var client = CreateClientMock();
            client.Setup(x => x.CreateReceiptAsync(It.IsAny<string>()))
                .Callback<string>(sentDocuments.Add)
                .ReturnsAsync(Ok(("fingerPrint", "ignored")));
            var scu = CreateScu(client, out _);

            var queueId = Guid.NewGuid();
            var first = await scu.ProcessReceiptAsync(SaleProcessRequest(queueId));
            var second = await scu.ProcessReceiptAsync(SaleProcessRequest(queueId));

            using (new AssertionScope())
            {
                first.ReceiptResponse.ftSignatures.Should().Contain(x => x.Caption == "<rt-doc-number>" && x.Data == "0001");
                second.ReceiptResponse.ftSignatures.Should().Contain(x => x.Caption == "<rt-doc-number>" && x.Data == "0002");
                sentDocuments.Should().HaveCount(2);

                // The first document is chained to the token, the second to the first document's CCDC.
                sentDocuments[0].Should().Contain($"<hash fingerPrint=\"{Token}\"/>");
                var firstCcdc = first.ReceiptResponse.ftSignatures.Single(x => x.Caption == "<rt-server-shametadata>").Data;
                sentDocuments[1].Should().Contain($"<hash fingerPrint=\"{firstCcdc}\"/>");
            }
        }

        [Fact]
        public async Task ProcessReceiptAsync_Should_Not_Advance_Chain_When_Server_Rejects()
        {
            var sentDocuments = new List<string>();
            var client = CreateClientMock();
            client.Setup(x => x.CreateReceiptAsync(It.IsAny<string>()))
                .Callback<string>(sentDocuments.Add)
                .ReturnsAsync(Error(-25, "receipt number error"));
            var scu = CreateScu(client, out _);

            var queueId = Guid.NewGuid();
            var first = await scu.ProcessReceiptAsync(SaleProcessRequest(queueId));
            var second = await scu.ProcessReceiptAsync(SaleProcessRequest(queueId));

            using (new AssertionScope())
            {
                ((ulong) first.ReceiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
                ((ulong) second.ReceiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);

                // The chain must not advance on rejection: both attempts are seeded by the (re-requested) token
                // and use the same document number.
                sentDocuments.Should().OnlyContain(x => x.Contains($"<hash fingerPrint=\"{Token}\"/>"));
                sentDocuments.Should().OnlyContain(x => x.Contains("recNumber=\"0001\""));
            }
        }

        [Fact]
        public async Task ProcessReceiptAsync_Should_Request_New_Token_And_Retry_On_ChainError()
        {
            var responses = new Queue<RtServerResponse>(new[] { Error(-22, "hash error"), Ok(("fingerPrint", "ignored")) });
            var client = CreateClientMock();
            client.Setup(x => x.CreateReceiptAsync(It.IsAny<string>())).ReturnsAsync(responses.Dequeue);
            var scu = CreateScu(client, out _);

            var result = await scu.ProcessReceiptAsync(SaleProcessRequest());

            using (new AssertionScope())
            {
                ((ulong) result.ReceiptResponse.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
                result.ReceiptResponse.ftSignatures.Should().Contain(x => x.Caption == "<rt-doc-number>");
                client.Verify(x => x.CreateReceiptAsync(It.IsAny<string>()), Times.Exactly(2));
                // Initial seeding + chain-error recovery.
                client.Verify(x => x.CreateTokenAsync("FISK0001"), Times.Exactly(2));
            }
        }

        [Fact]
        public async Task ProcessReceiptAsync_Should_Advance_And_Warn_When_Accepted_With_Warning()
        {
            var client = CreateClientMock();
            client.Setup(x => x.CreateReceiptAsync(It.IsAny<string>())).ReturnsAsync(Error(-52, "till offline"));
            var scu = CreateScu(client, out _);

            var queueId = Guid.NewGuid();
            var first = await scu.ProcessReceiptAsync(SaleProcessRequest(queueId));
            var second = await scu.ProcessReceiptAsync(SaleProcessRequest(queueId));

            using (new AssertionScope())
            {
                // -52 ("till offline") is "Receipt accepted with error in log file": not a failure, the chain
                // advances and a warning (kept off the fiscal PDF) is surfaced.
                ((ulong) first.ReceiptResponse.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
                first.ReceiptResponse.ftSignatures.Should().Contain(x => x.Caption == "rt-server-receipt-warning" && x.Data.Contains("-52"));
                first.ReceiptResponse.ftSignatures.Should().Contain(x => x.Caption == "<rt-doc-number>" && x.Data == "0001");
                second.ReceiptResponse.ftSignatures.Should().Contain(x => x.Caption == "<rt-doc-number>" && x.Data == "0002");
                // Not a state-out-of-sync recovery: no extra token is requested.
                client.Verify(x => x.CreateTokenAsync("FISK0001"), Times.Once);
            }
        }

        [Fact]
        public async Task ProcessReceiptAsync_Should_Fail_And_Not_Advance_On_Blocking_Rejection()
        {
            var sentDocuments = new List<string>();
            var client = CreateClientMock();
            client.Setup(x => x.CreateReceiptAsync(It.IsAny<string>()))
                .Callback<string>(sentDocuments.Add)
                .ReturnsAsync(Error(-32, "refund or void not possible"));
            var scu = CreateScu(client, out _);

            var queueId = Guid.NewGuid();
            var first = await scu.ProcessReceiptAsync(SaleProcessRequest(queueId));
            var second = await scu.ProcessReceiptAsync(SaleProcessRequest(queueId));

            using (new AssertionScope())
            {
                // -32 is "Receipt not accepted": blocking, the chain must not advance.
                ((ulong) first.ReceiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
                ((ulong) second.ReceiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
                sentDocuments.Should().OnlyContain(x => x.Contains("recNumber=\"0001\""));
            }
        }

        [Fact]
        public async Task ProcessReceiptAsync_Should_Fail_For_Invalid_TillId()
        {
            var client = CreateClientMock();
            var scu = CreateScu(client, out _);
            var request = SaleProcessRequest();
            request.ReceiptResponse.ftCashBoxIdentification = "TOOLONGTILLID";

            var result = await scu.ProcessReceiptAsync(request);

            ((ulong) result.ReceiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.Data.Contains("8 characters"));
        }

        [Fact]
        public async Task ProcessReceiptAsync_Should_Not_Crash_And_Reseed_Once_When_No_Writable_Folder()
        {
            var client = CreateClientMock();
            client.Setup(x => x.CreateReceiptAsync(It.IsAny<string>())).ReturnsAsync(Ok(("fingerPrint", "ignored")));

            var configuration = new EpsonRTServerConfiguration { ServerUrl = "https://localhost", SendReceiptsSync = true };
            var queue = new EpsonRTServerCommunicationQueue(Guid.NewGuid(), client.Object,
                NullLogger<EpsonRTServerCommunicationQueue>.Instance, configuration);
            var scu = new EpsonRTServerSCU(Guid.NewGuid(), NullLogger<EpsonRTServerSCU>.Instance,
                configuration, client.Object, queue, personalFolderProvider: () => string.Empty);

            var queueId = Guid.NewGuid();
            var first = await scu.ProcessReceiptAsync(SaleProcessRequest(queueId));
            var second = await scu.ProcessReceiptAsync(SaleProcessRequest(queueId));

            using (new AssertionScope())
            {
                ((ulong) first.ReceiptResponse.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
                ((ulong) second.ReceiptResponse.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
                first.ReceiptResponse.ftSignatures.Should().Contain(x => x.Caption == "<rt-doc-number>" && x.Data == "0001");
                second.ReceiptResponse.ftSignatures.Should().Contain(x => x.Caption == "<rt-doc-number>" && x.Data == "0002");
                // In-memory state survived across receipts on one instance -> token requested only once.
                client.Verify(x => x.CreateTokenAsync("FISK0001"), Times.Once);
            }
        }

        [Fact]
        public async Task ProcessReceiptAsync_Should_Not_Crash_When_Service_Folder_Is_Unwritable()
        {
            var client = CreateClientMock();
            client.Setup(x => x.CreateReceiptAsync(It.IsAny<string>())).ReturnsAsync(Ok(("fingerPrint", "ignored")));

            // A FILE where a directory is expected makes Directory.CreateDirectory throw (cross-platform).
            var filePath = Path.Combine(Path.GetTempPath(), $"epsonrtserver-unwritable-{Guid.NewGuid()}");
            File.WriteAllText(filePath, "x");
            var cacheDir = Path.Combine(Path.GetTempPath(), $"epsonrtserver-cache-{Guid.NewGuid()}");
            try
            {
                // ServiceFolder = the file (breaks the SCU state cache path); CacheDirectory = a real dir so the queue is fine.
                var configuration = new EpsonRTServerConfiguration
                {
                    ServerUrl = "https://localhost", SendReceiptsSync = true, ServiceFolder = filePath, CacheDirectory = cacheDir
                };
                var queue = new EpsonRTServerCommunicationQueue(Guid.NewGuid(), client.Object,
                    NullLogger<EpsonRTServerCommunicationQueue>.Instance, configuration);
                var scu = new EpsonRTServerSCU(Guid.NewGuid(), NullLogger<EpsonRTServerSCU>.Instance,
                    configuration, client.Object, queue);

                var result = await scu.ProcessReceiptAsync(SaleProcessRequest());

                ((ulong) result.ReceiptResponse.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
            }
            finally
            {
                File.Delete(filePath);
                if (Directory.Exists(cacheDir)) Directory.Delete(cacheDir, true);
            }
        }

        [Fact]
        public async Task ProcessReceiptAsync_InitOperation_Should_Program_Till_When_Missing_From_Map()
        {
            var client = CreateClientMock();
            client.Setup(x => x.GetTillMapAsync()).ReturnsAsync(TillMapResponse("FISK0002"));
            client.Setup(x => x.CreateTillsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync(Ok());
            client.Setup(x => x.RebootWebServerAsync()).ReturnsAsync(Ok());
            var scu = CreateScu(client, out _);

            var result = await scu.ProcessReceiptAsync(InitOperationProcessRequest());

            using (new AssertionScope())
            {
                ((ulong) result.ReceiptResponse.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
                client.Verify(x => x.CreateTillsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()), Times.Once);
                client.Verify(x => x.RebootWebServerAsync(), Times.Once);
            }
        }

        [Fact]
        public async Task ProcessReceiptAsync_InitOperation_Should_Skip_Programming_When_Till_Already_In_Map()
        {
            var client = CreateClientMock();
            client.Setup(x => x.GetTillMapAsync()).ReturnsAsync(TillMapResponse("FISK0001", "FISK0002"));
            var scu = CreateScu(client, out _);

            var result = await scu.ProcessReceiptAsync(InitOperationProcessRequest());

            using (new AssertionScope())
            {
                ((ulong) result.ReceiptResponse.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
                client.Verify(x => x.CreateTillsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()), Times.Never);
                client.Verify(x => x.RebootWebServerAsync(), Times.Never);
            }
        }
    }
}
