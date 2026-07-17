using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.IT.EpsonRTServer.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer.UnitTest
{
    public class EpsonRTServerCommunicationQueueTests
    {
        [Fact]
        public async Task EnqueueDocument_Should_Send_Synchronously_When_No_Writable_Folder_Even_If_Async_Configured()
        {
            var client = new Mock<IEpsonRTServerClient>();
            client.Setup(x => x.CreateReceiptAsync(It.IsAny<string>()))
                .ReturnsAsync(new RtServerResponse { Success = true, Code = "0", Status = "OK" });

            var configuration = new EpsonRTServerConfiguration { ServerUrl = "https://localhost", SendReceiptsSync = false };
            var queue = new EpsonRTServerCommunicationQueue(Guid.NewGuid(), client.Object,
                NullLogger<EpsonRTServerCommunicationQueue>.Instance, configuration, personalFolderProvider: () => string.Empty);

            var response = await queue.EnqueueDocument("FISK0001", "<createReceipt/>", 1, 1);

            using (new AssertionScope())
            {
                // Async would cache to disk and return null; with no folder it must send synchronously.
                response.Should().NotBeNull();
                client.Verify(x => x.CreateReceiptAsync("<createReceipt/>"), Times.Once);
            }
        }
    }
}
