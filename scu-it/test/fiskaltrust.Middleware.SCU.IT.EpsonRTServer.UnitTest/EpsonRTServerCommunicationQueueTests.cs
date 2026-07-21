using System;
using System.IO;
using System.Reflection;
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
        public async Task EnqueueDocument_Should_Send_Synchronously_When_LauncherEnvironment_Is_Cloud_Even_With_Writable_Folder()
        {
            // Regression for the cloud image: Environment.GetFolderPath(SpecialFolder.Personal) resolves to
            // "/root" there (root, no $HOME), which IS a writable/creatable folder, so the disk-cache heuristic
            // alone would allow async and buffer fiscal documents on a pod with no durable storage. The host
            // must be able to force sync explicitly via LauncherEnvironment="cloud" regardless of what the
            // folder heuristic says.
            var tempServiceFolder = Path.Combine(Path.GetTempPath(), "epsonrtserver-cloud-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempServiceFolder);
            try
            {
                var client = new Mock<IEpsonRTServerClient>();
                client.Setup(x => x.CreateReceiptAsync(It.IsAny<string>()))
                    .ReturnsAsync(new RtServerResponse { Success = true, Code = "0", Status = "OK" });

                var configuration = new EpsonRTServerConfiguration
                {
                    ServerUrl = "https://localhost",
                    SendReceiptsSync = false,
                    ServiceFolder = tempServiceFolder,
                    LauncherEnvironment = "cloud",
                };
                var queue = new EpsonRTServerCommunicationQueue(Guid.NewGuid(), client.Object,
                    NullLogger<EpsonRTServerCommunicationQueue>.Instance, configuration);

                var response = await queue.EnqueueDocument("FISK0001", "<createReceipt/>", 1, 1);

                using (new AssertionScope())
                {
                    // Async would cache to disk and return null; on a stateless/cloud host it must send synchronously.
                    response.Should().NotBeNull();
                    client.Verify(x => x.CreateReceiptAsync("<createReceipt/>"), Times.Once);

                    // No document must ever have been written to disk: a stateless host cannot durably hold a
                    // buffered fiscal document, so nothing may be persisted even transiently.
                    Directory.EnumerateFileSystemEntries(tempServiceFolder, "*", SearchOption.AllDirectories)
                        .Should().BeEmpty("a stateless/cloud host must never buffer fiscal documents to disk");
                }
            }
            finally
            {
                Directory.Delete(tempServiceFolder, recursive: true);
            }
        }

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

        [Fact]
        public async Task ProcessAllReceipts_Should_Not_Throw_Or_Call_Client_When_No_Writable_Folder()
        {
            var client = new Mock<IEpsonRTServerClient>();

            var configuration = new EpsonRTServerConfiguration { ServerUrl = "https://localhost", SendReceiptsSync = false };
            var queue = new EpsonRTServerCommunicationQueue(Guid.NewGuid(), client.Object,
                NullLogger<EpsonRTServerCommunicationQueue>.Instance, configuration, personalFolderProvider: () => string.Empty);

            // Reached on every daily closing (EpsonRTServerSCU.PerformDailyClosingAsync). With no disk cache
            // there is nothing to drain, so this must be a no-op that never touches the (never-created) cache path.
            Func<Task> act = () => queue.ProcessAllReceipts("FISK0001");

            using (new AssertionScope())
            {
                await act.Should().NotThrowAsync();
                client.Verify(x => x.CreateReceiptAsync(It.IsAny<string>()), Times.Never);

                // The awaited call alone doesn't reveal the real defect: the `finally` block used to
                // unconditionally restart the drain via a fire-and-forget `Task.Run`, which then hit
                // Directory.GetDirectories on a path that was never created, throwing every ~2s forever
                // (caught and logged, never surfacing here). That loop sets this flag true on its very first
                // iteration, so poll briefly for the regression instead of trusting a fixed sleep.
                var processingField = typeof(EpsonRTServerCommunicationQueue)
                    .GetField("_processingReceipts", BindingFlags.NonPublic | BindingFlags.Instance)!;
                var deadline = DateTime.UtcNow.AddMilliseconds(300);
                while (DateTime.UtcNow < deadline && !(bool)processingField.GetValue(queue)!)
                {
                    await Task.Delay(10);
                }
                ((bool)processingField.GetValue(queue)!).Should().BeFalse(
                    "the background drain must never (re)start when there is no writable cache folder");
            }
        }
    }
}
