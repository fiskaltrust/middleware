using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.UnitTest.RequestCommandsTests
{
    public class YearlyClosingReceiptCommandTests
    {
        [Fact]
        public async Task ExecuteAsync_YearlyClosing_ValidResultAsync()
        {

            var mockRepository = new Mock<ICountrySpecificQueueRepository>();
            var mockSettings = new Mock<ICountrySpecificSettings>();
            var mockQueue = new ftQueue();

            var queueId = Guid.NewGuid();

            var queueItem = new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queueId,
                ftWorkMoment = DateTime.Now
            };

            var mockCountrySpecificQueue = new Mock<ICountrySpecificQueue>();
            mockCountrySpecificQueue.Setup(q => q.ftQueueId).Returns(queueId);
            mockCountrySpecificQueue.Setup(q => q.CashBoxIdentification).Returns("TestCashBoxIdentification");

            mockRepository.Setup(r => r.GetQueueAsync(queueId)).ReturnsAsync(mockCountrySpecificQueue.Object);

            mockSettings.Setup(s => s.CountrySpecificQueueRepository).Returns(mockRepository.Object);

            var command = new YearlyClosingReceiptCommand(mockSettings.Object);

            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid().ToString(),
                ftPosSystemId = Guid.NewGuid().ToString(),
                cbTerminalID = "D",
                cbReceiptReference = "yearly-closing-" + DateTime.Now.ToString(),
                cbReceiptMoment = DateTime.Now,
                cbChargeItems = Array.Empty<ChargeItem>(),
                cbPayItems = Array.Empty<PayItem>(),
                ftReceiptCase = 4919338172267102214
            };


            var requestResponse = await command.ExecuteAsync(mockQueue, receiptRequest, queueItem);

            requestResponse.ActionJournals.Should().HaveCount(1);
            requestResponse.ActionJournals.FirstOrDefault()?.ftQueueItemId.Should().Be(queueItem.ftQueueItemId);
            requestResponse.ActionJournals.FirstOrDefault()?.Type.Should().Be(receiptRequest.ftReceiptCase.ToString("X"));

        }
    }
}
