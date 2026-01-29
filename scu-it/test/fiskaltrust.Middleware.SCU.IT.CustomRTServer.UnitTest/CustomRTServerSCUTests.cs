using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.CustomRTServer;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer.UnitTest
{
    public class CustomRTServerSCUTests
    {
        private static readonly Guid _queueId = Guid.Parse("da8147d6-a0c2-42c6-8091-4ea7adbe4afc");
        private static readonly Guid _scuId = Guid.Parse("5b95ea47-dbf7-4ba6-bcab-ae46030bc0e9");

        private static ReceiptResponse GetReceiptResponse() => new ReceiptResponse
        {
            ftCashBoxIdentification = "TEST0001",
            ftQueueID = _queueId.ToString(),
            ftSignatures = Array.Empty<SignaturItem>()
        };

        [Fact]
        public async Task ProcessReceiptAsync_ProtocolUnspecified0x3000_WithBarcodeFlag_ShouldReturnNoOp()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<CustomRTServerSCU>>();
            var mockClient = new Mock<CustomRTServerClient>(
                new CustomRTServerConfiguration(), 
                null);
            var mockQueue = new Mock<CustomRTServerCommunicationQueue>(
                new CustomRTServerConfiguration(),
                mockClient.Object,
                Mock.Of<ILogger<CustomRTServerCommunicationQueue>>());

            var scu = new CustomRTServerSCU(
                _scuId,
                mockLogger.Object,
                new CustomRTServerConfiguration(),
                mockClient.Object,
                mockQueue.Object);

            var request = new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetProtocolUnspecifiedWithBarcodeFlag(),
                ReceiptResponse = GetReceiptResponse()
            };

            // Act
            var result = await scu.ProcessReceiptAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.ReceiptResponse.Should().NotBeNull();
            result.ReceiptResponse.ftSignatures.Should().BeEmpty("NoOp should return empty signatures");
        }

        [Fact]
        public async Task ProcessReceiptAsync_ProtocolUnspecified0x3000_WithoutFlags_ShouldReturnNoOp()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<CustomRTServerSCU>>();
            var mockClient = new Mock<CustomRTServerClient>(
                new CustomRTServerConfiguration(), 
                null);
            var mockQueue = new Mock<CustomRTServerCommunicationQueue>(
                new CustomRTServerConfiguration(),
                mockClient.Object,
                Mock.Of<ILogger<CustomRTServerCommunicationQueue>>());

            var scu = new CustomRTServerSCU(
                _scuId,
                mockLogger.Object,
                new CustomRTServerConfiguration(),
                mockClient.Object,
                mockQueue.Object);

            // Create a simple ProtocolUnspecified0x3000 request without any barcode flags
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = "00000000-0000-0000-0000-000000000000",
                ftPosSystemId = "00000000-0000-0000-0000-000000000000",
                cbTerminalID = "00010001",
                cbReceiptReference = "Protocol-Unspecified",
                cbReceiptMoment = DateTime.UtcNow,
                cbChargeItems = Array.Empty<ChargeItem>(),
                cbPayItems = Array.Empty<PayItem>(),
                ftReceiptCase = 0x4954_2000_0000_3000, // ProtocolUnspecified0x3000 without additional flags
                cbUser = "Admin"
            };

            var request = new ProcessRequest
            {
                ReceiptRequest = receiptRequest,
                ReceiptResponse = GetReceiptResponse()
            };

            // Act
            var result = await scu.ProcessReceiptAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.ReceiptResponse.Should().NotBeNull();
            result.ReceiptResponse.ftSignatures.Should().BeEmpty("NoOp should return empty signatures");
        }
    }
}
