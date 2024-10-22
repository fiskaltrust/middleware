using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest
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
                ftState = 0x5054_2000_0000_0000,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "receiptIdentification",
                ftReceiptMoment = DateTime.UtcNow,
            };

            var sut = new ReceiptProcessor(LoggerFactory.Create(x => { }).CreateLogger<ReceiptProcessor>(), Mock.Of<ILifecycleCommandProcessor>(MockBehavior.Strict), Mock.Of<IReceiptCommandProcessor>(MockBehavior.Strict), Mock.Of<IDailyOperationsCommandProcessor>(MockBehavior.Strict), Mock.Of<IInvoiceCommandProcessor>(MockBehavior.Strict), Mock.Of<IProtocolCommandProcessor>(MockBehavior.Strict));
            var result = await sut.ProcessAsync(receiptRequest, receiptResponse, new ftQueue { }, new ftQueueItem { });

            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x4752_2000_EEEE_EEEE);
            result.receiptResponse.ftSignatures.Should().HaveCount(1);
            result.receiptResponse.ftSignatures[0].ftSignatureType.Should().Be(0x5054_2000_0000_3000);
            result.receiptResponse.ftSignatures[0].Caption.Should().Be("FAILURE");
        }

        [Fact]
        public async Task ReceiptProcessor_ReturnNotSupported_ReturnErrorResponse()
        {
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = 0x4752_2000_0000_0000
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = 0x4752_2000_0000_0000,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "receiptIdentification",
                ftReceiptMoment = DateTime.UtcNow,
            };

            var sut = new ReceiptProcessor(LoggerFactory.Create(x => { }).CreateLogger<ReceiptProcessor>(), Mock.Of<ILifecycleCommandProcessor>(MockBehavior.Strict), Mock.Of<IReceiptCommandProcessor>(MockBehavior.Strict), Mock.Of<IDailyOperationsCommandProcessor>(MockBehavior.Strict), Mock.Of<IInvoiceCommandProcessor>(MockBehavior.Strict), Mock.Of<IProtocolCommandProcessor>(MockBehavior.Strict));
            var result = await sut.ProcessAsync(receiptRequest, receiptResponse, new ftQueue { }, new ftQueueItem { });

            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x4752_2000_EEEE_EEEE);
            result.receiptResponse.ftSignatures.Should().HaveCount(1);
            result.receiptResponse.ftSignatures[0].ftSignatureType.Should().Be(0x5054_2000_0000_3000);
            result.receiptResponse.ftSignatures[0].Caption.Should().Be("FAILURE");
        }
    }
}
