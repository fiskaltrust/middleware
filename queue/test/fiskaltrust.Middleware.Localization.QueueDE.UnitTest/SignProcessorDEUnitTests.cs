using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Helpers;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.DE;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;


namespace fiskaltrust.Middleware.Localization.QueueDE.UnitTest
{
    public class SignProcessorDEUnitTests
    {
        [Fact]
        public async Task SignProcessor_ImplicitFlow_ShouldReturnValidResponse()
        {
            var loggerMock = new Mock<ILogger<SignProcessorDE>>();
            var configurationRepository = new Mock<IConfigurationRepository>(MockBehavior.Strict);
            var desscdClient = new Mock<IDESSCD>(MockBehavior.Strict);
            var transactionPayloadFactory = new Mock<ITransactionPayloadFactory>(MockBehavior.Strict);

            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(File.ReadAllText(Path.Combine("Data", "ImplicitPosReceipt", "Request.json")));
            var expectedResponse = JsonConvert.DeserializeObject<ReceiptResponse>(File.ReadAllText(Path.Combine("Data", "ImplicitPosReceipt", "Response.json")));
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = receiptRequest.cbReceiptMoment,
                cbReceiptReference = receiptRequest.cbReceiptReference,
                cbTerminalID = receiptRequest.cbTerminalID,
                country = "DE",
                ftQueueId = Guid.Parse(receiptRequest.ftQueueID),
                ftQueueItemId = Guid.Parse(expectedResponse.ftQueueItemID),
                ftQueueRow = expectedResponse.ftQueueRow,
                request = JsonConvert.SerializeObject(receiptRequest),
                requestHash = "test request hash"
            };
            var queue = new ftQueue
            {
                ftQueueId = Guid.Parse(receiptRequest.ftQueueID),
                ftReceiptNumerator = 10,
                ftQueuedRow = 1200,
                StartMoment = DateTime.UtcNow
            };
            var queueDE = new ftQueueDE
            {
                ftSignaturCreationUnitDEId = Guid.NewGuid(),
                CashBoxIdentification = expectedResponse.ftCashBoxIdentification,
            };
            var scuDE = new ftSignaturCreationUnitDE
            {
                Url = "http://testurl.fiskaltrust.de:8080",
                ftSignaturCreationUnitDEId = queueDE.ftSignaturCreationUnitDEId.Value
            };

            var expectedPayload = receiptRequest;

            var startTransactionResponse = new StartTransactionResponse
            {
                TseSerialNumberOctet = "dc19faf6e7ab21690772be6f0ffc586eccdfeb299c17985c06b59029409a7613",
                ClientId = "955002-00",
                SignatureData = new TseSignatureData
                {
                    PublicKeyBase64 = "BHhWOeisRpPBTGQ1W4VUH95TXx2GARf8e2NYZXJoInjtGqnxJ8sZ3CQpYgjI+LYEmW5A37sLWHsyU7nSJUBemyU=",
                    SignatureAlgorithm = "ecdsa-plain-SHA256",
                    SignatureBase64 = "c+3+k0v3bycwayxb8oyB01WgMNVmZEPMH9ink2XDY3z57g/IfX2kfH9xxDYcGbc2wEF9UbCSG1DLjtJAlpENvQ==",
                    SignatureCounter = 111
                },
                TimeStamp = new DateTime(2019, 7, 10, 18, 41, 2),
                TransactionNumber = 18
            };

            var finishResultResponse = new FinishTransactionResponse
            {
                TseSerialNumberOctet = "dc19faf6e7ab21690772be6f0ffc586eccdfeb299c17985c06b59029409a7613",
                ClientId = "955002-00",
                TimeStamp = new DateTime(2019, 7, 10, 18, 41, 4),
                TseTimeStampFormat = "unixTime",
                ProcessDataBase64 = "QmVsZWdeMC4wMF8yLjU1XzAuMDBfMC4wMF8wLjAwXjIuNTU6QmFy",
                ProcessType = "Kassenbeleg-V1",
                SignatureData = new TseSignatureData
                {
                    PublicKeyBase64 = "BHhWOeisRpPBTGQ1W4VUH95TXx2GARf8e2NYZXJoInjtGqnxJ8sZ3CQpYgjI+LYEmW5A37sLWHsyU7nSJUBemyU=",
                    SignatureAlgorithm = "ecdsa-plain-SHA256",
                    SignatureBase64 = "MEQCIAy4P9k+7x9saDO0uRZ4El8QwN+qTgYiv1DIaJIMWRiuAiAt+saFDGjK2Yi5Cxgy7PprXQ5O0seRgx4ltdpW9REvwA==",
                    SignatureCounter = 112
                },
                StartTransactionTimeStamp = new DateTime(2019, 7, 10, 18, 41, 2),
                TransactionNumber = 18
            };

