using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Native;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.IntegrationTest
{
    public class CryptoVisionSCUTests
    {
        private const int DEFAULT_TSE_IO_TIMEOUT = 40 * 1000;
        private readonly int _tseIOTimeout = DEFAULT_TSE_IO_TIMEOUT;

        private readonly string _clientId = "POS002";

        private IDESSCD GetSutCryptoVision()
        {
            var config = new CryptoVisionConfiguration
            {
                DevicePath = "e:",
                TseIOTimeout = _tseIOTimeout
            };            
            var massStorageAdapter = new MassStorageClassTransportAdapter(Mock.Of<ILogger<MassStorageClassTransportAdapter>>(), new WindowsFileIo(), config);
            
            return new CryptoVisionSCU(config, new CryptoVisionFileProxy(massStorageAdapter), Mock.Of<ILogger<CryptoVisionSCU>>());
        }

#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task InitTse_WithTwoClients()
        {
            var fixture = new Fixture();
            var sut = GetSutCryptoVision();
            var info = await sut.GetTseInfoAsync();
            info = await PerformInitWithClient(sut, Encoding.ASCII.GetString(fixture.CreateMany<byte>(27).ToArray()));
            info = await PerformInitWithClient(sut, Encoding.ASCII.GetString(fixture.CreateMany<byte>(24).ToArray()));
        }

        private static async Task<TseInfo> PerformInitWithClient(IDESSCD sut, string clientId)
        {
            await sut.SetTseStateAsync(new TseState
            {
                CurrentState = TseStates.Initialized
            });
            var registerResponse = await sut.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = clientId
            });
            var info = await sut.GetTseInfoAsync();
            return info;
        }

#if DEBUG
        [Fact]
#endif
        public async Task StartTransactionAsync_Should_Return_Valid_Transaction_Result()
        {
            var sut = GetSutCryptoVision();

            var request = CreateStartTransactionRequest(_clientId);
            var result = await sut.StartTransactionAsync(request);

            result.Should().NotBeNull();
            result.TransactionNumber.Should().BeGreaterThan(0);
            result.SignatureData.Should().NotBeNull();
            result.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            result.ClientId.Should().Be(_clientId);
        }

#if DEBUG
        [Fact]
#endif
        public async Task StartTransactionAsync_Should_Fail_Because_SerialNumberNotRegistered()
        {
            var sut = GetSutCryptoVision();
            var serialNumber = Guid.NewGuid().ToString();
            var request = CreateStartTransactionRequest(_clientId);
            var action = new Func<Task>(async () => await sut.StartTransactionAsync(request));

            await action.Should().ThrowAsync<Exception>().WithMessage($"The client {serialNumber} is not registered.");
        }


#if DEBUG
        [Fact]
#endif
        public async Task StartTransactionAsync_Should_Register_NewClient()
        {
            var sut = GetSutCryptoVision();
            var serialNumber = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

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

#if DEBUG
        [Fact]
#endif
        public async Task UpdateTransactionAsync_Should_Not_Increment_TransactionNumber_And_Increment_SignatureCounter()
        {
            var sut = GetSutCryptoVision();

            var startRequest = CreateStartTransactionRequest(_clientId);
            var startResult = await sut.StartTransactionAsync(startRequest);

            var updateRequest = CreateUpdateTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var updateResult = await sut.UpdateTransactionAsync(updateRequest);

            updateResult.Should().NotBeNull();
            updateResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            updateResult.SignatureData.Should().NotBeNull();
            updateResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            updateResult.ClientId.Should().Be(_clientId);
            updateResult.ProcessDataBase64.Should().BeEquivalentTo(updateRequest.ProcessDataBase64);
            updateResult.ProcessType.Should().Be(updateRequest.ProcessType);
        }

#if DEBUG
        [Fact]
#endif
        public async Task FinishTransactionAsync_Should_Not_Increment_TransactionNumber()
        {
            var sut = GetSutCryptoVision();

            var startRequest = CreateStartTransactionRequest(_clientId);
            var startResult = await sut.StartTransactionAsync(startRequest);

            var finishRequest = CreateFinishTransactionRequest(startRequest.ClientId, startResult.TransactionNumber);
            var finishResult = await sut.FinishTransactionAsync(finishRequest);

            finishResult.Should().NotBeNull();
            finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            finishResult.SignatureData.Should().NotBeNull();
            finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            finishResult.ClientId.Should().Be(_clientId);
            finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
            finishResult.ProcessType.Should().Be(finishRequest.ProcessType);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task FinishTransactionAsync_Should_Succeed_EvenIf_MemoryIsLost()
        {
            var sut = GetSutCryptoVision();
            var startRequest = CreateStartTransactionRequest(_clientId);
            var startResult = await sut.StartTransactionAsync(startRequest);

            // We do simulate a restart of the SCU. If the SCU is restarted we loose all state and so this is similar 
            // as if we recreate the sut.
            var sut2 = GetSutCryptoVision();
            var finishRequest = CreateFinishTransactionRequest(startRequest.ClientId, startResult.TransactionNumber);
            var finishResult = await sut2.FinishTransactionAsync(finishRequest);

            finishResult.Should().NotBeNull();
            finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            finishResult.SignatureData.Should().NotBeNull();
            finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            finishResult.ClientId.Should().Be(_clientId);
            finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
            finishResult.ProcessType.Should().Be(finishRequest.ProcessType);
        }

#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task GetCertificates()
        {
            var clientId = "POS002";
            var sut = GetSutCryptoVision();
            await sut.SetTseStateAsync(new TseState
            {
                CurrentState = TseStates.Initialized
            });
            var registerResponse = await sut.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = clientId
            });

            for (var i = 0; i < 10; i++)
            {

                var result = await sut.StartTransactionAsync(CreateStartTransactionRequest(clientId));
                await sut.FinishTransactionAsync(CreateFinishTransactionRequest(clientId, result.TransactionNumber));
            }
        }

        private static FinishTransactionRequest CreateFinishTransactionRequest(string clientId, ulong transactionNumber)
        {
            var fixture = new Fixture();
            var finishRequest = new FinishTransactionRequest
            {
                TransactionNumber = transactionNumber,
                ClientId = clientId,
                ProcessDataBase64 = Convert.ToBase64String(fixture.CreateMany<byte>(1000).ToArray()),
                ProcessType = "Kassenbeleg-V1",
                QueueItemId = Guid.NewGuid(),
                IsRetry = false,
            };
            return finishRequest;
        }

        private static UpdateTransactionRequest CreateUpdateTransactionRequest(ulong transactionNumber, string clientId)
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

        private static StartTransactionRequest CreateStartTransactionRequest(string clientId)
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

#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task Async_Should_Return_MultipleTransactionLogs()
        {
            var sut = GetSutCryptoVision();
            var exportSession = await sut.StartExportSessionAsync(new StartExportSessionRequest { });
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
    }
}
