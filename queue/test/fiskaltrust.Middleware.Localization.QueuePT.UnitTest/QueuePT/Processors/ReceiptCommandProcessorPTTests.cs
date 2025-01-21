using System;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Storage;
using fiskaltrust.Middleware.Storage.PT;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using Xunit;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT.Processors
{
    public class ReceiptCommandProcessorPTTests
    {
        private readonly ReceiptCommandProcessorPT _sut = new ReceiptCommandProcessorPT(Mock.Of<IPTSSCD>(), new ftQueuePT(), new ftSignaturCreationUnitPT(), Mock.Of<IReadOnlyQueueItemRepository>());

        [Theory]
        [InlineData(ReceiptCases.PaymentTransfer0x0002)]
        [InlineData(ReceiptCases.PointOfSaleReceiptWithoutObligation0x0003)]
        [InlineData(ReceiptCases.ECommerce0x0004)]
        [InlineData(ReceiptCases.Protocol0x0005)]
        public async Task ProcessReceiptAsync_ShouldReturnEmptyList(ReceiptCases receiptCase)
        {
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = (int) receiptCase
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
                ftReceiptCase = -1
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
            var request = new ProcessCommandRequest(new ftQueue { }, receiptRequest, receiptResponse);

            var result = await _sut.ProcessReceiptAsync(request);
            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x5054_2000_EEEE_EEEE);
        }

        [Fact]
        public async Task PointOfSaleReceipt0x0001Async_Should_Return_QRCodeInSignatures()
        {
            var queue = TestHelpers.CreateQueue();
            var queueItem = TestHelpers.CreateQueueItem();
            var queuePT = new ftQueuePT
            {
                IssuerTIN = "123456789",
                TaxRegion = "PT",
                ATCUD = "CSDF7T5H0035",
                SimplifiedInvoiceSeries = "AB2019",
                SimplifiedInvoiceSeriesNumerator = 34
            };
            var signaturCreationUnitPT = new ftSignaturCreationUnitPT
            {
                PrivateKey = File.ReadAllText("PrivateKey.pem"),
                SoftwareCertificateNumber = "9999",
            };

            var configMock = new Mock<IConfigurationRepository>();
            configMock.Setup(x => x.InsertOrUpdateQueueAsync(It.IsAny<ftQueue>())).Returns(Task.CompletedTask);

            var queueItemRepository = new Mock<IReadOnlyQueueItemRepository>();

            var sut = new ReceiptCommandProcessorPT(new InMemorySCU(signaturCreationUnitPT), queuePT, signaturCreationUnitPT, queueItemRepository.Object);

            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftReceiptCase = 0x5054_2000_0000_0000 | (long) ReceiptCases.InitialOperationReceipt0x4001,
                cbReceiptMoment = new DateTime(2019, 12, 31),
                cbChargeItems = [
                    new ChargeItem
                    {
                        ftChargeItemCase = 0x5054_2000_0000_0008,
                        Amount = 12000.00m,
                        VATAmount = 0m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = 0x5054_2000_0000_0001,
                        Amount = 15900m,
                        VATAmount = 900m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = 0x5054_2000_0000_0006,
                        Amount = 56500m,
                        VATAmount = 6500m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = 0x5054_2000_0000_0003,
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
                ftState = 0x5054_2000_0000_0000,
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


            result.receiptResponse.ftState.Should().Be(0x5054_2000_0000_0000, because: $"ftState {result.receiptResponse.ftState.ToString("X")} is different than expected.");
            var expectedSignaturItem = new SignatureItem
            {
                ftSignatureType = 0x5054_2000_0000_0001,
                ftSignatureFormat = (int) ifPOS.v1.SignaturItem.Formats.QR_Code,
                Caption = "[www.fiskaltrust.pt]",
                Data = $"A:123456789*B:999999990*C:PT*D:FS*E:N*F:20191231*G:FS AB2019/0035*H:CSDF7T5H0035*I1:PT*I2:12000.00*I3:15000.00*I4:900.00*I5:50000.00*I6:6500.00*I7:80000.00*I8:18400.00*N:25800.00*O:182800.00*Q:jvs6*R:9999*S:ftQueueId={receiptResponse.ftQueueID};ftQueueItemId={receiptResponse.ftQueueItemID}"
            };
            result.receiptResponse.ftQueueID.Should().Be(receiptResponse.ftQueueID);
            result.receiptResponse.ftQueueItemID.Should().Be(receiptResponse.ftQueueItemID);
            result.receiptResponse.ftReceiptIdentification.Should().Be("FS AB2019/0035");
            result.receiptResponse.ftSignatures[0].Should().BeEquivalentTo(expectedSignaturItem);
        }
    }
}
