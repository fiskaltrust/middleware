// C#
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest
{
    public class SignProcessorTests
    {
        private SignProcessor CreateSignProcessor(
            Mock<IMiddlewareQueueItemRepository> queueItemRepositoryMock,
            MiddlewareConfiguration? config = null)
        {
            var loggerMock = new Mock<ILogger<SignProcessor>>();
            var storageProviderMock = new Mock<IStorageProvider>();
            var actionJournalRepositoryMock = new Mock<IMiddlewareActionJournalRepository>();
            actionJournalRepositoryMock.Setup(x => x.InsertAsync(It.IsAny<ftActionJournal>())).Returns(Task.CompletedTask);
            var receiptJournalRepositoryMock = new Mock<IMiddlewareReceiptJournalRepository>();
            receiptJournalRepositoryMock.Setup(x => x.InsertAsync(It.IsAny<ftReceiptJournal>())).Returns(Task.CompletedTask);

            var processRequestMock = new Func<ReceiptRequest, ReceiptResponse, ftQueue, ftQueueItem, Task<(ReceiptResponse, List<ftActionJournal>)>>(
                (req, resp, queue, item) => Task.FromResult((resp, new List<ftActionJournal>()))
            );
            var cashBoxIdentification = new AsyncLazy<string>(() => Task.FromResult("TestCashBoxIdentification"));
            var queueItemRepositoryLazy = new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(queueItemRepositoryMock.Object));
            var configuration = config ?? new MiddlewareConfiguration
            {
                QueueId = Guid.NewGuid(),
                CashBoxId = Guid.NewGuid(),
                IsSandbox = false,
                ReceiptRequestMode = 0
            };

            var configurationRepositoryMock = new Mock<IConfigurationRepository>();
            configurationRepositoryMock.Setup(x => x.GetQueueAsync(configuration.QueueId)).ReturnsAsync(new ftQueue
            {
                ftQueueId = configuration.QueueId,
                Timeout = 300,
                StartMoment = DateTime.MinValue,
                StopMoment = null,
                CountryCode = "EU"
            });

            storageProviderMock.Setup(x => x.CreateConfigurationRepository()).Returns(new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(configurationRepositoryMock.Object)));
            storageProviderMock.Setup(x => x.CreateMiddlewareQueueItemRepository()).Returns(queueItemRepositoryLazy);
            storageProviderMock.Setup(x => x.CreateMiddlewareActionJournalRepository()).Returns(new AsyncLazy<IMiddlewareActionJournalRepository>(() => Task.FromResult(actionJournalRepositoryMock.Object)));
            storageProviderMock.Setup(x => x.CreateMiddlewareReceiptJournalRepository()).Returns(new AsyncLazy<IMiddlewareReceiptJournalRepository>(() => Task.FromResult(receiptJournalRepositoryMock.Object)));

            return new SignProcessor(
                loggerMock.Object,
                new QueueStorageProvider(configuration.QueueId, storageProviderMock.Object),
                processRequestMock,
                cashBoxIdentification,
                configuration
            );
        }

        private static ftQueueItem CreateQueueItem(string reference, ReceiptRequest req, ReceiptResponse resp)
        {
            return new ftQueueItem
            {
                cbReceiptReference = reference,
                request = JsonSerializer.Serialize(req),
                response = JsonSerializer.Serialize(resp),
                ftDoneMoment = DateTime.Now,
                responseHash = "test"
            };
        }

        [Fact]
        public async Task ProcessAsync_SetsMiddlewareState_WithSinglePreviousReceiptReference()
        {
            // Arrange
            var previousRef = "prev-ref-1";
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                cbPreviousReceiptReference = previousRef,
                cbChargeItems = [],
                cbPayItems = []
            };
            var expectedPrevRequest = new ReceiptRequest { ftCashBoxID = receiptRequest.ftCashBoxID };
            var expectedPrevResponse = new ReceiptResponse { ftCashBoxIdentification = "Test" };
            var queueItem = CreateQueueItem(previousRef, expectedPrevRequest, expectedPrevResponse);

            var repoMock = new Mock<IMiddlewareQueueItemRepository>();
            repoMock.Setup(r => r.GetByReceiptReferenceAsync(previousRef, It.IsAny<string?>()))
                .Returns(new[] { queueItem }.ToAsyncEnumerable());

            var processor = CreateSignProcessor(repoMock, new MiddlewareConfiguration { CashBoxId = receiptRequest.ftCashBoxID.Value });

            // Act
            var response = await processor.ProcessAsync(receiptRequest);

            // Assert
            response.Should().NotBeNull();
            response!.ftStateData.Should().NotBeNull();
            var middlewareStateData = (MiddlewareStateData) response.ftStateData!;
            middlewareStateData.PreviousReceiptReference.Should().NotBeNull();
            middlewareStateData.PreviousReceiptReference.Should().HaveCount(1);
            middlewareStateData.PreviousReceiptReference![0].Request.ftCashBoxID.Should().Be(receiptRequest.ftCashBoxID);
            middlewareStateData.PreviousReceiptReference![0].Response.ftCashBoxIdentification.Should().Be("Test");
        }

        [Fact]
        public async Task ProcessAsync_SetsMiddlewareState_WithMultiplePreviousReceiptReferences()
        {
            // Arrange
            var previousRefs = new[] { "prev-ref-1", "prev-ref-2" };
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                cbPreviousReceiptReference = previousRefs,
                cbChargeItems = [],
                cbPayItems = []
            };

            var queueItems = new List<ftQueueItem>();
            for (int i = 0; i < previousRefs.Length; i++)
            {
                var req = new ReceiptRequest { ftCashBoxID = receiptRequest.ftCashBoxID };
                var resp = new ReceiptResponse { ftCashBoxIdentification = $"Test{i}" };
                queueItems.Add(CreateQueueItem(previousRefs[i], req, resp));
            }

            var repoMock = new Mock<IMiddlewareQueueItemRepository>();
            repoMock.Setup(r => r.GetByReceiptReferenceAsync(previousRefs[0], It.IsAny<string?>()))
                .Returns(new[] { queueItems[0] }.ToAsyncEnumerable());
            repoMock.Setup(r => r.GetByReceiptReferenceAsync(previousRefs[1], It.IsAny<string?>()))
                .Returns(new[] { queueItems[1] }.ToAsyncEnumerable());

            var processor = CreateSignProcessor(repoMock, new MiddlewareConfiguration { CashBoxId = receiptRequest.ftCashBoxID.Value });

            // Act
            var response = await processor.ProcessAsync(receiptRequest);

            // Assert
            response.Should().NotBeNull();
            response!.ftStateData.Should().NotBeNull();
            var middlewareStateData = (MiddlewareStateData) response.ftStateData!;
            middlewareStateData.PreviousReceiptReference.Should().NotBeNull();
            middlewareStateData.PreviousReceiptReference.Should().HaveCount(2);
            middlewareStateData.PreviousReceiptReference![0].Response.ftCashBoxIdentification.Should().Be("Test0");
            middlewareStateData.PreviousReceiptReference[1].Response.ftCashBoxIdentification.Should().Be("Test1");
        }

        [Fact]
        public async Task ProcessAsync_SetsMiddlewareState_WithNoPreviousReceiptReferences()
        {
            // Arrange
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                cbChargeItems = [],
                cbPayItems = []
            };

            var repoMock = new Mock<IMiddlewareQueueItemRepository>();
            var processor = CreateSignProcessor(repoMock, new MiddlewareConfiguration { CashBoxId = receiptRequest.ftCashBoxID.Value });

            // Act
            var response = await processor.ProcessAsync(receiptRequest);

            // Assert
            response.Should().NotBeNull();
            response!.ftStateData.Should().BeNull();
        }
    }
}