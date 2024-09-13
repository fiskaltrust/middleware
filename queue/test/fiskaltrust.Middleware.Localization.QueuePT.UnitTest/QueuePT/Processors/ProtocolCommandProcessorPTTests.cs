using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.v2;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT.Processors
{
    public class ProtocolCommandProcessorPTTests
    {
        private readonly ProtocolCommandProcessorPT _sut = new ProtocolCommandProcessorPT();

        [Theory]
        [InlineData(ReceiptCases.ProtocolUnspecified0x3000)]
        [InlineData(ReceiptCases.ProtocolTechnicalEvent0x3001)]
        [InlineData(ReceiptCases.ProtocolAccountingEvent0x3002)]
        [InlineData(ReceiptCases.InternalUsageMaterialConsumption0x3003)]
        [InlineData(ReceiptCases.Order0x3004)]
        [InlineData(ReceiptCases.CopyReceiptPrintExistingReceipt0x3010)]
        public async Task ProcessReceiptAsync_ShouldReturnEmptyList(ReceiptCases receiptCase)
        {
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = (int) receiptCase
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = 0x5054_2000_0000_0000
            };
            var request = new ProcessCommandRequest(null, null, receiptRequest, receiptResponse, null);

            var result = await _sut.ProcessReceiptAsync(request);

            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x5054_2000_0000_0000);
        }

        [Fact]
        public async Task ProcessReceiptAsync_ShouldReturnError()
        {
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = -1
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = 0x5054_2000_0000_0000
            };
            var request = new ProcessCommandRequest(null, null, receiptRequest, receiptResponse, null);

            var result = await _sut.ProcessReceiptAsync(request);
            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x5054_2000_EEEE_EEEE);
        }
    }
}
