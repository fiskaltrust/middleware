using System;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.myDataSCU;
using fiskaltrust.Middleware.Localization.QueueGR.Processors;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Storage;
using fiskaltrust.Middleware.Storage.GR;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using Xunit;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest.QueueGR.Processors
{
    public class ReceiptCommandProcessorGRTests
    {
        private readonly ReceiptProcessor _sut = new(Mock.Of<ILogger<ReceiptProcessor>>(), null!, new ReceiptCommandProcessorGR(Mock.Of<IGRSSCD>(), new ftQueueGR(), new ftSignaturCreationUnitGR()), null!, null!, null!);

        [Theory]
        [InlineData(ReceiptCase.PaymentTransfer0x0002, Skip = "broken")]
        [InlineData(ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003, Skip = "broken")]
        [InlineData(ReceiptCase.ECommerce0x0004, Skip = "broken")]
        [InlineData(ReceiptCase.Protocol0x0005, Skip = "broken")]
        public async Task ProcessReceiptAsync_ShouldReturnEmptyList(ReceiptCase receiptCase)
        {
            var queue = TestHelpers.CreateQueue();
            var queueItem = TestHelpers.CreateQueueItem();
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = receiptCase
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = (State) 0x4752_2000_0000_0000,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "receiptIdentification",
                ftReceiptMoment = DateTime.UtcNow,
            };
            var result = await _sut.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000);
        }

        [Fact]
        public async Task ProcessReceiptAsync_ShouldReturnError()
        {
            var queue = TestHelpers.CreateQueue();
            var queueItem = TestHelpers.CreateQueueItem();
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = (ReceiptCase) (-1)
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = (State) 0x4752_2000_0000_0000,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "receiptIdentification",
                ftReceiptMoment = DateTime.UtcNow,
            };
            var result = await _sut.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x4752_2000_EEEE_EEEE);
        }

        [Fact(Skip = "broken")]
        public async Task PointOfSaleReceipt0x0001Async_Should_Return_QRCodeInSignatures()
        {
            var queue = TestHelpers.CreateQueue();
            var queueItem = TestHelpers.CreateQueueItem();
            var queuePT = new ftQueueGR();
            var signaturCreationUnitPT = new ftSignaturCreationUnitGR
            {

            };

            var configMock = new Mock<storage.V0.IConfigurationRepository>();
            configMock.Setup(x => x.InsertOrUpdateQueueAsync(It.IsAny<ftQueue>())).Returns(Task.CompletedTask);
            var sut = new ReceiptCommandProcessorGR(new MyDataApiClient("", "", false, new MasterDataConfiguration(), true), queuePT, signaturCreationUnitPT);

            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftReceiptCase = (ReceiptCase) (0x4752_2000_0000_0000 | (long) ReceiptCase.InitialOperationReceipt0x4001),
                cbReceiptMoment = new DateTime(2019, 12, 31),
                cbChargeItems = [
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0008,
                        Amount = 12000.00m,
                        VATAmount = 0m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0001,
                        Amount = 15900m,
                        VATAmount = 900m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0006,
                        Amount = 56500m,
                        VATAmount = 6500m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0003,
                        Amount = 98400m,
                        VATAmount = 18400m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                ]
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = (State) 0x4752_2000_0000_0000,
                ftQueueID = queue.ftQueueId,
                ftQueueItemID = queueItem.ftQueueItemId,
                ftCashBoxIdentification = "cashBoxIdentification",

                ftQueueRow = 1,
                ftReceiptIdentification = "receiptIdentification",
                ftReceiptMoment = DateTime.UtcNow,
            };

            var request = new ProcessCommandRequest(queue, receiptRequest, receiptResponse);
            var result = await sut.PointOfSaleReceipt0x0001Async(request);

            using var scope = new AssertionScope();
            result.receiptResponse.Should().Be(receiptResponse);
            result.actionJournals.Should().BeEmpty();
            result.receiptResponse.ftSignatures.Should().NotBeEmpty();


            result.receiptResponse.ftState.Should().Be(0x4752_2000_0000_0000, because: $"ftState {result.receiptResponse.ftState.ToString("X")} is different than expected.");
            var expectedSignaturItem = new SignatureItem
            {
                ftSignatureType = (SignatureType) 0x4752_2000_0000_0001,
                ftSignatureFormat = SignatureFormat.QRCode,
                Caption = "[www.fiskaltrust.gr]",
                Data = $"??????"
            };
            result.receiptResponse.ftQueueID.Should().Be(receiptResponse.ftQueueID);
            result.receiptResponse.ftQueueItemID.Should().Be(receiptResponse.ftQueueItemID);
            result.receiptResponse.ftReceiptIdentification.Should().Be("????");
            result.receiptResponse.ftSignatures[0].Should().BeEquivalentTo(expectedSignaturItem);
        }
    }
}
