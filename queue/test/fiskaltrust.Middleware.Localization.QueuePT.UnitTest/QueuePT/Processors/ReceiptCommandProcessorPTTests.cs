using System;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT.Processors
{
    public class ReceiptCommandProcessorPTTests
    {
        private readonly ReceiptCommandProcessorPT _sut = new ReceiptCommandProcessorPT(Mock.Of<IPTSSCD>());

        [Theory]
        [InlineData(ReceiptCases.UnknownReceipt0x0000)]
        [InlineData(ReceiptCases.PointOfSaleReceipt0x0001)]
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

        [Fact]
        public async Task PointOfSaleReceipt0x0001Async_Should_Return_QRCodeInSignatures()
        {
            var queue = TestHelpers.CreateQueue();
            var queueItem = TestHelpers.CreateQueueItem();

            var configMock = new Mock<IConfigurationRepository>();
            configMock.Setup(x => x.InsertOrUpdateQueueAsync(It.IsAny<ftQueue>())).Returns(Task.CompletedTask);
            var sut = new ReceiptCommandProcessorPT(new InMemorySCU(new InMemorySCUConfiguration
            {
                PrivateKey = File.ReadAllText("PrivateKey.pem"),
            }));

            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid().ToString(),
                ftReceiptCase = 0x5054_2000_0000_0000 | (long) ReceiptCases.InitialOperationReceipt0x4001
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = 0x5054_2000_0000_0000
            };

            var request = new ProcessCommandRequest(queue, null, receiptRequest, receiptResponse, queueItem);
            var result = await sut.PointOfSaleReceipt0x0001Async(request);

            using var scope = new AssertionScope();
            result.receiptResponse.Should().Be(receiptResponse);
            result.actionJournals.Should().BeEmpty();
            result.receiptResponse.ftSignatures.Should().NotBeEmpty();

           
            result.receiptResponse.ftState.Should().Be(0x5054_2000_0000_0000, because: $"ftState {result.receiptResponse.ftState.ToString("X")} is different than expected.");
            var expectedSignaturItem = new SignaturItem
            {
                ftSignatureType = 0x5054_2000_0000_0001,
                ftSignatureFormat = (int) SignaturItem.Formats.QR_Code,
                Caption = "[www.fiskaltrust.pt]",
                Data = $"A:123456789*B:999999990*C:PT*D:FS*E:N*F:YYYY4113*G:*H:0*I1:PT*I2:0,00*I3:0,00*I4:0,00*I5:0,00*I6:0,00*I7:0,00*I8:0,00*N:0,00*O:0,00*Q:NklQ*R:*S:"
            };

            result.receiptResponse.ftSignatures[0].Should().BeEquivalentTo(expectedSignaturItem);
        }
    }
}