            configurationRepository.Setup(x => x.GetSignaturCreationUnitDEListAsync()).ReturnsAsync(new List<ftSignaturCreationUnitDE> { scuDE });
            configurationRepository.Setup(x => x.GetSignaturCreationUnitDEAsync(scuDE.ftSignaturCreationUnitDEId)).ReturnsAsync(scuDE);
            configurationRepository.Setup(x => x.GetQueueAsync(queue.ftQueueId)).ReturnsAsync(queue);
            configurationRepository.Setup(x => x.GetQueueDEAsync(queue.ftQueueId)).ReturnsAsync(queueDE);
            configurationRepository.Setup(x => x.InsertOrUpdateQueueDEAsync(queueDE)).Returns(Task.CompletedTask);

            desscdClient.Setup(x => x.GetTseInfoAsync()).ReturnsAsync(new TseInfo { CertificationIdentification = "BSI-TK-0000-0000" });
            desscdClient.Setup(x => x.StartTransactionAsync(It.Is<StartTransactionRequest>(req => req.ClientId == queueDE.CashBoxIdentification && string.IsNullOrEmpty(req.ProcessDataBase64) && string.IsNullOrEmpty(req.ProcessType)))).ReturnsAsync(startTransactionResponse);
            desscdClient.Setup(x => x.FinishTransactionAsync(It.Is<FinishTransactionRequest>(req => Match(req, queueDE, "Kassenbeleg-V1", "payload", startTransactionResponse.TransactionNumber)))).ReturnsAsync(finishResultResponse);
            var deSSCDProviderMock = new Mock<IDESSCDProvider>();
            deSSCDProviderMock.SetupGet(x => x.Instance).Returns(desscdClient.Object);

            transactionPayloadFactory.Setup(x => x.CreateReceiptPayload(It.IsAny<ReceiptRequest>())).Returns(("Kassenbeleg-V1", "payload"));

            var journalRepositoryMock = new Mock<IMiddlewareJournalDERepository>(MockBehavior.Strict);
            var actionJournalRepositoryMock = new Mock<IActionJournalRepository>(MockBehavior.Strict);
            var config = new MiddlewareConfiguration { Configuration = new Dictionary<string, object>() };
            var sut = RequestCommandFactoryHelper.ConstructSignProcessor(loggerMock.Object, configurationRepository.Object, journalRepositoryMock.Object, actionJournalRepositoryMock.Object, 
                deSSCDProviderMock.Object, transactionPayloadFactory.Object, new InMemoryFailedFinishTransactionRepository(), new InMemoryFailedStartTransactionRepository(), 
                new InMemoryOpenTransactionRepository(), Mock.Of<IMasterDataService>(), config, new InMemoryQueueItemRepository(), 
                new SignatureFactoryDE(QueueDEConfiguration.FromMiddlewareConfiguration(config)));

            var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, queue, queueItem);

