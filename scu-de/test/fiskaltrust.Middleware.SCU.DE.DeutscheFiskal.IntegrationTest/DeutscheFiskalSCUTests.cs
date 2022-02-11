using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs.Models;
using FluentAssertions;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.IntegrationTest
{
    [Collection("DeutscheFiskalSCUTests")]
    public class DeutscheFiskalSCUTests : IClassFixture<DeutscheFiskalFixture>
    {
        private readonly DeutscheFiskalFixture _fixture;

        public DeutscheFiskalSCUTests(DeutscheFiskalFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task RegisterClient_Should_ReturnRegisteredClient()
        {
            var sut = _fixture.GetSut();

            var request = new RegisterClientIdRequest
            {
                ClientId = _fixture.TestClientId
            };
            var result = await sut.RegisterClientIdAsync(request);

            result.Should().NotBeNull();
            result.ClientIds.Should().Contain(_fixture.TestClientId);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task StartTransactionAsync_Should_Return_Valid_Transaction_Result()
        {
            var sut = _fixture.GetSut();

            var request = CreateStartTransactionRequest(_fixture.TestClientId);
            var result = await sut.StartTransactionAsync(request);

            result.Should().NotBeNull();
            result.TransactionNumber.Should().BeGreaterThan(0);
            result.SignatureData.Should().NotBeNull();
            result.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            result.ClientId.Should().Be(_fixture.TestClientId);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task StartTransactionAsync_Should_Fail_Because_SerialNumberNotRegistered()
        {
            var sut = _fixture.GetSut();
            var serialNumber = Guid.NewGuid().ToString();
            var request = CreateStartTransactionRequest(serialNumber);
            var action = new Func<Task>(async () => await sut.StartTransactionAsync(request));

            await action.Should().ThrowAsync<Exception>().WithMessage($"The client {serialNumber} is not registered.");
        }


        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task StartTransactionAsync_Should_Register_NewClient()
        {
            var sut = _fixture.GetSut();
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

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task UpdateTransactionAsync_Should_Not_Increment_TransactionNumber_And_Increment_SignatureCounter()
        {
            var sut = _fixture.GetSut();

            var startRequest = CreateStartTransactionRequest(_fixture.TestClientId);
            var startResult = await sut.StartTransactionAsync(startRequest);

            var updateRequest = CreateUpdateTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var updateResult = await sut.UpdateTransactionAsync(updateRequest);

            updateResult.Should().NotBeNull();
            updateResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            updateResult.SignatureData.Should().NotBeNull();
            updateResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            updateResult.ClientId.Should().Be(_fixture.TestClientId);
            updateResult.ProcessDataBase64.Should().BeEquivalentTo(updateRequest.ProcessDataBase64);
            updateResult.ProcessType.Should().Be(updateRequest.ProcessType);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task UpdateTransactionAsync_Should_Fail_Because_SerialNumberNotRegistered()
        {
            var sut = _fixture.GetSut();
            var serialNumber = Guid.NewGuid().ToString();
            var request = CreateUpdateTransactionRequest(0, serialNumber);
            var action = new Func<Task>(async () => await sut.UpdateTransactionAsync(request));

            await action.Should().ThrowAsync<Exception>().WithMessage($"The client {serialNumber} is not registered.");
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task FinishTransactionAsync_Should_Not_Increment_TransactionNumber()
        {
            var sut = _fixture.GetSut();

            var startRequest = CreateStartTransactionRequest(_fixture.TestClientId);
            var startResult = await sut.StartTransactionAsync(startRequest);

            var finishRequest = CreateFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var finishResult = await sut.FinishTransactionAsync(finishRequest);

            finishResult.Should().NotBeNull();
            finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            finishResult.SignatureData.Should().NotBeNull();
            finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            finishResult.ClientId.Should().Be(_fixture.TestClientId);
            finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
            finishResult.ProcessType.Should().Be(finishRequest.ProcessType);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task StartAndFinishTransactionAsync_Should_WorkFor_OrderWithoutContent()
        {
            var sut = _fixture.GetSut();

            var startRequest = CreateOrderStartTransactionRequest(_fixture.TestClientId, "");
            var startResult = await sut.StartTransactionAsync(startRequest);

            var finishRequest = CreateOrderFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId, null);
            var finishResult = await sut.FinishTransactionAsync(finishRequest);

            finishResult.Should().NotBeNull();
            finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            finishResult.SignatureData.Should().NotBeNull();
            finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            finishResult.ClientId.Should().Be(_fixture.TestClientId);
            finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
            finishResult.ProcessType.Should().Be(finishRequest.ProcessType);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task FinishTransactionAsync_Should_Fail_Because_SerialNumberNotRegistered()
        {
            var sut = _fixture.GetSut();
            var serialNumber = Guid.NewGuid().ToString();
            var request = CreateFinishTransactionRequest(0, serialNumber);
            var action = new Func<Task>(async () => await sut.FinishTransactionAsync(request));

            await action.Should().ThrowAsync<Exception>().WithMessage($"The client {serialNumber} is not registered.");
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task FinishTransactionAsync_Should_Succeed_EvenIf_MemoryIsLost()
        {
            var sut = _fixture.GetSut();
            var startRequest = CreateStartTransactionRequest(_fixture.TestClientId);
            var startResult = await sut.StartTransactionAsync(startRequest);

            // We do simulate a restart of the SCU. If the SCU is restarted we loose all state and so this is similar 
            // as if we recreate the sut.
            var sut2 = _fixture.GetSut();
            var finishRequest = CreateFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var finishResult = await sut2.FinishTransactionAsync(finishRequest);

            finishResult.Should().NotBeNull();
            finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            finishResult.SignatureData.Should().NotBeNull();
            finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            finishResult.ClientId.Should().Be(_fixture.TestClientId);
            finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
            finishResult.ProcessType.Should().Be(finishRequest.ProcessType);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task UpdateTransactionAsync_Should_Fail_After_FinishTransactionAsync()
        {
            var sut = _fixture.GetSut();

            var startRequest = CreateStartTransactionRequest(_fixture.TestClientId);
            var startResult = await sut.StartTransactionAsync(startRequest);

            var finishRequest = CreateFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var finishResult = await sut.FinishTransactionAsync(finishRequest);

            var updateRequest = CreateUpdateTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            Func<Task> act = async () => await sut.UpdateTransactionAsync(updateRequest);
            act.Should().Throw<Exception>();
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task GetTseInfoAsync_Should_Return_Valid_Data()
        {
            var sut = _fixture.GetSut();

            var result = await sut.GetTseInfoAsync().ConfigureAwait(false);

            result.CurrentNumberOfClients.Should().BeGreaterThan(0);
            result.CurrentNumberOfStartedTransactions.Should().BeGreaterThan(0);
            result.MaxNumberOfClients.Should().BeGreaterOrEqualTo(result.CurrentNumberOfClients);
            result.MaxNumberOfStartedTransactions.Should().BeGreaterOrEqualTo(result.CurrentNumberOfStartedTransactions);
            result.CertificatesBase64.Should().HaveCount(1);
        }

        [Fact]
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        [Trait("TseCategory", "Cloud")]
        public async Task ExportDataAsync_Should_Return_MultipleTransactionLogs()
        {
            var sut = _fixture.GetSut();

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

        [Fact]
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        [Trait("TseCategory", "Cloud")]
        public async Task StartExportSessionByTransactionAsync_Should_Return_MultipleTransactionLogs()
        {
            var sut = _fixture.GetSut();

            var exportSession = await sut.StartExportSessionByTransactionAsync(new StartExportSessionByTransactionRequest
            {
                From = 1,
                To = 2
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
                var transactionNumbers = logs.Select(x => (TransactionLogMessage) x).Select(x => x.TransactionNumber).Distinct();
                transactionNumbers.Should().BeEquivalentTo(new List<int> { 1, 2 });
            }
        }

        [Fact]
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        [Trait("TseCategory", "Cloud")]
        public async Task StartExportSessionByTimeStampAsync_Should_Return_MultipleTransactionLogs()
        {
            var sut = _fixture.GetSut();

            var exportSession = await sut.StartExportSessionByTimeStampAsync(new StartExportSessionByTimeStampRequest
            {
                From = new DateTime(2020, 5, 10, 9, 49, 23),
                To = new DateTime(2020, 5, 20, 9, 49, 26)
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

                var transactionNumbers = logs.Select(x => (TransactionLogMessage) x).Select(x => x.TransactionNumber).Distinct();

                transactionNumbers.Should().BeEquivalentTo(new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            }
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task SetTseStateAsync_Should_Return_Valid_Data()
        {
            var sut = _fixture.GetSut();

            var tseState = new TseState
            {
                CurrentState = TseStates.Initialized
            };

            var result = await sut.SetTseStateAsync(tseState);

            result.CurrentState.Should().Be(tseState.CurrentState);
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
                ProcessType = "Kassenbeleg-V1",
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
