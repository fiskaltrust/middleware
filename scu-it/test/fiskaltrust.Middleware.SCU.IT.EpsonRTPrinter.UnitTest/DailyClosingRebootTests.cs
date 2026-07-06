using System.Net.Http;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.UnitTest
{
    public class DailyClosingRebootTests
    {
        // directIO 4034 = restart printer (EpsonCommandFactory.RebootCommand, issue #550).
        private static bool IsRebootCommand(string payload) => payload.Contains("4034");

        private static ReceiptRequest DailyClosing() => new() { ftReceiptCase = 0x4954_2000_0000_2011 };

        private static EpsonRTPrinterSCU CreateSut(IEpsonFpMateClient client, bool forceReboot = false) =>
            new(NullLogger<EpsonRTPrinterSCU>.Instance, new EpsonRTPrinterSCUConfiguration { ForceRebootAfterDailyClosing = forceReboot }, client);

        private static Mock<IEpsonFpMateClient> ClientReturning(ReportResponse zReportResult)
        {
            var xml = SoapSerializer.Serialize(zReportResult);
            var client = new Mock<IEpsonFpMateClient>();
            client.Setup(c => c.SendCommandAsync(It.IsAny<string>()))
                  .ReturnsAsync(() => new HttpResponseMessage { Content = new StringContent(xml) });
            return client;
        }

        [Fact]
        public async Task DailyClosing_WhenEnabledAndZReportSucceeds_SendsRebootCommand()
        {
            var client = ClientReturning(new ReportResponse { Success = true, ReportInfo = new ReportInfo { ZRepNumber = "1", PrinterStatus = "00000000" } });
            var sut = CreateSut(client.Object, forceReboot: true);

            await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = DailyClosing(), ReceiptResponse = new ReceiptResponse() });

            client.Verify(c => c.SendCommandAsync(It.Is<string>(p => IsRebootCommand(p))), Times.Once);
        }

        [Fact]
        public async Task DailyClosing_WhenEnabledAndZReportFails_DoesNotReboot()
        {
            var client = ClientReturning(new ReportResponse { Success = false, Code = "1", Status = "1" });
            var sut = CreateSut(client.Object, forceReboot: true);

            await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = DailyClosing(), ReceiptResponse = new ReceiptResponse() });

            client.Verify(c => c.SendCommandAsync(It.Is<string>(p => IsRebootCommand(p))), Times.Never);
        }

        [Fact]
        public async Task DailyClosing_WhenDisabled_DoesNotReboot()
        {
            var client = ClientReturning(new ReportResponse { Success = true, ReportInfo = new ReportInfo { ZRepNumber = "1", PrinterStatus = "00000000" } });
            var sut = CreateSut(client.Object);

            await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = DailyClosing(), ReceiptResponse = new ReceiptResponse() });

            client.Verify(c => c.SendCommandAsync(It.Is<string>(p => IsRebootCommand(p))), Times.Never);
        }
    }
}
