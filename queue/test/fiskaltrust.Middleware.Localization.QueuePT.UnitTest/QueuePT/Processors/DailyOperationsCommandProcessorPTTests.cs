using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.v2;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT.Processors
{
    public class DailyOperationsCommandProcessorPTTests
    {
        private readonly DailyOperationsCommandProcessorPT _sut = new DailyOperationsCommandProcessorPT();

        [Theory]
        [InlineData(ReceiptCases.ZeroReceipt0x2000)]
        [InlineData(ReceiptCases.OneReceipt0x2001)]
        [InlineData(ReceiptCases.ShiftClosing0x2010)]
        [InlineData(ReceiptCases.DailyClosing0x2011)]
        [InlineData(ReceiptCases.MonthlyClosing0x2012)]
        [InlineData(ReceiptCases.YearlyClosing0x2013)]
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
        public async Task ProcessReceiptAsync_ShouldReturnError_IfInvalidCaseIsUsed()
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
