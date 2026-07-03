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
    }
}
