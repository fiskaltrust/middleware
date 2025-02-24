using System.Threading.Tasks;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT.Processors
{
    public class ProtocolCommandProcessorPTTests
    {
        private readonly ProtocolCommandProcessorPT _sut = new ProtocolCommandProcessorPT();

        [Theory]
        [InlineData(ReceiptCase.ProtocolUnspecified0x3000)]
        [InlineData(ReceiptCase.ProtocolTechnicalEvent0x3001)]
        [InlineData(ReceiptCase.ProtocolAccountingEvent0x3002)]
        [InlineData(ReceiptCase.InternalUsageMaterialConsumption0x3003)]
        [InlineData(ReceiptCase.Order0x3004)]
        [InlineData(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010)]
        public async Task ProcessReceiptAsync_ShouldReturnEmptyList(ReceiptCase receiptCase)
        {
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = receiptCase
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = (State) 0x5054_2000_0000_0000,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "receiptIdentification",
                ftReceiptMoment = DateTime.UtcNow,
            };
            var request = new ProcessCommandRequest(new ftQueue { }, receiptRequest, receiptResponse);

            var result = await _sut.ProcessReceiptAsync(request);

            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x5054_2000_0000_0000);
        }

        [Fact]
        public async Task ProcessReceiptAsync_ShouldReturnError()
        {
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = (ReceiptCase) (-1)
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = (State) 0x5054_2000_0000_0000,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "receiptIdentification",
                ftReceiptMoment = DateTime.UtcNow,
            };
            var request = new ProcessCommandRequest(new ftQueue { }, receiptRequest, receiptResponse);

            var result = await _sut.ProcessReceiptAsync(request);
            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x5054_2000_EEEE_EEEE);
        }
    }
}
