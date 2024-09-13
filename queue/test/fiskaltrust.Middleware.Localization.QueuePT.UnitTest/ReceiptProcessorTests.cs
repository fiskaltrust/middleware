using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.v2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest
{
    public class ReceiptProcessorTests
    {
        [Fact]
        public async Task ReceiptProcessor_ThrowException_ReturnErrorResponse()
        {
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = 0x5054_2000_0000_0000
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = 0x5054_2000_0000_0000
            };

            var sut = new ReceiptProcessor(LoggerFactory.Create(x => { }).CreateLogger<ReceiptProcessor>(), null, null, null, null, null, null);
            var result = await sut.ProcessAsync(receiptRequest, receiptResponse, null, null);

            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x5054_2000_EEEE_EEEE);
            result.receiptResponse.ftSignatures.Should().HaveCount(1);
            result.receiptResponse.ftSignatures[0].ftSignatureType.Should().Be(0x5054_2000_0000_3000);
            result.receiptResponse.ftSignatures[0].Caption.Should().Be("FAILURE");
        }

        [Fact]
        public async Task ReceiptProcessor_ReturnNotSupported_ReturnErrorResponse()
        {
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = 0x5054_2000_0000_0000
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = 0x5054_2000_0000_0000
            };

            var sut = new ReceiptProcessor(LoggerFactory.Create(x => { }).CreateLogger<ReceiptProcessor>(), null, null, null, null, null, null);
            var result = await sut.ProcessAsync(receiptRequest, receiptResponse, null, null);

            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x5054_2000_EEEE_EEEE);
            result.receiptResponse.ftSignatures.Should().HaveCount(1);
            result.receiptResponse.ftSignatures[0].ftSignatureType.Should().Be(0x5054_2000_0000_3000);
            result.receiptResponse.ftSignatures[0].Caption.Should().Be("FAILURE");
        }
    }
}
