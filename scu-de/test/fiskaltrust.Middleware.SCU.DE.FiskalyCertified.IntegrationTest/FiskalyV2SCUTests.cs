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
using Xunit;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Services;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.IntegrationTest
{
    [Collection("FiskalySCUTests")]
    public class FiskalyV2SCUTests : IClassFixture<TestFixture>
    {
        private readonly TestFixture _testFixture;

        public FiskalyV2SCUTests(TestFixture testFixture)
        {
            _testFixture = testFixture;
        }
                
        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task StartTransactionAsync_Should_Return_Valid_Transaction_Result()
        {
            var sut = GetSut();

            var request = CreateStartTransactionRequest(_testFixture.ClientId.ToString());
            var result = await sut.StartTransactionAsync(request);

            result.Should().NotBeNull();
            result.TransactionNumber.Should().BeGreaterThan(0);
            result.SignatureData.Should().NotBeNull();
            result.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            result.ClientId.Should().Be(_testFixture.ClientId.ToString());
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task StartTransactionAsync_Should_Fail_Because_SerialNumberNotRegistered()
        {
            var sut = GetSut();
            var serialNumber = Guid.NewGuid().ToString();
            var request = CreateStartTransactionRequest(serialNumber);
            var action = new Func<Task>(async () => await sut.StartTransactionAsync(request));

            await action.Should().ThrowAsync<Exception>().WithMessage($"The client {serialNumber} is not registered.");
        }


        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task StartTransactionAsync_Should_Register_NewClient()
        {
            var sut = GetSut();
            var serialNumber = Guid.NewGuid().ToString();

            var clients = await sut.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = serialNumber
            });
            clients.ClientIds.Should().Contain(serialNumber);

            var request = CreateStartTransactionRequest(serialNumber);
            var result = await sut.StartTransactionAsync(request);

            result.Should().NotBeNull();
            result.TransactionNumber.Should().BeGreaterThan(0);
            result.SignatureData.Should().NotBeNull();
            result.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            result.ClientId.Should().Be(serialNumber);
        }

        [Fact(Skip = "UpdateTransaction is a NOP in FiskalyCertified")]
        [Trait("TseCategory", "Cloud")]
        public async Task UpdateTransactionAsync_Should_Not_Increment_TransactionNumber_And_Increment_SignatureCounter()
        {
            var sut = GetSut();

            var startRequest = CreateStartTransactionRequest(_testFixture.ClientId.ToString());
            var startResult = await sut.StartTransactionAsync(startRequest);

            var updateRequest = CreateUpdateTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var updateResult = await sut.UpdateTransactionAsync(updateRequest);

            updateResult.Should().NotBeNull();
            updateResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            updateResult.SignatureData.Should().NotBeNull();
            updateResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            updateResult.ClientId.Should().Be(_testFixture.ClientId.ToString());
            updateResult.ProcessDataBase64.Should().BeEquivalentTo(updateRequest.ProcessDataBase64);
            updateResult.ProcessType.Should().Be(updateRequest.ProcessType);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task UpdateTransactionAsync_Should_Fail_Because_ClientIdIsNotRegistered()
        {
            var sut = GetSut();
            var clientId = Guid.NewGuid().ToString();
            var request = CreateUpdateTransactionRequest(0, clientId);
            var action = new Func<Task>(async () => await sut.UpdateTransactionAsync(request));

            await action.Should().ThrowAsync<Exception>().WithMessage($"The client {clientId} is not registered.");
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task FinishTransactionAsync_Should_Not_Increment_TransactionNumber()
        {
            var sut = GetSut();

            var startRequest = CreateStartTransactionRequest(_testFixture.ClientId.ToString());
            var startResult = await sut.StartTransactionAsync(startRequest);

            var finishRequest = CreateFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var finishResult = await sut.FinishTransactionAsync(finishRequest);

            finishResult.Should().NotBeNull();
            finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            finishResult.SignatureData.Should().NotBeNull();
            finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            finishResult.ClientId.Should().Be(_testFixture.ClientId.ToString());
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
            var sut = GetSut();

            var startRequest = CreateOrderStartTransactionRequest(_testFixture.ClientId.ToString(), "");
            var startResult = await sut.StartTransactionAsync(startRequest);

            var finishRequest = CreateOrderFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId, "");
            var finishResult = await sut.FinishTransactionAsync(finishRequest);

            finishResult.Should().NotBeNull();
            finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            finishResult.SignatureData.Should().NotBeNull();
            finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            finishResult.ClientId.Should().Be(_testFixture.ClientId.ToString());
            finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
            finishResult.ProcessType.Should().Be(finishRequest.ProcessType);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task FinishTransactionAsync_Should_Fail_Because_SerialNumberNotRegistered()
        {
            var sut = GetSut();
            var serialNumber = Guid.NewGuid().ToString();
            var request = CreateFinishTransactionRequest(0, serialNumber);
            var action = new Func<Task>(async () => await sut.FinishTransactionAsync(request));

            await action.Should().ThrowAsync<Exception>().WithMessage($"The client {serialNumber} is not registered.");
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task FinishTransactionAsync_Should_Succeed_EvenIf_MemoryIsLost()
        {
            var sut = GetSut();
            var startRequest = CreateStartTransactionRequest(_testFixture.ClientId.ToString());
            var startResult = await sut.StartTransactionAsync(startRequest);

            // We do simulate a restart of the SCU. If the SCU is restarted we loose all state and so this is similar 
            // as if we recreate the sut.
            var sut2 = GetSut();
            var finishRequest = CreateFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var finishResult = await sut2.FinishTransactionAsync(finishRequest);

            finishResult.Should().NotBeNull();
            finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            finishResult.SignatureData.Should().NotBeNull();
            finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            finishResult.ClientId.Should().Be(_testFixture.ClientId.ToString());
            finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
            finishResult.ProcessType.Should().Be(finishRequest.ProcessType);
        }

        [Fact(Skip = "UpdateTransaction is a NOP in FiskalyCertified")]
        [Trait("TseCategory", "Cloud")]
        public async Task UpdateTransactionAsync_Should_Fail_After_FinishTransactionAsync()
        {
            var sut = GetSut();

            var startRequest = CreateStartTransactionRequest(_testFixture.ClientId.ToString());
            var startResult = await sut.StartTransactionAsync(startRequest);

            var finishRequest = CreateFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var finishResult = await sut.FinishTransactionAsync(finishRequest);

            var updateRequest = CreateUpdateTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            Func<Task> act = async () => await sut.UpdateTransactionAsync(updateRequest);
            act.Should().Throw<Exception>();
        }

        public static string FromTsePublicKeyToSerialNumber(string publicKeyBase64)
        {
            if (string.IsNullOrEmpty(publicKeyBase64))
            {
                return null;
            }
            var publicKey = Convert.FromBase64String(publicKeyBase64);

            using (var hasher = SHA256.Create())
            {
                var hash = hasher.ComputeHash(publicKey);
                return BitConverter.ToString(hash).ToLower().Replace("-", "");
            }
        }


        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task GetTseInfoAsync_Should_Return_Valid_Data()
        {
            var sut = GetSut();

            var result = await sut.GetTseInfoAsync().ConfigureAwait(false);

            result.CurrentNumberOfClients.Should().BeGreaterThan(0);
            result.SerialNumberOctet.Should().NotBeNullOrEmpty();
            result.PublicKeyBase64.Should().NotBeNullOrEmpty();            
            result.CurrentNumberOfStartedTransactions.Should().BeGreaterOrEqualTo(0);
            result.MaxNumberOfClients.Should().BeGreaterOrEqualTo(result.CurrentNumberOfClients);
            result.MaxNumberOfStartedTransactions.Should().BeGreaterOrEqualTo(result.CurrentNumberOfStartedTransactions);
            result.CertificatesBase64.Should().HaveCount(1);

            var bytes = Convert.FromBase64String(result.CertificatesBase64.ToList().First());
            using (var cert = new X509Certificate2(bytes))
            {
                _ = cert.SerialNumber.TrimStart('0').Should().BeEquivalentTo(result.SerialNumberOctet);
            }

            using (var hasher = SHA256.Create())
            {
                var hash = hasher.ComputeHash(Convert.FromBase64String(result.PublicKeyBase64));
                var computedSerialNumber = BitConverter.ToString(hash).ToLower().Replace("-", "");
                result.SerialNumberOctet.Should().BeEquivalentTo(computedSerialNumber);
            }
        }

        [Fact]
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        [Trait("TseCategory", "Cloud")]
        public async Task Async_Should_Return_MultipleTransactionLogs()
        {
            var sut = GetSut();

            var exportSession = await sut.StartExportSessionAsync(new StartExportSessionRequest
            {

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
                        MaxChunkSize = 1024
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
                TokenId = exportSession.TokenId
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

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task SetTseStateAsync_Should_Return_Valid_Data()
        {
            var sut = GetSut();

            var tseState = new TseState
            {
                CurrentState = TseStates.Initialized
            };

            var result = await sut.SetTseStateAsync(tseState);

            result.CurrentState.Should().Be(tseState.CurrentState);
        }

        private FiskalySCU GetSut()
        {
            var apiProvider = new FiskalyV2ApiProvider(_testFixture.Configuration, Mock.Of<ILogger<FiskalyV2ApiProvider>>());
            return new FiskalySCU(Mock.Of<ILogger<FiskalySCU>>(), apiProvider, new ClientCache(apiProvider), _testFixture.Configuration);
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
