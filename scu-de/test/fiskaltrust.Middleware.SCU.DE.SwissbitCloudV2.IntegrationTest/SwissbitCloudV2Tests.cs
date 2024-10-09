using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Org.BouncyCastle.Asn1.Ocsp;
using Xunit;


namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.IntegrationTest
{
    [Collection("SwissbitCloudV2Tests")]
    public class SwissbitCloudV2Tests : IClassFixture<SwissbitCloudV2Fixture>
    {
        private readonly SwissbitCloudV2Fixture _testFixture;

        public SwissbitCloudV2Tests(SwissbitCloudV2Fixture testFixture)
        {
            _testFixture = testFixture;
        }
                
        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task StartTransactionAsync_Should_Return_Valid_Transaction_Result()
        {
            var sut = await _testFixture.GetSut();

            var request = CreateStartTransactionRequest(_testFixture.TestClientId.ToString());
            var result = await sut.StartTransactionAsync(request);

            result.Should().NotBeNull();
            result.TransactionNumber.Should().BeGreaterThan(0);
            result.SignatureData.Should().NotBeNull();
            result.SignatureData.SignatureBase64.Should().NotBeNull();
            result.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            result.ClientId.Should().Be(_testFixture.TestClientId.ToString());

            await sut.FinishTransactionAsync(CreateFinishTransactionRequest(result.TransactionNumber, request.ClientId));
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task StartTransactionAsync_Should_Fail_Because_ClientNotRegistered()
        {
            var sut = await _testFixture.GetSut();
            var ClientId = Guid.NewGuid().ToString();
            var request = CreateStartTransactionRequest(ClientId);
            var result = new Func<Task>(async () => await sut.StartTransactionAsync(request));

            await result.Should().ThrowAsync<Exception>().WithMessage($"The client {ClientId} is not registered.");
        }


        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task RegisterAndUnregisterClientIdAsync_Should_Return_ValidData()
        {
            var sut = await _testFixture.GetSut();

            var ClientId = Guid.NewGuid().ToString().Replace("-", "").Remove(30);
            
            var registerdClients = await sut.RegisterClientIdAsync(new RegisterClientIdRequest { ClientId = ClientId });
            registerdClients.ClientIds.Should().Contain(ClientId);

            var unregisterdClients = await sut.UnregisterClientIdAsync(new UnregisterClientIdRequest { ClientId = ClientId });

            unregisterdClients.ClientIds.Should().NotContain(ClientId);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task UpdateTransactionAsync_Should_Not_Increment_TransactionNumber_And_Increment_SignatureCounter()
        {
            var sut = await _testFixture.GetSut();

            var startRequest = CreateStartTransactionRequest(_testFixture.TestClientId.ToString());
            var startResult = await sut.StartTransactionAsync(startRequest);

            var updateRequest = CreateUpdateTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var updateResult = await sut.UpdateTransactionAsync(updateRequest);

            updateResult.Should().NotBeNull();
            updateResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            updateResult.SignatureData.Should().NotBeNull();
            updateResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            updateResult.SignatureData.SignatureBase64.Should().NotBeNull();
            updateResult.ClientId.Should().Be(_testFixture.TestClientId.ToString());
            updateResult.ProcessDataBase64.Should().BeEquivalentTo(updateRequest.ProcessDataBase64);
            updateResult.ProcessType.Should().Be(updateRequest.ProcessType);

            await sut.FinishTransactionAsync(CreateFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId));
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task UpdateTransactionAsync_Should_Fail_Because_ClientIdIsNotRegistered()
        {
            var sut = await _testFixture.GetSut();
            var clientId = Guid.NewGuid().ToString();
            var request = CreateUpdateTransactionRequest(0, clientId);
            var action = new Func<Task>(async () => await sut.UpdateTransactionAsync(request));

            await action.Should().ThrowAsync<Exception>().WithMessage($"The client {clientId} is not registered.");
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task FinishTransactionAsync_Should_Not_Increment_TransactionNumber()
        {
            var sut = await _testFixture.GetSut();

            var startRequest = CreateStartTransactionRequest(_testFixture.TestClientId.ToString());
            var startResult = await sut.StartTransactionAsync(startRequest);

            var finishRequest = CreateFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var finishResult = await sut.FinishTransactionAsync(finishRequest);

            finishResult.Should().NotBeNull();
            finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            finishResult.SignatureData.Should().NotBeNull();
            finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            finishResult.SignatureData.SignatureBase64.Should().NotBeNull();
            finishResult.ClientId.Should().Be(_testFixture.TestClientId.ToString());
            finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
            finishResult.ProcessType.Should().Be(finishRequest.ProcessType);

            var tseInfo = await sut.GetTseInfoAsync();
            startResult.TseSerialNumberOctet.Should().Be(tseInfo.SerialNumberOctet);
            finishResult.TseSerialNumberOctet.Should().Be(tseInfo.SerialNumberOctet);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task StartAndFinishTransactionAsync_Should_WorkFor_OrderWithoutContent()
        {
            var sut = await _testFixture.GetSut();

            var startRequest = CreateOrderStartTransactionRequest(_testFixture.TestClientId.ToString(), "");
            var startResult = await sut.StartTransactionAsync(startRequest);

            var finishRequest = CreateOrderFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId, "");
            var finishResult = await sut.FinishTransactionAsync(finishRequest);

            finishResult.Should().NotBeNull();
            finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            finishResult.SignatureData.Should().NotBeNull();
            finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            finishResult.SignatureData.SignatureBase64.Should().NotBeNull();
            finishResult.ClientId.Should().Be(_testFixture.TestClientId.ToString());
            finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
            finishResult.ProcessType.Should().Be(finishRequest.ProcessType);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task FinishTransactionAsync_Should_Fail_Because_ClientIdIsNotRegistered()
        {
            var sut = await _testFixture.GetSut();
            var serialNumber = Guid.NewGuid().ToString();
            var request = CreateFinishTransactionRequest(0, serialNumber);
            var action = new Func<Task>(async () => await sut.FinishTransactionAsync(request));

            await action.Should().ThrowAsync<Exception>().WithMessage($"The client {serialNumber} is not registered.");
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task FinishTransactionAsync_Should_Succeed_EvenIf_MemoryIsLost()
        {
            var sut = await _testFixture.GetSut();
            var startRequest = CreateStartTransactionRequest(_testFixture.TestClientId.ToString());
            var startResult = await sut.StartTransactionAsync(startRequest);

            // We do simulate a restart of the SCU. If the SCU is restarted we loose all state and so this is similar 
            // as if we recreate the sut.
            var sut2 = _testFixture.GetNewSut();
            var finishRequest = CreateFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var finishResult = await sut2.FinishTransactionAsync(finishRequest);

            finishResult.Should().NotBeNull();
            finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            finishResult.SignatureData.Should().NotBeNull();
            finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            finishResult.ClientId.Should().Be(_testFixture.TestClientId.ToString());
            finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
            finishResult.ProcessType.Should().Be(finishRequest.ProcessType);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task GetTseInfoAsync_Should_Return_Valid_TseInfo()
        {
            var sut = await _testFixture.GetSut();

            var result = await sut.GetTseInfoAsync().ConfigureAwait(false);

            result.Should().NotBeNull();
            result.CurrentNumberOfClients.Should().BeGreaterThan(0);
            result.SerialNumberOctet.Should().NotBeNullOrEmpty();
            result.PublicKeyBase64.Should().NotBeNullOrEmpty();
            result.MaxNumberOfClients.Should().BeGreaterOrEqualTo(result.CurrentNumberOfClients);
            result.MaxNumberOfStartedTransactions.Should().BeGreaterOrEqualTo(result.CurrentNumberOfStartedTransactions);
            result.CertificatesBase64.Should().HaveCount(1);
            result.CurrentClientIds.Should().Contain(_testFixture.TestClientId);
            result.CurrentState.Should().Be(TseStates.Initialized);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task ExportDataAsync_Should_Return_MultipleTransactionLogs()
        {
            var sut = await _testFixture.GetSut();

            var exportSession = await sut.StartExportSessionAsync(new StartExportSessionRequest
            {
                ClientId = _testFixture.TestClientId
            });
            exportSession.Should().NotBeNull();
            using (var fileStream = File.OpenWrite($"export_{exportSession.TokenId}.tar"))
            {
                ExportDataResponse export;
                do
                {
                    export = await sut.ExportDataAsync(new ExportDataRequest
                    {
                        TokenId = exportSession.TokenId,
                        MaxChunkSize = 1024 * 1024
                    });
                    if (!export.TotalTarFileSizeAvailable)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        var allBytes = Convert.FromBase64String(export.TarFileByteChunkBase64);
                        await fileStream.WriteAsync(allBytes, 0, allBytes.Length);
                    }
                } while (!export.TarFileEndOfFile);
            }

            var endSessionRequest = new EndExportSessionRequest
            {
                TokenId = exportSession.TokenId,
                Erase = true
            };
            using (var fileStream = File.OpenRead($"export_{exportSession.TokenId}.tar"))
            {
                endSessionRequest.Sha256ChecksumBase64 = Convert.ToBase64String(SHA256.Create().ComputeHash(fileStream));
            }

            var endExportSessionResult = await sut.EndExportSessionAsync(endSessionRequest);
            endExportSessionResult.IsValid.Should().BeTrue();

            using (var fileStream = File.OpenRead($"export_{exportSession.TokenId}.tar"))
            {
                var logs = LogParser.GetLogsFromTarStream(fileStream).ToList();
                logs.Should().HaveCountGreaterThan(0);
            }
        }

        private StartTransactionRequest CreateStartTransactionRequest(string clientId)
        {
            var fixture = new Fixture();
            return new StartTransactionRequest
            {
                ClientId = clientId,
                ProcessDataBase64 = Convert.ToBase64String(fixture.CreateMany<byte>(100).ToArray()),
                ProcessType = "Kassenbeleg-V1",
                QueueItemId = Guid.NewGuid(),
                IsRetry = false,
            };
        }

        private UpdateTransactionRequest CreateUpdateTransactionRequest(ulong transactionNumber, string clientId)
        {
            var fixture = new Fixture();
            return new UpdateTransactionRequest
            {
                TransactionNumber = transactionNumber,
                ClientId = clientId,
                ProcessDataBase64 = Convert.ToBase64String(fixture.CreateMany<byte>(100).ToArray()),
                ProcessType = "Kassenbeleg-V1",
                QueueItemId = Guid.NewGuid(),
                IsRetry = false,
            };
        }

        private FinishTransactionRequest CreateFinishTransactionRequest(ulong transactionNumber, string clientId)
        {
            var fixture = new Fixture();
            return new FinishTransactionRequest
            {
                TransactionNumber = transactionNumber,
                ClientId = clientId,
                ProcessDataBase64 = Convert.ToBase64String(fixture.CreateMany<byte>(100).ToArray()),
                ProcessType = "Kassenbeleg-V1",
                QueueItemId = Guid.NewGuid(),
                IsRetry = false,
            };
        }

        private StartTransactionRequest CreateOrderStartTransactionRequest(string clientId, string processDataBase64)
        {
            return new StartTransactionRequest
            {
                ClientId = clientId,
                ProcessDataBase64 = processDataBase64,
                ProcessType = "",
                QueueItemId = Guid.NewGuid(),
                IsRetry = false,
            };
        }

        private FinishTransactionRequest CreateOrderFinishTransactionRequest(ulong transactionNumber, string clientId, string processDataBase64)
        {
            return new FinishTransactionRequest
            {
                TransactionNumber = transactionNumber,
                ClientId = clientId,
                ProcessDataBase64 = processDataBase64,
                ProcessType = "Bestellung-V1",
                QueueItemId = Guid.NewGuid(),
                IsRetry = false,
            };
        }

       
    }
}
