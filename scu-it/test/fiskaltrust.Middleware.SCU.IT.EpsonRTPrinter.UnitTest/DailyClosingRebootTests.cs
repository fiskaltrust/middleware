using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.UnitTest
{
    public class DailyClosingRebootTests
    {
        private static readonly string RebootData = "019800" + new string(' ', 64);

        private static bool IsRebootCommand(string payload) =>
            payload.Contains("command=\"4034\"") && payload.Contains($"data=\"{RebootData}\"");

        private static ReceiptRequest DailyClosing() => new() { ftReceiptCase = 0x4954_2000_0000_2011 };

        private static EpsonRTPrinterSCU CreateSut(IEpsonFpMateClient client, bool forceReboot = false) =>
            new(NullLogger<EpsonRTPrinterSCU>.Instance, new EpsonRTPrinterSCUConfiguration { ForceRebootAfterDailyClosing = forceReboot }, client);

        private static HttpResponseMessage SuccessfulCommandResponse()
        {
            var xml = SoapSerializer.Serialize(new PrinterCommandResponse { Success = true, SuccessSpecified = true });
            return new HttpResponseMessage { Content = new StringContent(xml) };
        }

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
            client.Setup(c => c.SendCommandAsync(It.Is<string>(payload => IsRebootCommand(payload))))
                .ReturnsAsync(SuccessfulCommandResponse);
            var sut = CreateSut(client.Object, forceReboot: true);

            await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = DailyClosing(), ReceiptResponse = new ReceiptResponse() });

            client.Verify(c => c.SendCommandAsync(It.Is<string>(p => IsRebootCommand(p))), Times.Once);
        }

        [Fact]
        public async Task DailyClosingRebootRejection_DoesNotInvalidateSuccessfulClosing()
        {
            const string rejectionXml = """
<?xml version="1.0" encoding="utf-8"?>
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
  <soapenv:Body>
    <response success="false" code="FP_NO_ANSWER" status="13" />
  </soapenv:Body>
</soapenv:Envelope>
""";
            var reportXml = SoapSerializer.Serialize(new ReportResponse
            {
                Success = true,
                ReportInfo = new ReportInfo { ZRepNumber = "1", PrinterStatus = "00000000" }
            });
            var client = new Mock<IEpsonFpMateClient>();
            client.Setup(c => c.SendCommandAsync(It.Is<string>(payload => !IsRebootCommand(payload))))
                .ReturnsAsync(new HttpResponseMessage { Content = new StringContent(reportXml) });
            client.Setup(c => c.SendCommandAsync(It.Is<string>(payload => IsRebootCommand(payload))))
                .ReturnsAsync(new HttpResponseMessage { Content = new StringContent(rejectionXml) });
            var sut = CreateSut(client.Object, forceReboot: true);

            var result = await sut.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = DailyClosing(),
                ReceiptResponse = new ReceiptResponse()
            });

            Assert.False(result.ReceiptResponse.HasFailed());
            Assert.Null(result.ReceiptResponse.ftStateData);
            var zNumberSignature = Assert.Single(result.ReceiptResponse.ftSignatures);
            Assert.Equal("<rt-z-number>", zNumberSignature.Caption);
            Assert.Equal("0001", zNumberSignature.Data);
            Assert.Equal((long) SignatureTypesIT.RTZNumber, zNumberSignature.ftSignatureType & 0xFF);
            client.Verify(c => c.SendCommandAsync(It.Is<string>(payload => IsRebootCommand(payload))), Times.Once);
        }

        [Fact]
        public async Task DailyClosingRebootNoResponse_DoesNotInvalidateSuccessfulClosing()
        {
            var expectedPayload = EpsonCommandFactory.RebootCommand();
            var reportXml = SoapSerializer.Serialize(new ReportResponse
            {
                Success = true,
                ReportInfo = new ReportInfo { ZRepNumber = "1", PrinterStatus = "00000000" }
            });
            var client = new Mock<IEpsonFpMateClient>();
            client.Setup(c => c.SendCommandAsync(It.Is<string>(payload => !IsRebootCommand(payload))))
                .ReturnsAsync(new HttpResponseMessage { Content = new StringContent(reportXml) });
            client.Setup(c => c.SendCommandAsync(expectedPayload))
                .ThrowsAsync(new TaskCanceledException("dispatched without response"));
            var sut = CreateSut(client.Object, forceReboot: true);

            var result = await sut.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = DailyClosing(),
                ReceiptResponse = new ReceiptResponse()
            });

            Assert.False(result.ReceiptResponse.HasFailed());
            Assert.Null(result.ReceiptResponse.ftStateData);
            var zNumberSignature = Assert.Single(result.ReceiptResponse.ftSignatures);
            Assert.Equal("<rt-z-number>", zNumberSignature.Caption);
            Assert.Equal("0001", zNumberSignature.Data);
            Assert.Equal((long) SignatureTypesIT.RTZNumber, zNumberSignature.ftSignatureType & 0xFF);
            client.Verify(c => c.SendCommandAsync(expectedPayload), Times.Once);
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

        [Fact]
        public async Task DailyClosingRebootThrowsUnexpectedError_ReturnsErroredResponse()
        {
            var reportXml = SoapSerializer.Serialize(new ReportResponse
            {
                Success = true,
                ReportInfo = new ReportInfo { ZRepNumber = "1", PrinterStatus = "00000000" }
            });
            var client = new Mock<IEpsonFpMateClient>();
            client.Setup(c => c.SendCommandAsync(It.Is<string>(payload => !IsRebootCommand(payload))))
                .ReturnsAsync(new HttpResponseMessage { Content = new StringContent(reportXml) });
            client.Setup(c => c.SendCommandAsync(It.Is<string>(payload => IsRebootCommand(payload))))
                .ThrowsAsync(new InvalidOperationException("unexpected reboot failure"));
            var sut = CreateSut(client.Object, forceReboot: true);

            var result = await sut.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = DailyClosing(),
                ReceiptResponse = new ReceiptResponse()
            });

            Assert.True(result.ReceiptResponse.HasFailed());
            Assert.Contains(result.ReceiptResponse.ftSignatures, signature => signature.Data.Contains("unexpected reboot failure"));
            client.Verify(c => c.SendCommandAsync(It.Is<string>(payload => IsRebootCommand(payload))), Times.Once);
        }
    }
}
