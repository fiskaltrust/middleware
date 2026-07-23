using System;
using System.Collections.Generic;
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
        public async Task EnqueueDocument_Should_Send_Synchronously_When_No_Writable_Folder_Even_If_Async_Configured()
        {
            var client = new Mock<IEpsonRTServerClient>();
            client.Setup(x => x.CreateReceiptAsync(It.IsAny<string>()))
                .ReturnsAsync(new RtServerResponse { Success = true, Code = "0", Status = "OK" });

            var configuration = new EpsonRTServerConfiguration { ServerUrl = "https://localhost", SendReceiptsSync = false };
            var queue = new EpsonRTServerCommunicationQueue(Guid.NewGuid(), client.Object,
                NullLogger<EpsonRTServerCommunicationQueue>.Instance, configuration);

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
                NullLogger<EpsonRTServerCommunicationQueue>.Instance, configuration);

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

        [Fact]
        public async Task EnqueueDocument_Should_Cache_To_Disk_When_ServiceFolder_Is_Explicitly_Configured()
        {
            // On-prem pin: an explicitly configured ServiceFolder is the durable location that unlocks
            // async signing. This must keep working exactly as before the fallback removal.
            var serviceFolder = Path.Combine(Path.GetTempPath(), "epsonrtserver-onprem-pin-" + Guid.NewGuid());
            try
            {
                var client = new Mock<IEpsonRTServerClient>();
                var id = Guid.NewGuid();

                var configuration = new EpsonRTServerConfiguration
                {
                    ServerUrl = "https://localhost",
                    SendReceiptsSync = false,
                    ServiceFolder = serviceFolder
                };
                var queue = new EpsonRTServerCommunicationQueue(id, client.Object,
                    NullLogger<EpsonRTServerCommunicationQueue>.Instance, configuration);

                var response = await queue.EnqueueDocument("FISK0001", "<createReceipt/>", 1, 1);

                using (new AssertionScope())
                {
                    response.Should().BeNull("with an explicit ServiceFolder configured, signing is asynchronous");
                    var tillFolder = Path.Combine(serviceFolder, "epsonrtservercache", id.ToString(), "FISK0001");
                    Directory.Exists(tillFolder).Should().BeTrue();
                    Directory.GetFiles(tillFolder, "*_createreceipt.xml").Should().HaveCount(1);
                }
            }
            finally
            {
                if (Directory.Exists(serviceFolder))
                {
                    Directory.Delete(serviceFolder, recursive: true);
                }
            }
        }

        [Fact]
        public async Task ProcessAllReceipts_Should_Park_Out_Of_Sync_Document_Instead_Of_Throwing()
        {
            var serviceFolder = Path.Combine(Path.GetTempPath(), "epsonrtserver-oos-" + Guid.NewGuid());
            var client = new Mock<IEpsonRTServerClient>();
            client.Setup(x => x.CreateReceiptAsync(It.IsAny<string>()))
                .ReturnsAsync(new RtServerResponse { Success = false, Code = "-25", Status = "receipt number error" });
            var id = Guid.NewGuid();
            var configuration = new EpsonRTServerConfiguration { ServerUrl = "https://localhost", SendReceiptsSync = false, ServiceFolder = serviceFolder };
            var queue = new EpsonRTServerCommunicationQueue(id, client.Object, NullLogger<EpsonRTServerCommunicationQueue>.Instance, configuration);
            try
            {
                await queue.EnqueueDocument("FISK0001", "<createReceipt/>", 798, 15);

                Func<Task> act = () => queue.ProcessAllReceipts("FISK0001");

                using (new AssertionScope())
                {
                    // -25 (receipt number error) means the RT Server session moved on; replaying the same
                    // document can never succeed, so the daily-closing drain must park it and continue, not throw.
                    await act.Should().NotThrowAsync();
                    var tillFolder = Path.Combine(serviceFolder, "epsonrtservercache", id.ToString(), "FISK0001");
                    Directory.GetFiles(tillFolder, "*_createreceipt.xml").Should().BeEmpty();
                    Directory.GetFiles(Path.Combine(tillFolder, "failed"), "*_createreceipt.xml").Should().HaveCount(1);
                    // The daily-closing path runs under the SCU lock and must NOT realign (would deadlock).
                    client.Verify(x => x.CreateTokenAsync(It.IsAny<string>()), Times.Never);
                }
            }
            finally
            {
                queue.Dispose();
                if (Directory.Exists(serviceFolder)) Directory.Delete(serviceFolder, recursive: true);
            }
        }

        [Fact]
        public async Task Background_Drain_Should_Realign_Till_And_Park_On_Out_Of_Sync()
        {
            var serviceFolder = Path.Combine(Path.GetTempPath(), "epsonrtserver-realign-" + Guid.NewGuid());
            var client = new Mock<IEpsonRTServerClient>();
            client.Setup(x => x.CreateReceiptAsync(It.IsAny<string>()))
                .ReturnsAsync(new RtServerResponse { Success = false, Code = "-25", Status = "receipt number error" });
            var id = Guid.NewGuid();
            var configuration = new EpsonRTServerConfiguration { ServerUrl = "https://localhost", SendReceiptsSync = false, ServiceFolder = serviceFolder };
            var queue = new EpsonRTServerCommunicationQueue(id, client.Object, NullLogger<EpsonRTServerCommunicationQueue>.Instance, configuration);
            var realigned = new List<string>();
            queue.TillStateRealigner = tillId => { lock (realigned) { realigned.Add(tillId); } return Task.CompletedTask; };
            try
            {
                await queue.EnqueueDocument("FISK0001", "<createReceipt/>", 798, 15);

                // The background drain (started in the ctor) picks up the cached document.
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (DateTime.UtcNow < deadline)
                {
                    lock (realigned) { if (realigned.Count > 0) break; }
                    await Task.Delay(50);
                }

                using (new AssertionScope())
                {
                    lock (realigned) { realigned.Should().ContainSingle().Which.Should().Be("FISK0001"); }
                    var tillFolder = Path.Combine(serviceFolder, "epsonrtservercache", id.ToString(), "FISK0001");
                    var deadline2 = DateTime.UtcNow.AddSeconds(2);
                    while (DateTime.UtcNow < deadline2 && Directory.GetFiles(tillFolder, "*_createreceipt.xml").Length > 0) await Task.Delay(50);
                    Directory.GetFiles(tillFolder, "*_createreceipt.xml").Should().BeEmpty();
                    Directory.GetFiles(Path.Combine(tillFolder, "failed"), "*_createreceipt.xml").Should().HaveCount(1);
                }
            }
            finally
            {
                queue.Dispose();
                if (Directory.Exists(serviceFolder)) Directory.Delete(serviceFolder, recursive: true);
            }
        }

        private const string OutOfSyncReceiptXml =
            "<createReceipt><receipt><hash fingerPrint=\"SEED\"/><printerFiscalReceipt>"
            + "<fiscalInformation zRepNumber=\"0798\" recNumber=\"0015\" dateTime=\"20260723T095711\" />"
            + "</printerFiscalReceipt></receipt><receiptSecurity><hash fingerPrint=\"CCDC0015\" /></receiptSecurity></createReceipt>";

        [Fact]
        public async Task Background_Drain_Should_Consume_Out_Of_Sync_Document_Already_Registered_On_Device()
        {
            var serviceFolder = Path.Combine(Path.GetTempPath(), "epsonrtserver-consume-" + Guid.NewGuid());
            var client = new Mock<IEpsonRTServerClient>();
            client.Setup(x => x.CreateReceiptAsync(It.IsAny<string>()))
                .ReturnsAsync(new RtServerResponse { Success = false, Code = "-25", Status = "receipt number error" });
            // Device confirms it already stored the receipt: the read returns the prefixed CCDC (dateTime+till+Z+rec+ccdc).
            client.Setup(x => x.GetReceiptAsync("FISK0005", 798, 15, "20260723"))
                .ReturnsAsync(new RtServerResponse { Success = true, Code = "0", Status = "OK",
                    AddInfo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["hash"] = "20260723T095711FISK000507980015CCDC0015" } });
            var id = Guid.NewGuid();
            var configuration = new EpsonRTServerConfiguration { ServerUrl = "https://localhost", SendReceiptsSync = false, ServiceFolder = serviceFolder };
            var queue = new EpsonRTServerCommunicationQueue(id, client.Object, NullLogger<EpsonRTServerCommunicationQueue>.Instance, configuration);
            queue.TillStateRealigner = _ => Task.CompletedTask;
            try
            {
                await queue.EnqueueDocument("FISK0005", OutOfSyncReceiptXml, 798, 15);
                var tillFolder = Path.Combine(serviceFolder, "epsonrtservercache", id.ToString(), "FISK0005");
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (DateTime.UtcNow < deadline && Directory.GetFiles(tillFolder, "*_createreceipt.xml").Length > 0) await Task.Delay(50);

                using (new AssertionScope())
                {
                    // A document the device already registered is consumed (not parked): no takings loss, no double-send.
                    await Task.Delay(100);
                    Directory.GetFiles(tillFolder, "*_createreceipt.xml").Should().BeEmpty();
                    var failed = Path.Combine(tillFolder, "failed");
                    (Directory.Exists(failed) ? Directory.GetFiles(failed, "*_createreceipt.xml").Length : 0).Should().Be(0);
                }
            }
            finally
            {
                queue.Dispose();
                if (Directory.Exists(serviceFolder)) Directory.Delete(serviceFolder, recursive: true);
            }
        }

        [Fact]
        public async Task Background_Drain_Should_Park_Out_Of_Sync_Document_Not_Registered_On_Device()
        {
            var serviceFolder = Path.Combine(Path.GetTempPath(), "epsonrtserver-notreg-" + Guid.NewGuid());
            var client = new Mock<IEpsonRTServerClient>();
            client.Setup(x => x.CreateReceiptAsync(It.IsAny<string>()))
                .ReturnsAsync(new RtServerResponse { Success = false, Code = "-25", Status = "receipt number error" });
            // Device does not have it: the read-back throws -31 (file not found).
            client.Setup(x => x.GetReceiptAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>()))
                .ThrowsAsync(new EpsonRTServerCommunicationException("file not found", -31));
            var id = Guid.NewGuid();
            var configuration = new EpsonRTServerConfiguration { ServerUrl = "https://localhost", SendReceiptsSync = false, ServiceFolder = serviceFolder };
            var queue = new EpsonRTServerCommunicationQueue(id, client.Object, NullLogger<EpsonRTServerCommunicationQueue>.Instance, configuration);
            queue.TillStateRealigner = _ => Task.CompletedTask;
            try
            {
                await queue.EnqueueDocument("FISK0005", OutOfSyncReceiptXml, 798, 15);
                var tillFolder = Path.Combine(serviceFolder, "epsonrtservercache", id.ToString(), "FISK0005");
                var failed = Path.Combine(tillFolder, "failed");
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (DateTime.UtcNow < deadline && !(Directory.Exists(failed) && Directory.GetFiles(failed, "*_createreceipt.xml").Length == 1)) await Task.Delay(50);

                using (new AssertionScope())
                {
                    Directory.GetFiles(tillFolder, "*_createreceipt.xml").Should().BeEmpty();
                    Directory.GetFiles(failed, "*_createreceipt.xml").Should().HaveCount(1);
                }
            }
            finally
            {
                queue.Dispose();
                if (Directory.Exists(serviceFolder)) Directory.Delete(serviceFolder, recursive: true);
            }
        }
    }
}
