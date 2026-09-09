using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.UnitTest
{
    public class ReprintSerialNumberTests
    {
        private const string ReprintCommand = "3098";
        private const string SerialNumberCommand = "3217";
        private const string StatusCommand = "queryPrinterStatus";

        // Layout the 3217 response data must have for GetSerialNumberAsync: [2..7] digits, [8..9] letters, [10..11] digits.
        private const string SerialResponseData = "XX123456AB99";
        private const string RtType = "M";
        private const string ExpectedSerialNumber = "99MAB123456";

        private static ReceiptRequest Reprint() => new() { ftReceiptCase = 0x4954_2000_0000_3010 };

        private static SignaturItem Reference(SignatureTypesIT signatureType, string data) => new()
        {
            Data = data,
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            ftSignatureType = ITConstants.BASE_STATE | (long) signatureType
        };

        private static ReceiptResponse ResponseWithReferences() => new()
        {
            ftSignatures = new[]
            {
                Reference(SignatureTypesIT.RTReferenceZNumber, "0090"),
                Reference(SignatureTypesIT.RTReferenceDocumentNumber, "0006"),
                Reference(SignatureTypesIT.RTReferenceDocumentMoment, "2026-08-06")
            }
        };

        private static EpsonRTPrinterSCU CreateSut(IEpsonFpMateClient client) =>
            new(NullLogger<EpsonRTPrinterSCU>.Instance, new EpsonRTPrinterSCUConfiguration(), client);

        private static Mock<IEpsonFpMateClient> PrinterReturning(string rtType, ICollection<string> sentPayloads)
        {
            // The reprint response omits the addInfo block, as the device does for directIO 3098 (#743).
            var reprint = SoapSerializer.Serialize(new PrinterReceiptResponse { Success = true });
            var status = SoapSerializer.Serialize(new StatusResponse
            {
                Success = true,
                Printerstatus = new Printerstatus { RtType = rtType, MfStatus = "02", DailyOpen = "1" }
            });
            var serial = SoapSerializer.Serialize(new PrinterCommandResponse
            {
                Success = true,
                CommandResponse = new CommandResponse { ResponseData = SerialResponseData }
            });
            var generic = SoapSerializer.Serialize(new PrinterResponse { Success = true });

            var client = new Mock<IEpsonFpMateClient>();
            client.Setup(c => c.SendCommandAsync(It.IsAny<string>()))
                  .ReturnsAsync((string payload) =>
                  {
                      sentPayloads.Add(payload);
                      var body = generic;
                      if (payload.Contains(ReprintCommand))
                      {
                          body = reprint;
                      }
                      else if (payload.Contains(SerialNumberCommand))
                      {
                          body = serial;
                      }
                      else if (payload.Contains(StatusCommand))
                      {
                          body = status;
                      }
                      return new HttpResponseMessage { Content = new StringContent(body) };
                  });
            return client;
        }

        private static async Task<ReceiptResponse> ReprintAsync(string rtType, ICollection<string> sentPayloads)
        {
            var sut = CreateSut(PrinterReturning(rtType, sentPayloads).Object);
            var response = await sut.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = Reprint(),
                ReceiptResponse = ResponseWithReferences()
            });
            return response.ReceiptResponse;
        }

        private static SignaturItem SerialNumberSignature(ReceiptResponse response) =>
            response.ftSignatures.FirstOrDefault(x => x.Caption == "<rt-serialnumber>");

        [Fact]
        public async Task Reprint_WhenTheResponseCarriesNoReceiptInfo_SignsWithTheSerialNumberOfThePrinter()
        {
            var response = await ReprintAsync(RtType, new List<string>());

            response.HasFailed().Should().BeFalse();
            var serialNumber = SerialNumberSignature(response);
            serialNumber.Should().NotBeNull();
            serialNumber.Data.Should().Be(ExpectedSerialNumber);
        }

        [Fact]
        public async Task Reprint_WhenThePrinterReportsNoRtType_SignsWithAnEmptyStringInsteadOfNull()
        {
            var response = await ReprintAsync(null, new List<string>());

            response.HasFailed().Should().BeFalse();
            var serialNumber = SerialNumberSignature(response);
            serialNumber.Should().NotBeNull();
            serialNumber.Data.Should().NotBeNull();
            serialNumber.Data.Should().Be("");
        }

        [Fact]
        public async Task Reprint_ReadsTheSerialNumberBeforeItSendsTheReprintCommand()
        {
            // A failed read after the reprint would error a document that the printer already printed.
            var sentPayloads = new List<string>();

            await ReprintAsync(RtType, sentPayloads);

            var serialIndex = sentPayloads.FindIndex(x => x.Contains(SerialNumberCommand));
            var reprintIndex = sentPayloads.FindIndex(x => x.Contains(ReprintCommand));
            serialIndex.Should().BeGreaterOrEqualTo(0);
            reprintIndex.Should().BeGreaterOrEqualTo(0);
            serialIndex.Should().BeLessThan(reprintIndex);
        }
    }
}
