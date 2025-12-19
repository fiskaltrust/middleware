using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Queue.AcceptanceTest
{

    public class SignProcessorUnitTests
    {
        [Fact]
        public async Task CreateReceiptJournalAsync_Should_Match()
        {
            var loggergMock = new Mock<ILogger<SignProcessor>>(MockBehavior.Strict);
            var configMock = new Mock<IConfigurationRepository>(MockBehavior.Strict);
            var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
            var receiptJournalRepositoryMock = new Mock<IMiddlewareReceiptJournalRepository>(MockBehavior.Strict);
            var actionJournalRepositoryMock = new Mock<IMiddlewareActionJournalRepository>(MockBehavior.Strict);
            var cryptoHelperMock = new Mock<ICryptoHelper>(MockBehavior.Strict);
            var marketSpecificSignProcessorMock = new Mock<IMarketSpecificSignProcessor>(MockBehavior.Strict);
            var queueId = Guid.NewGuid();
            var cashboxId = Guid.NewGuid();
            var receiptRequestMode = 0;
            var receiptHash = "MyHash";

            var queue = new ftQueue
            {
                ftCashBoxId = cashboxId,
                ftQueueId = queueId
            };

            var queueItem = new ftQueueItem()
            {
                ftQueueItemId = Guid.NewGuid(),
            };
            var request = new ReceiptRequest()
            {
                cbReceiptAmount = 21,
            };

            var receiptJournal = new ftReceiptJournal()
            {
                ftQueueId = queueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                ftReceiptNumber = queue.ftReceiptNumerator,
                ftReceiptHash = receiptHash
            };

            var configuration = new MiddlewareConfiguration
            {
                QueueId = queueId,
                CashBoxId = cashboxId,
                ReceiptRequestMode = receiptRequestMode,
                ProcessingVersion = "test"
            };

            string previousReceiptHash = null;
            cryptoHelperMock.Setup(x => x.GenerateBase64ChainHash(previousReceiptHash, It.Is<ftReceiptJournal>(rj => rj.ftQueueItemId == queueItem.ftQueueItemId), queueItem)).Returns(receiptHash);
            receiptJournalRepositoryMock.Setup(x => x.InsertAsync(It.IsAny<ftReceiptJournal>())).Returns(Task.CompletedTask);
            configMock.Setup(x => x.InsertOrUpdateQueueAsync(queue)).Returns(Task.CompletedTask);

            var sut = new SignProcessor(loggergMock.Object, configMock.Object, queueItemRepositoryMock.Object, receiptJournalRepositoryMock.Object, actionJournalRepositoryMock.Object, cryptoHelperMock.Object, marketSpecificSignProcessorMock.Object, configuration);

            await sut.CreateReceiptJournalAsync(queue, queueItem, request);
            receiptJournalRepositoryMock.Verify(x => x.InsertAsync(It.Is<ftReceiptJournal>(rj =>
                rj.ftQueueItemId == queueItem.ftQueueItemId &&
                rj.ftQueueId == queue.ftQueueId &&
                rj.ftReceiptHash == receiptHash &&
                rj.ftReceiptNumber == queue.ftReceiptNumerator
            )), Times.Once());
        }

        [Fact]
        public void ProcessAsync_WhenExceptionIsThrown_Should_ThrowExceptionAndSaveToQueueItem()
        {
            var logger = new Mock<ILogger<SignProcessor>>(MockBehavior.Loose);

            var receiptJournalRepository = new Mock<IMiddlewareReceiptJournalRepository>(MockBehavior.Strict);

            var actionJournalRepository = new Mock<IMiddlewareActionJournalRepository>(MockBehavior.Strict);
            actionJournalRepository.Setup(x => x.InsertAsync(It.IsAny<ftActionJournal>())).Returns(Task.CompletedTask);

            var cryptoHelper = new Mock<ICryptoHelper>(MockBehavior.Strict);
            cryptoHelper.Setup(x => x.GenerateBase64Hash(It.IsAny<string>())).Returns("MyHash");

            var queueId = Guid.NewGuid();
            var cashboxId = Guid.NewGuid();

            var queue = new ftQueue
            {
                ftCashBoxId = cashboxId,
                ftQueueId = queueId,
                ftCurrentRow = 1,
                CountryCode = "AT"
            };

            var configuration = new MiddlewareConfiguration
            {
                QueueId = queueId,
                CashBoxId = cashboxId,
                ProcessingVersion = "test"
            };

            var configurationRepository = new Mock<IConfigurationRepository>(MockBehavior.Strict);
            configurationRepository.Setup(x => x.GetQueueAsync(queueId)).ReturnsAsync(queue);
            configurationRepository.Setup(x => x.InsertOrUpdateQueueAsync(queue)).Returns(Task.CompletedTask);

            var request = new ReceiptRequest()
            {
                ftCashBoxID = cashboxId.ToString(),
                ftQueueID = queueId.ToString(),
                cbTerminalID = "MyTerminalId",
            };

            var marketSpecificSignProcessor = new Mock<IMarketSpecificSignProcessor>(MockBehavior.Strict);
            marketSpecificSignProcessor.Setup(x => x.ProcessAsync(request, queue, It.IsAny<ftQueueItem>())).ThrowsAsync(new Exception("MyException"));
            marketSpecificSignProcessor.Setup(x => x.GetFtCashBoxIdentificationAsync(queue)).ReturnsAsync("MyCashBoxIdentification");
            marketSpecificSignProcessor.Setup(x => x.FirstTaskAsync()).Returns(Task.CompletedTask);
            marketSpecificSignProcessor.Setup(x => x.FinalTaskAsync(queue, It.IsAny<ftQueueItem>(), request, actionJournalRepository.Object, It.IsAny<IMiddlewareQueueItemRepository>(), receiptJournalRepository.Object)).Returns(Task.CompletedTask);

            var expectedCountryBits = EncodeCountry_TestOnly(queue.CountryCode);

            var matchResponse = (ftQueueItem queueItem, ReceiptResponse response) =>
            {
                try
                {
                    response.ftCashBoxIdentification.Should().Be("MyCashBoxIdentification");
                    response.ftQueueID.Should().Be(queueId.ToString());
                    response.ftCashBoxID.Should().Be(cashboxId.ToString());
                    response.ftState.Should().Match(x => (x & 0xFFFF_FFFF) == 0xEEEE_EEEE);
                    response.ftQueueItemID.Should().Be(queueItem.ftQueueItemId.ToString());
                    response.cbTerminalID.Should().Be(request.cbTerminalID);
                    response.ftQueueRow.Should().Be(1);

                    response.ftSignatures.Should().HaveCount(1).And.ContainSingle(x =>
                        x.ftSignatureType == (long)(expectedCountryBits | 0x2000_0000_3000)
                        && x.ftSignatureFormat == 0x1
                        && x.Caption == "uncaught-exeption"
                        && x.Data.StartsWith("System.Exception: MyException") && x.Data.Contains("\n")
                    );
                }
                catch
                {
                    return false;
                }

                return true;
            };

            var queueItemRepository = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
            queueItemRepository.Setup(x => x.InsertOrUpdateAsync(It.Is<ftQueueItem>(qi => qi.ftQueueId == queueId && qi.response == null))).Returns(Task.CompletedTask).Verifiable();
            queueItemRepository.Setup(x => x.InsertOrUpdateAsync(It.Is<ftQueueItem>(qi => qi.ftQueueId == queueId && qi.response != null && matchResponse(qi, JsonConvert.DeserializeObject<ReceiptResponse>(qi.response))))).Returns(Task.CompletedTask).Verifiable();

            var sut = new SignProcessor(logger.Object, configurationRepository.Object, queueItemRepository.Object, receiptJournalRepository.Object, actionJournalRepository.Object, cryptoHelper.Object, marketSpecificSignProcessor.Object, configuration);

            var process = async () => await sut.ProcessAsync(request);

            process.Should().Throw<Exception>().WithMessage("MyException");

            queueItemRepository.Verify();
        }

        [Fact]
        public async Task RequestPreviousReceipt_WithV1RequestAndFtStateError_ShouldReturnNull()
        {
            var (request, configuration, queueItemRepository) = SetupTestEnvironment(
            ftReceiptCase: 0x0000800000000000L, // V1 tagging
            ftState: 0xEEEE_EEEE);

            var sut = CreateSignProcessor(queueItemRepository, configuration);

            var response = await sut.ProcessAsync(request);
            response.Should().BeNull();           
        }
     
        [Fact]
        public async Task RequestPreviousReceipt_WithNonErrorFtState_ShouldReturnResponseRegardlessOfTagging()
        {
            var (request, configuration, queueItemRepository) = SetupTestEnvironment(
                 ftReceiptCase: 0x0000800000000000L, // V1 tagging
                 ftState: 0x0000_EEEE);

            var sut = CreateSignProcessor(queueItemRepository, configuration);

            var response = await sut.ProcessAsync(request);
            response.Should().NotBeNull();
        }
        
        [Fact]
        public async Task ProcessAsync_WhenCountrySpecificThrows_ShouldSetFtStateCountryFromQueue()
        {
            var queueId = Guid.NewGuid();
            var cashBoxId = Guid.NewGuid();

            var queue = new ftQueue
            {
                ftQueueId = queueId,
                ftCashBoxId = cashBoxId,
                ftQueuedRow = 0,
                ftCurrentRow = 1,
                CountryCode = "DE"
            };

            var request = new ReceiptRequest
            {
                ftCashBoxID = cashBoxId.ToString(),
                ftReceiptCase = 0x4154000000000000,
                cbReceiptReference = "ABC",
                cbTerminalID = "TERM1",
                cbReceiptMoment = DateTime.UtcNow,
                cbChargeItems = Array.Empty<ChargeItem>(),
                cbPayItems = Array.Empty<PayItem>()
            };

            var logger = new Mock<ILogger<SignProcessor>>();

            var configurationRepo = new Mock<IConfigurationRepository>();
            configurationRepo
                .Setup(x => x.GetQueueAsync(queueId))
                .ReturnsAsync(queue);

            ftQueueItem? persistedWithResponse = null;
            var queueItemRepo = new Mock<IMiddlewareQueueItemRepository>();
            queueItemRepo.Setup(x => x.InsertOrUpdateAsync(It.IsAny<ftQueueItem>())).Callback<ftQueueItem>(queueItem => { if (!string.IsNullOrWhiteSpace(queueItem.response)) { persistedWithResponse = queueItem; } }).Returns(Task.CompletedTask);

            var receiptJournalRepo = new Mock<IMiddlewareReceiptJournalRepository>();
            var actionJournalRepo = new Mock<IMiddlewareActionJournalRepository>();

            var crypto = new Mock<ICryptoHelper>();
            crypto.Setup(x => x.GenerateBase64Hash(It.IsAny<string>()))
                 .Returns("HASH");

            var marketSpecific = new Mock<IMarketSpecificSignProcessor>();
            marketSpecific
                .Setup(x => x.ProcessAsync(request, queue, It.IsAny<ftQueueItem>()))
                .ThrowsAsync(new Exception("boom"));

            marketSpecific
                .Setup(x => x.GetFtCashBoxIdentificationAsync(queue))
                .ReturnsAsync("IDENTIFIER");

            marketSpecific
                .Setup(x => x.FirstTaskAsync())
                .Returns(Task.CompletedTask);

            marketSpecific
                .Setup(x => x.FinalTaskAsync(queue, It.IsAny<ftQueueItem>(), request, actionJournalRepo.Object, queueItemRepo.Object, receiptJournalRepo.Object))
                .Returns(Task.CompletedTask);

            var cfg = new MiddlewareConfiguration
            {
                QueueId = queueId,
                CashBoxId = cashBoxId
            };

            var processor = new SignProcessor(
                logger.Object,
                configurationRepo.Object,
                queueItemRepo.Object,
                receiptJournalRepo.Object,
                actionJournalRepo.Object,
                crypto.Object,
                marketSpecific.Object,
                cfg);

            var act = async () => await processor.ProcessAsync(request);

            await act.Should().ThrowAsync<Exception>().WithMessage("boom");

            persistedWithResponse.Should().NotBeNull("QueueItem with persisted response should exist even when V1 throws");
            var response = JsonConvert.DeserializeObject<ReceiptResponse>(persistedWithResponse!.response);
            response.Should().NotBeNull();

            ulong ftState = (ulong)response!.ftState;
            ulong expectedCountryBits = EncodeCountry_TestOnly("DE");

            (ftState & 0xFFFF000000000000UL).Should().Be(expectedCountryBits);
            (ftState & 0xFFFFFFFFUL).Should().Be(0xEEEE_EEEEUL);
        }
        
        [Fact]
        public async Task ProcessAsync_ShouldUseQueueCountryForQueueItem_WhenQueueCountryIsConfigured()
        {
            // Arrange
            var queueId = Guid.NewGuid();
            var cashboxId = Guid.NewGuid();

            var queue = new ftQueue
            {
                ftQueueId = queueId,
                ftCashBoxId = cashboxId,
                ftQueuedRow = 0,
                ftCurrentRow = 1,
                CountryCode = "DE"
            };

            var request = new ReceiptRequest
            {
                ftCashBoxID = cashboxId.ToString(),
                ftReceiptCase = 0x4154000000000000, 
                cbReceiptReference = "REF-1",
                cbTerminalID = "TERMINAL-1",
                cbReceiptMoment = DateTime.UtcNow,
                cbChargeItems = Array.Empty<ChargeItem>(),
                cbPayItems = Array.Empty<PayItem>()
            };

            var logger = new Mock<ILogger<SignProcessor>>();

            var configurationRepo = new Mock<IConfigurationRepository>();
            configurationRepo
                .Setup(x => x.GetQueueAsync(queueId))
                .ReturnsAsync(queue);

            ftQueueItem? createdQueueItem = null;
            var queueItemRepo = new Mock<IMiddlewareQueueItemRepository>();
            queueItemRepo
                .Setup(x => x.InsertOrUpdateAsync(It.IsAny<ftQueueItem>()))
                .Callback<ftQueueItem>(qi =>
                {
                    if (createdQueueItem == null)
                    {
                        createdQueueItem = qi;
                    }
                })
                .Returns(Task.CompletedTask);

            var receiptJournalRepo = new Mock<IMiddlewareReceiptJournalRepository>();
            var actionJournalRepo = new Mock<IMiddlewareActionJournalRepository>();

            var crypto = new Mock<ICryptoHelper>();
            crypto.Setup(x => x.GenerateBase64Hash(It.IsAny<string>()))
                 .Returns("HASH");

            var marketSpecific = new Mock<IMarketSpecificSignProcessor>();
            marketSpecific
                .Setup(x => x.ProcessAsync(It.IsAny<ReceiptRequest>(), It.IsAny<ftQueue>(), It.IsAny<ftQueueItem>()))
                .ReturnsAsync((new ReceiptResponse
                {
                    ftState = 0,
                    ftSignatures = Array.Empty<SignaturItem>(),
                    ftQueueID = queueId.ToString(),
                    ftQueueItemID = Guid.NewGuid().ToString(),
                    ftCashBoxIdentification = "IDENTIFIER"
                }, new List<ftActionJournal>()));

            var cfg = new MiddlewareConfiguration
            {
                QueueId = queueId,
                CashBoxId = cashboxId
            };

            var processor = new SignProcessor(
                logger.Object,
                configurationRepo.Object,
                queueItemRepo.Object,
                receiptJournalRepo.Object,
                actionJournalRepo.Object,
                crypto.Object,
                marketSpecific.Object,
                cfg);

            // Act
            var response = await processor.ProcessAsync(request);

            // Assert
            createdQueueItem.Should().NotBeNull("QueueItem should have been created");
            createdQueueItem!.country.Should().Be("DE", "queueItem.country should come from queue.CountryCode when configured");
        }

        private static (ReceiptRequest request, MiddlewareConfiguration config, Mock<IMiddlewareQueueItemRepository> repo) SetupTestEnvironment(long ftReceiptCase, long ftState)
        {
            var queueId = Guid.NewGuid();
            var cashboxId = Guid.NewGuid();

            var request = new ReceiptRequest
            {
                ftReceiptCase = ftReceiptCase,
                ftCashBoxID = cashboxId.ToString(),
                ftQueueID = queueId.ToString(),
                cbTerminalID = "MyTerminalId",
                cbReceiptReference = "MycbReceiptReference"
            };

            var responseObject = new ReceiptResponse { ftState = ftState };

            var queueItem = new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                request = JsonConvert.SerializeObject(request),
                response = JsonConvert.SerializeObject(responseObject),
                ftDoneMoment = DateTime.UtcNow,
                responseHash = "responseHash"
            };

            var repo = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByReceiptReferenceAsync(request.cbReceiptReference, request.cbTerminalID))
                .Returns(new List<ftQueueItem> { queueItem }.ToAsyncEnumerable());

            var config = new MiddlewareConfiguration
            {
                QueueId = queueId,
                CashBoxId = cashboxId,
                ProcessingVersion = "test"
            };

            return (request, config, repo);
        }

        private static SignProcessor CreateSignProcessor(
            Mock<IMiddlewareQueueItemRepository> repo,
            MiddlewareConfiguration config)
        {
            return new SignProcessor(
                Mock.Of<ILogger<SignProcessor>>(),
                Mock.Of<IConfigurationRepository>(),
                repo.Object,
                Mock.Of<IMiddlewareReceiptJournalRepository>(),
                Mock.Of<IMiddlewareActionJournalRepository>(),
                Mock.Of<ICryptoHelper>(),
                Mock.Of<IMarketSpecificSignProcessor>(),
                config);
        }
        
        private static ulong EncodeCountry_TestOnly(string? countryCode) => countryCode?.ToUpperInvariant() switch 
        {
            "DE" => 0x4445000000000000,
            "FR" => 0x4652000000000000,
            "ME" => 0x4D45000000000000,
            "IT" => 0x4954000000000000,
            "AT" => 0x4154000000000000,
            null => throw new ArgumentNullException(nameof(countryCode), "Country code cannot be null"),
            _ => throw new NotSupportedException($"Country code '{countryCode}' is not supported")
        };
    }
}