            receiptResponse.Should().BeEquivalentTo(expectedResponse, x => x.Excluding(x => x.ftReceiptMoment));
        }

        [Fact]
        public async Task SignProcessor_ExplicitFlow_StartTransaction_ForPosReceipt_ShouldReturnValidResponses()
        {
            var loggerMock = new Mock<ILogger<SignProcessorDE>>();
            var configurationRepository = new Mock<IConfigurationRepository>(MockBehavior.Strict);
            var desscdClient = new Mock<IDESSCD>(MockBehavior.Strict);
            var transactionPayloadFactory = new Mock<ITransactionPayloadFactory>(MockBehavior.Strict);

            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(File.ReadAllText(Path.Combine("Data", "StartTransactionReceipt", "Request.json")));
            var expectedResponse = JsonConvert.DeserializeObject<ReceiptResponse>(File.ReadAllText(Path.Combine("Data", "StartTransactionReceipt", "Response.json")));
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = receiptRequest.cbReceiptMoment,
                cbReceiptReference = receiptRequest.cbReceiptReference,
                cbTerminalID = receiptRequest.cbTerminalID,
                country = "DE",
                ftQueueId = Guid.Parse(receiptRequest.ftQueueID),
                ftQueueItemId = Guid.Parse(expectedResponse.ftQueueItemID),
                ftQueueRow = expectedResponse.ftQueueRow,
                request = JsonConvert.SerializeObject(receiptRequest),
                requestHash = "test request hash"
            };
            var queue = new ftQueue
            {
                ftQueueId = Guid.Parse(receiptRequest.ftQueueID),
                ftReceiptNumerator = 10,
                ftQueuedRow = 1200,
                StartMoment = DateTime.UtcNow
            };
            var queueDE = new ftQueueDE
            {
                ftSignaturCreationUnitDEId = Guid.NewGuid(),
                CashBoxIdentification = expectedResponse.ftCashBoxIdentification,
            };
            var scuDE = new ftSignaturCreationUnitDE
            {
                Url = "http://testurl.fiskaltrust.de:8080",
                ftSignaturCreationUnitDEId = queueDE.ftSignaturCreationUnitDEId.Value
            };

            var expectedPayload = receiptRequest;

            var startTransactionResponse = new StartTransactionResponse
            {
                TseSerialNumberOctet = "dc19faf6e7ab21690772be6f0ffc586eccdfeb299c17985c06b59029409a7613",
                ClientId = "955002-00",
                SignatureData = new TseSignatureData
                {
                    PublicKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("BHhWOeisRpPBTGQ1W4VUH95TXx2GARf8e2NYZXJoInjtGqnx J8sZ3CQpYgjI+LYEmW5A37sLWHsyU7nSJUBemyU=")),
                    SignatureAlgorithm = "ecdsa-plain-SHA256",
                    SignatureBase64 = "c+3+k0v3bycwayxb8oyB01WgMNVmZEPMH9ink2XDY3z57g/IfX2kfH9xxDYcGbc2wEF9UbCSG1DLjtJAlpENvQ==",
                    SignatureCounter = 111
                },
                TimeStamp = new DateTime(2019, 7, 10, 18, 41, 2),
                TransactionNumber = 18
            };

            configurationRepository.Setup(x => x.GetSignaturCreationUnitDEListAsync()).ReturnsAsync(new List<ftSignaturCreationUnitDE> { scuDE });
            configurationRepository.Setup(x => x.GetSignaturCreationUnitDEAsync(scuDE.ftSignaturCreationUnitDEId)).ReturnsAsync(scuDE);
            configurationRepository.Setup(x => x.GetQueueAsync(queue.ftQueueId)).ReturnsAsync(queue);
            configurationRepository.Setup(x => x.GetQueueDEAsync(queue.ftQueueId)).ReturnsAsync(queueDE);
            configurationRepository.Setup(x => x.InsertOrUpdateQueueDEAsync(queueDE)).Returns(Task.CompletedTask);
            desscdClient.Setup(x => x.StartTransactionAsync(It.Is<StartTransactionRequest>(req => req.ClientId == queueDE.CashBoxIdentification && string.IsNullOrEmpty(req.ProcessDataBase64) && string.IsNullOrEmpty(req.ProcessType)))).ReturnsAsync(startTransactionResponse);
            var deSSCDProviderMock = new Mock<IDESSCDProvider>();
            deSSCDProviderMock.SetupGet(x => x.Instance).Returns(desscdClient.Object);

            transactionPayloadFactory.Setup(x => x.CreateReceiptPayload(It.IsAny<ReceiptRequest>())).Returns(("Kassenbeleg-V1", "payload"));

            var journalRepositoryMock = new Mock<IMiddlewareJournalDERepository>(MockBehavior.Strict);
            var actionJournalRepositoryMock = new Mock<IActionJournalRepository>(MockBehavior.Strict);
            var config = new MiddlewareConfiguration { Configuration = new Dictionary<string, object>() };
            var sut = RequestCommandFactoryHelper.ConstructSignProcessor(loggerMock.Object, configurationRepository.Object, journalRepositoryMock.Object, actionJournalRepositoryMock.Object, 
                deSSCDProviderMock.Object, transactionPayloadFactory.Object, new InMemoryFailedFinishTransactionRepository(), new InMemoryFailedStartTransactionRepository(), 
                new InMemoryOpenTransactionRepository(), Mock.Of<IMasterDataService>(), config, new InMemoryQueueItemRepository(),
                new SignatureFactoryDE(QueueDEConfiguration.FromMiddlewareConfiguration(config)));

            var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, queue, queueItem);

            receiptResponse.Should().BeEquivalentTo(expectedResponse, x => x.Excluding(x => x.ftReceiptMoment));
        }

        [Fact]
        public async Task SignProcessor_ExplicitFlow_FinishTransaction_ForPosReceipt_ShouldReturnValidResponses()
        {
            var loggerMock = new Mock<ILogger<SignProcessorDE>>();
            var configurationRepository = new Mock<IConfigurationRepository>(MockBehavior.Strict);
            var desscdClient = new Mock<IDESSCD>(MockBehavior.Strict);
            var transactionPayloadFactory = new Mock<ITransactionPayloadFactory>(MockBehavior.Strict);

            var startTransactionRequest = JsonConvert.DeserializeObject<ReceiptRequest>(File.ReadAllText(Path.Combine("Data", "StartTransactionReceipt", "Request.json")));
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(File.ReadAllText(Path.Combine("Data", "ExplicitPosReceipt", "Request.json")));
            var expectedResponse = JsonConvert.DeserializeObject<ReceiptResponse>(File.ReadAllText(Path.Combine("Data", "ExplicitPosReceipt", "Response.json")));
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = receiptRequest.cbReceiptMoment,
                cbReceiptReference = receiptRequest.cbReceiptReference,
                cbTerminalID = receiptRequest.cbTerminalID,
                country = "DE",
                ftQueueId = Guid.Parse(receiptRequest.ftQueueID),
                ftQueueItemId = Guid.Parse(expectedResponse.ftQueueItemID),
                ftQueueRow = expectedResponse.ftQueueRow,
                request = JsonConvert.SerializeObject(receiptRequest),
                requestHash = "test request hash"
            };
            var queue = new ftQueue
            {
                ftQueueId = Guid.Parse(receiptRequest.ftQueueID),
                ftReceiptNumerator = 10,
                ftQueuedRow = 1200,
                StartMoment = DateTime.UtcNow
            };
            var queueDE = new ftQueueDE
            {
                ftSignaturCreationUnitDEId = Guid.NewGuid(),
                CashBoxIdentification = expectedResponse.ftCashBoxIdentification,
            };
            var scuDE = new ftSignaturCreationUnitDE
            {
                Url = "http://testurl.fiskaltrust.de:8080",
                ftSignaturCreationUnitDEId = queueDE.ftSignaturCreationUnitDEId.Value
            };

            var expectedPayload = receiptRequest;
            var startTransactionResponse = new StartTransactionResponse
            {
                TseSerialNumberOctet = "dc19faf6e7ab21690772be6f0ffc586eccdfeb299c17985c06b59029409a7613",
                ClientId = "955002-00",
                SignatureData = new TseSignatureData
                {
                    PublicKeyBase64 = "BHhWOeisRpPBTGQ1W4VUH95TXx2GARf8e2NYZXJoInjtGqnxJ8sZ3CQpYgjI+LYEmW5A37sLWHsyU7nSJUBemyU=",
                    SignatureAlgorithm = "ecdsa-plain-SHA256",
                    SignatureBase64 = "c+3+k0v3bycwayxb8oyB01WgMNVmZEPMH9ink2XDY3z57g/IfX2kfH9xxDYcGbc2wEF9UbCSG1DLjtJAlpENvQ==",
                    SignatureCounter = 111
                },
                TimeStamp = new DateTime(2019, 7, 10, 18, 41, 2),
                TransactionNumber = 18
            };
            var finishResultResponse = new FinishTransactionResponse
            {
                TseSerialNumberOctet = "dc19faf6e7ab21690772be6f0ffc586eccdfeb299c17985c06b59029409a7613",
                ClientId = "955002-00",
                TimeStamp = new DateTime(2019, 7, 10, 18, 41, 4),
                TseTimeStampFormat = "unixTime",
                ProcessDataBase64 = "QmVsZWdeMC4wMF8yLjU1XzAuMDBfMC4wMF8wLjAwXjIuNTU6QmFy",
                ProcessType = "Kassenbeleg-V1",
                SignatureData = new TseSignatureData
                {
                    PublicKeyBase64 = "BHhWOeisRpPBTGQ1W4VUH95TXx2GARf8e2NYZXJoInjtGqnxJ8sZ3CQpYgjI+LYEmW5A37sLWHsyU7nSJUBemyU=",
                    SignatureAlgorithm = "ecdsa-plain-SHA256",
                    SignatureBase64 = "MEQCIAy4P9k+7x9saDO0uRZ4El8QwN+qTgYiv1DIaJIMWRiuAiAt+saFDGjK2Yi5Cxgy7PprXQ5O0seRgx4ltdpW9REvwA==",
                    SignatureCounter = 112
                },
                StartTransactionTimeStamp = new DateTime(2019, 7, 10, 18, 41, 2),
                TransactionNumber = 18
            };

            configurationRepository.Setup(x => x.GetSignaturCreationUnitDEListAsync()).ReturnsAsync(new List<ftSignaturCreationUnitDE> { scuDE });
            configurationRepository.Setup(x => x.GetSignaturCreationUnitDEAsync(scuDE.ftSignaturCreationUnitDEId)).ReturnsAsync(scuDE);
            configurationRepository.Setup(x => x.GetQueueAsync(queue.ftQueueId)).ReturnsAsync(queue);
            configurationRepository.Setup(x => x.GetQueueDEAsync(queue.ftQueueId)).ReturnsAsync(queueDE);
            configurationRepository.Setup(x => x.InsertOrUpdateQueueDEAsync(queueDE)).Returns(Task.CompletedTask);
            
            desscdClient.Setup(x => x.GetTseInfoAsync()).ReturnsAsync(new TseInfo { CertificationIdentification = "BSI-TK-0000-0000" });
            desscdClient.Setup(x => x.StartTransactionAsync(It.Is<StartTransactionRequest>(req => req.ClientId == queueDE.CashBoxIdentification && string.IsNullOrEmpty(req.ProcessDataBase64) && string.IsNullOrEmpty(req.ProcessType)))).ReturnsAsync(startTransactionResponse);
            desscdClient.Setup(x => x.FinishTransactionAsync(It.Is<FinishTransactionRequest>(req => Match(req, queueDE, "Kassenbeleg-V1", "payload", finishResultResponse.TransactionNumber)))).ReturnsAsync(finishResultResponse);
            var deSSCDProviderMock = new Mock<IDESSCDProvider>();
            deSSCDProviderMock.SetupGet(x => x.Instance).Returns(desscdClient.Object);

            transactionPayloadFactory.Setup(x => x.CreateReceiptPayload(It.IsAny<ReceiptRequest>())).Returns(("Kassenbeleg-V1", "payload"));

            var journalRepositoryMock = new Mock<IMiddlewareJournalDERepository>(MockBehavior.Strict);
            var actionJournalRepositoryMock = new Mock<IActionJournalRepository>(MockBehavior.Strict);
            var config = new MiddlewareConfiguration { Configuration = new Dictionary<string, object>() };
            var sut = RequestCommandFactoryHelper.ConstructSignProcessor(loggerMock.Object, configurationRepository.Object, journalRepositoryMock.Object, actionJournalRepositoryMock.Object, 
                deSSCDProviderMock.Object, transactionPayloadFactory.Object, new InMemoryFailedFinishTransactionRepository(), new InMemoryFailedStartTransactionRepository(), 
                new InMemoryOpenTransactionRepository(), Mock.Of<IMasterDataService>(), config, new InMemoryQueueItemRepository(),
                new SignatureFactoryDE(QueueDEConfiguration.FromMiddlewareConfiguration(config)));

            _ = await sut.ProcessAsync(startTransactionRequest, queue, queueItem);

            var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, queue, queueItem);

            receiptResponse.Should().BeEquivalentTo(expectedResponse, x => x.Excluding(x => x.ftReceiptMoment));
        }

        [Fact]
        public async Task SignProcessor_ImplicitOrderRequest_ShouldReturnValidResponse()
        {
            var processType = "Kassenbeleg-V1";
            var payload = "Beleg^0.00_2.55_0.00_0.00_0.00^2.55:Bar";

            var loggerMock = new Mock<ILogger<SignProcessorDE>>();
            var configurationRepository = new Mock<IConfigurationRepository>(MockBehavior.Strict);
            var desscdClient = new Mock<IDESSCD>(MockBehavior.Strict);
            var transactionPayloadFactory = new Mock<ITransactionPayloadFactory>(MockBehavior.Strict);
            transactionPayloadFactory.Setup(x => x.CreateReceiptPayload(It.IsAny<ReceiptRequest>())).Returns((processType, payload));

            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(File.ReadAllText(Path.Combine("Data", "ImplicitOrderRequest", "Request.json")));
            var expectedResponse = JsonConvert.DeserializeObject<ReceiptResponse>(File.ReadAllText(Path.Combine("Data", "ImplicitOrderRequest", "Response.json")));
            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = receiptRequest.cbReceiptMoment,
                cbReceiptReference = receiptRequest.cbReceiptReference,
                cbTerminalID = receiptRequest.cbTerminalID,
                country = "DE",
                ftQueueId = Guid.Parse(receiptRequest.ftQueueID),
                ftQueueItemId = Guid.Parse(expectedResponse.ftQueueItemID),
                ftQueueRow = expectedResponse.ftQueueRow,
                request = JsonConvert.SerializeObject(receiptRequest),
                requestHash = "test request hash"
            };
            var queue = new ftQueue
            {
                ftQueueId = Guid.Parse(receiptRequest.ftQueueID),
                ftReceiptNumerator = 10,
                ftQueuedRow = 1200,
                StartMoment = DateTime.UtcNow
            };
            var queueDE = new ftQueueDE
            {
                ftSignaturCreationUnitDEId = Guid.NewGuid(),
                CashBoxIdentification = expectedResponse.ftCashBoxIdentification,
            };
            var scuDE = new ftSignaturCreationUnitDE
            {
                Url = "http://testurl.fiskaltrust.de:8080",
                ftSignaturCreationUnitDEId = queueDE.ftSignaturCreationUnitDEId.Value
            };

            var startTransactionResponse = new StartTransactionResponse
            {
                TseSerialNumberOctet = "dc19faf6e7ab21690772be6f0ffc586eccdfeb299c17985c06b59029409a7613",
                ClientId = "955002-00",
                SignatureData = new TseSignatureData
                {
                    PublicKeyBase64 = "BHhWOeisRpPBTGQ1W4VUH95TXx2GARf8e2NYZXJoInjtGqnxJ8sZ3CQpYgjI+LYEmW5A37sLWHsyU7nSJUBemyU=",
                    SignatureAlgorithm = "ecdsa-plain-SHA256",
                    SignatureBase64 = "c+3+k0v3bycwayxb8oyB01WgMNVmZEPMH9ink2XDY3z57g/IfX2kfH9xxDYcGbc2wEF9UbCSG1DLjtJAlpENvQ==",
                    SignatureCounter = 111
                },
                TimeStamp = new DateTime(2019, 7, 10, 18, 41, 2),
                TransactionNumber = 18
            };

            var finishResultResponse = new FinishTransactionResponse
            {
                TseSerialNumberOctet = "dc19faf6e7ab21690772be6f0ffc586eccdfeb299c17985c06b59029409a7613",
                ClientId = "955002-00",
                TimeStamp = new DateTime(2019, 7, 10, 18, 41, 4),
                TseTimeStampFormat = "unixTime",
                ProcessDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload))),
                ProcessType = "Kassenbeleg-V1",
                SignatureData = new TseSignatureData
                {
                    PublicKeyBase64 = "BHhWOeisRpPBTGQ1W4VUH95TXx2GARf8e2NYZXJoInjtGqnxJ8sZ3CQpYgjI+LYEmW5A37sLWHsyU7nSJUBemyU=",
                    SignatureAlgorithm = "ecdsa-plain-SHA256",
                    SignatureBase64 = "MEQCIAy4P9k+7x9saDO0uRZ4El8QwN+qTgYiv1DIaJIMWRiuAiAt+saFDGjK2Yi5Cxgy7PprXQ5O0seRgx4ltdpW9REvwA==",
                    SignatureCounter = 112
                },
                StartTransactionTimeStamp = new DateTime(2019, 7, 10, 18, 41, 2),
                TransactionNumber = 18
            };

            configurationRepository.Setup(x => x.GetSignaturCreationUnitDEListAsync()).ReturnsAsync(new List<ftSignaturCreationUnitDE> { scuDE });
            configurationRepository.Setup(x => x.GetSignaturCreationUnitDEAsync(scuDE.ftSignaturCreationUnitDEId)).ReturnsAsync(scuDE);
            configurationRepository.Setup(x => x.GetQueueAsync(queue.ftQueueId)).ReturnsAsync(queue);
            configurationRepository.Setup(x => x.GetQueueDEAsync(queue.ftQueueId)).ReturnsAsync(queueDE);
            configurationRepository.Setup(x => x.InsertOrUpdateQueueDEAsync(queueDE)).Returns(Task.CompletedTask);

            desscdClient.Setup(x => x.GetTseInfoAsync()).ReturnsAsync(new TseInfo { CertificationIdentification = "BSI-TK-0000-0000" });
            desscdClient.Setup(x => x.StartTransactionAsync(It.Is<StartTransactionRequest>(req => req.ClientId == queueDE.CashBoxIdentification && string.IsNullOrEmpty(req.ProcessDataBase64) && string.IsNullOrEmpty(req.ProcessType)))).ReturnsAsync(startTransactionResponse);
            desscdClient.Setup(x => x.FinishTransactionAsync(It.Is<FinishTransactionRequest>(req => Match(req, queueDE, processType, payload, startTransactionResponse.TransactionNumber)))).ReturnsAsync(finishResultResponse);
            var deSSCDProviderMock = new Mock<IDESSCDProvider>();
            deSSCDProviderMock.SetupGet(x => x.Instance).Returns(desscdClient.Object);

            var journalRepositoryMock = new Mock<IMiddlewareJournalDERepository>(MockBehavior.Strict);
            var actionJournalRepositoryMock = new Mock<IActionJournalRepository>(MockBehavior.Strict);
            var config = new MiddlewareConfiguration { Configuration = new Dictionary<string, object>() };
            var sut = RequestCommandFactoryHelper.ConstructSignProcessor(loggerMock.Object, configurationRepository.Object, journalRepositoryMock.Object, actionJournalRepositoryMock.Object, 
                deSSCDProviderMock.Object, transactionPayloadFactory.Object, new InMemoryFailedFinishTransactionRepository(), new InMemoryFailedStartTransactionRepository(), 
                new InMemoryOpenTransactionRepository(), Mock.Of<IMasterDataService>(), config, new InMemoryQueueItemRepository(),
                new SignatureFactoryDE(QueueDEConfiguration.FromMiddlewareConfiguration(config)));

            var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, queue, queueItem);

            receiptResponse.Should().BeEquivalentTo(expectedResponse, x => x.Excluding(x => x.ftReceiptMoment));
        }

#pragma warning disable
        public bool Match(FinishTransactionRequest finishTransactionRequest, ftQueueDE queueDE, string expectedProcessType, string expectedPayload, ulong transactionNumber)
        {
            return finishTransactionRequest.ClientId == queueDE.CashBoxIdentification &&
                //finishTransactionRequest.ProcessDataBase64 == Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(expectedPayload))) &&
                finishTransactionRequest.ProcessType == expectedProcessType &&
                finishTransactionRequest.TransactionNumber == transactionNumber;
        }
    }
}
