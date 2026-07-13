using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json.Linq;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.UnitTest
{
    public class RebootCommandTests
    {
        private static readonly string RebootData = "019800" + new string(' ', 64);

        private static bool IsRebootCommand(string payload) =>
            payload.Contains("command=\"4034\"") && payload.Contains($"data=\"{RebootData}\"");

        private static HttpResponseMessage SuccessfulCommandResponse()
        {
            const string responseXml = """
<?xml version="1.0" encoding="utf-8"?>
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
  <soapenv:Body>
    <response success="true" />
  </soapenv:Body>
</soapenv:Envelope>
""";
            return new HttpResponseMessage { Content = new StringContent(responseXml) };
        }

        [Fact]
        public void RebootCommand_IsDirectIO4034_RestartPrinter()
        {
            var xml = EpsonCommandFactory.RebootCommand();

            Assert.Contains("<printerCommand>", xml);
            Assert.Contains("command=\"4034\"", xml);
            // PARAM=01 (Web server) + INDEX=98 (Restart printer) + FUNCTION=00 + 64-byte space padding.
            Assert.Contains("data=\"019800" + new string(' ', 64) + "\"", xml);
        }

        [Theory]
        [InlineData(0x4954_2040_0000_2000, true)]   // ZeroReceipt + reboot flag
        [InlineData(0x4954_2000_0000_2000, false)]  // plain ZeroReceipt
        [InlineData(0x4954_2001_0000_2000, false)]  // XReport flag must not be read as reboot
        public void IsRebootRequest_DetectsOnlyTheRebootFlag(long ftReceiptCase, bool expected)
        {
            var request = new ReceiptRequest { ftReceiptCase = ftReceiptCase };

            Assert.Equal(expected, request.IsRebootRequest());
        }

        [Fact]
        public async Task ProcessReceiptAsync_WhenEpsonAcknowledgesReboot_ReturnsStructuredSuccessWithoutRetry()
        {
            var expectedPayload = EpsonCommandFactory.RebootCommand();
            var client = new Mock<IEpsonFpMateClient>();
            client.Setup(c => c.SendCommandAsync(expectedPayload))
                .ReturnsAsync(SuccessfulCommandResponse);
            var sut = new EpsonRTPrinterSCU(NullLogger<EpsonRTPrinterSCU>.Instance, new EpsonRTPrinterSCUConfiguration(), client.Object);

            var result = await sut.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = new ReceiptRequest { ftReceiptCase = 0x4954_2040_0000_2000 },
                ReceiptResponse = new ReceiptResponse()
            });

            Assert.False(result.ReceiptResponse.HasFailed());
            var expectedStateData = JObject.Parse("{\"Reboot\":{\"Outcome\":\"acknowledged\"}}");
            var actualStateData = JObject.Parse(result.ReceiptResponse.ftStateData);
            Assert.True(JToken.DeepEquals(expectedStateData, actualStateData));
            client.Verify(c => c.SendCommandAsync(expectedPayload), Times.Once);
            client.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ManualRebootNoResponse_ReturnsStructuredSuccessWithoutRetry()
        {
            var expectedPayload = EpsonCommandFactory.RebootCommand();
            var client = new Mock<IEpsonFpMateClient>();
            client.Setup(c => c.SendCommandAsync(expectedPayload))
                .ThrowsAsync(new EpsonNoResponseException("dispatched without response"));
            var sut = new EpsonRTPrinterSCU(NullLogger<EpsonRTPrinterSCU>.Instance, new EpsonRTPrinterSCUConfiguration(), client.Object);

            var result = await sut.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = new ReceiptRequest { ftReceiptCase = 0x4954_2040_0000_2000 },
                ReceiptResponse = new ReceiptResponse()
            });

            Assert.False(result.ReceiptResponse.HasFailed());
            var expectedStateData = JObject.Parse("{\"Reboot\":{\"Outcome\":\"no-response\"}}");
            var actualStateData = JObject.Parse(result.ReceiptResponse.ftStateData);
            Assert.True(JToken.DeepEquals(expectedStateData, actualStateData));
            Assert.Empty(result.ReceiptResponse.ftSignatures);
            client.Verify(c => c.SendCommandAsync(expectedPayload), Times.Once);
            client.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ManualRebootHttpRequestException_ReturnsErroredResponseWithoutRetry()
        {
            var expectedPayload = EpsonCommandFactory.RebootCommand();
            var client = new Mock<IEpsonFpMateClient>();
            client.Setup(c => c.SendCommandAsync(expectedPayload))
                .ThrowsAsync(new HttpRequestException("HTTP reboot failure"));
            var sut = new EpsonRTPrinterSCU(NullLogger<EpsonRTPrinterSCU>.Instance, new EpsonRTPrinterSCUConfiguration(), client.Object);

            var result = await sut.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = new ReceiptRequest { ftReceiptCase = 0x4954_2040_0000_2000 },
                ReceiptResponse = new ReceiptResponse()
            });

            Assert.True(result.ReceiptResponse.HasFailed());
            Assert.Contains(result.ReceiptResponse.ftSignatures, signature => signature.Data.Contains("HTTP reboot failure"));
            client.Verify(c => c.SendCommandAsync(expectedPayload), Times.Once);
            client.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ManualRebootRejection_ReturnsStructuredErrorWithoutRetry()
        {
            const string responseXml = """
<?xml version="1.0" encoding="utf-8"?>
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
  <soapenv:Body>
    <response success="false" code="FP_NO_ANSWER" status="13" />
  </soapenv:Body>
</soapenv:Envelope>
""";
            var expectedPayload = EpsonCommandFactory.RebootCommand();
            var client = new Mock<IEpsonFpMateClient>();
            client.Setup(c => c.SendCommandAsync(expectedPayload))
                .ReturnsAsync(new HttpResponseMessage { Content = new StringContent(responseXml) });
            var sut = new EpsonRTPrinterSCU(NullLogger<EpsonRTPrinterSCU>.Instance, new EpsonRTPrinterSCUConfiguration(), client.Object);

            var result = await sut.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = new ReceiptRequest { ftReceiptCase = 0x4954_2040_0000_2000 },
                ReceiptResponse = new ReceiptResponse()
            });

            Assert.True(result.ReceiptResponse.HasFailed());
            var failure = Assert.Single(result.ReceiptResponse.ftSignatures);
            Assert.Equal("FAILURE", failure.Caption);
            Assert.Contains("FP_NO_ANSWER", failure.Data);
            Assert.Contains("13", failure.Data);
            var expectedStateData = JObject.Parse("{\"Reboot\":{\"Outcome\":\"rejected\",\"Code\":\"FP_NO_ANSWER\",\"Status\":\"13\"}}");
            var actualStateData = JObject.Parse(result.ReceiptResponse.ftStateData);
            Assert.True(JToken.DeepEquals(expectedStateData, actualStateData));
            client.Verify(c => c.SendCommandAsync(expectedPayload), Times.Once);
            client.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ManualRebootRejectionWithoutDetails_ReturnsUsefulFallbackWithoutRetry()
        {
            const string responseXml = """
<?xml version="1.0" encoding="utf-8"?>
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
  <soapenv:Body>
    <response success="false" />
  </soapenv:Body>
</soapenv:Envelope>
""";
            var expectedPayload = EpsonCommandFactory.RebootCommand();
            var client = new Mock<IEpsonFpMateClient>();
            client.Setup(c => c.SendCommandAsync(expectedPayload))
                .ReturnsAsync(new HttpResponseMessage { Content = new StringContent(responseXml) });
            var sut = new EpsonRTPrinterSCU(NullLogger<EpsonRTPrinterSCU>.Instance, new EpsonRTPrinterSCUConfiguration(), client.Object);

            var result = await sut.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = new ReceiptRequest { ftReceiptCase = 0x4954_2040_0000_2000 },
                ReceiptResponse = new ReceiptResponse()
            });

            Assert.True(result.ReceiptResponse.HasFailed());
            var failure = Assert.Single(result.ReceiptResponse.ftSignatures);
            Assert.Equal("FAILURE", failure.Caption);
            Assert.Equal("The Epson printer rejected the reboot command without code or status.", failure.Data);
            var expectedStateData = JObject.Parse("{\"Reboot\":{\"Outcome\":\"rejected\"}}");
            var actualStateData = JObject.Parse(result.ReceiptResponse.ftStateData);
            Assert.True(JToken.DeepEquals(expectedStateData, actualStateData));
            client.Verify(c => c.SendCommandAsync(expectedPayload), Times.Once);
            client.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ManualRebootRejectionWithMalformedStatus_PreservesRawErrorDetails()
        {
            const string responseXml = """
<?xml version="1.0" encoding="utf-8"?>
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
  <soapenv:Body>
    <response success="false" code="FP_NO_ANSWER" status="not-a-number" />
  </soapenv:Body>
</soapenv:Envelope>
""";
            var client = new Mock<IEpsonFpMateClient>();
            client.Setup(c => c.SendCommandAsync(It.Is<string>(payload => IsRebootCommand(payload))))
                .ReturnsAsync(new HttpResponseMessage { Content = new StringContent(responseXml) });
            var sut = new EpsonRTPrinterSCU(NullLogger<EpsonRTPrinterSCU>.Instance, new EpsonRTPrinterSCUConfiguration(), client.Object);

            var result = await sut.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = new ReceiptRequest { ftReceiptCase = 0x4954_2040_0000_2000 },
                ReceiptResponse = new ReceiptResponse()
            });

            Assert.True(result.ReceiptResponse.HasFailed());
            var failure = Assert.Single(result.ReceiptResponse.ftSignatures);
            Assert.Equal("FAILURE", failure.Caption);
            Assert.Contains("FP_NO_ANSWER", failure.Data);
            Assert.Contains("not-a-number", failure.Data);
        }

        [Fact]
        public async Task ManualRebootDispatchThrowsUnexpectedError_ReturnsErroredResponse()
        {
            var client = new Mock<IEpsonFpMateClient>();
            client.Setup(c => c.SendCommandAsync(It.Is<string>(payload => IsRebootCommand(payload))))
                .ThrowsAsync(new InvalidOperationException("unexpected reboot failure"));
            var sut = new EpsonRTPrinterSCU(NullLogger<EpsonRTPrinterSCU>.Instance, new EpsonRTPrinterSCUConfiguration(), client.Object);

            var result = await sut.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = new ReceiptRequest { ftReceiptCase = 0x4954_2040_0000_2000 },
                ReceiptResponse = new ReceiptResponse()
            });

            Assert.True(result.ReceiptResponse.HasFailed());
            Assert.Contains(result.ReceiptResponse.ftSignatures, signature => signature.Data.Contains("unexpected reboot failure"));
            client.Verify(c => c.SendCommandAsync(It.Is<string>(payload => IsRebootCommand(payload))), Times.Once);
        }

        [Fact]
        public async Task ManualRebootResponseIsMalformed_ReturnsErroredResponse()
        {
            var client = new Mock<IEpsonFpMateClient>();
            client.Setup(c => c.SendCommandAsync(It.Is<string>(payload => IsRebootCommand(payload))))
                .ReturnsAsync(new HttpResponseMessage { Content = new StringContent("<malformed") });
            var sut = new EpsonRTPrinterSCU(NullLogger<EpsonRTPrinterSCU>.Instance, new EpsonRTPrinterSCUConfiguration(), client.Object);

            var result = await sut.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = new ReceiptRequest { ftReceiptCase = 0x4954_2040_0000_2000 },
                ReceiptResponse = new ReceiptResponse()
            });

            Assert.True(result.ReceiptResponse.HasFailed());
            client.Verify(c => c.SendCommandAsync(It.Is<string>(payload => IsRebootCommand(payload))), Times.Once);
        }

        [Fact]
        public async Task ManualRebootResponseWithoutSuccess_ReturnsErroredResponse()
        {
            const string responseXml = """
<?xml version="1.0" encoding="utf-8"?>
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
  <soapenv:Body>
    <response />
  </soapenv:Body>
</soapenv:Envelope>
""";
            var client = new Mock<IEpsonFpMateClient>();
            client.Setup(c => c.SendCommandAsync(It.Is<string>(payload => IsRebootCommand(payload))))
                .ReturnsAsync(new HttpResponseMessage { Content = new StringContent(responseXml) });
            var sut = new EpsonRTPrinterSCU(NullLogger<EpsonRTPrinterSCU>.Instance, new EpsonRTPrinterSCUConfiguration(), client.Object);

            var result = await sut.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = new ReceiptRequest { ftReceiptCase = 0x4954_2040_0000_2000 },
                ReceiptResponse = new ReceiptResponse()
            });

            Assert.True(result.ReceiptResponse.HasFailed());
            client.Verify(c => c.SendCommandAsync(It.Is<string>(payload => IsRebootCommand(payload))), Times.Once);
        }
    }
}
