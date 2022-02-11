using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Epson.Exceptions;
using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using Moq;
using fiskaltrust.Middleware.SCU.DE.Epson.Commands;
using fiskaltrust.Middleware.SCU.DE.Epson.Communication;
using AutoFixture;
using System.Security.Cryptography.X509Certificates;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs.Models;

namespace fiskaltrust.Middleware.SCU.DE.Epson.IntegrationTest
{
    public class EpsonSCUTests : IDisposable
    {
        private const string _clientId = "POS5SHOP25";

        private readonly Dictionary<string, object> config = new Dictionary<string, object>
            {
                { "tseurl", "192.168.0.31" },
                { "tseport", 8010 },
                { "deviceid", "local_TSE" },
                { "timeout", 60000 }
            };

        private readonly EpsonConfiguration _configuration;
        private readonly TcpCommunicationQueue _tcpCommunicationQueue;
        private readonly EpsonSCU _epsonSCU;

        public EpsonSCUTests()
        {
            _configuration = new EpsonConfiguration()
            {
                Host = config["tseurl"].ToString(),
                Port = int.Parse(config["tseport"].ToString(), CultureInfo.InvariantCulture),
                DeviceId = config["deviceid"].ToString(),
                Timeout = int.Parse(config["timeout"].ToString(), CultureInfo.InvariantCulture)
            };
            _tcpCommunicationQueue = new TcpCommunicationQueue(Mock.Of<ILogger<TcpCommunicationQueue>>(), _configuration);
            _epsonSCU = new EpsonSCU(Mock.Of<ILogger<EpsonSCU>>(), _configuration, new OperationalCommandProvider(_tcpCommunicationQueue, _configuration));
        }

        private EpsonSCU GetSut() => _epsonSCU;

#if DEBUG
        [Fact]
#endif
        public async Task FullTest()
        {
            var sut = GetSut();

            await sut.SetTseStateAsync(new TseState
            {
                CurrentState = TseStates.Initialized
            });

            await sut.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = _clientId
            });

            while (true)
            {
                try
                {
                    var startRequest = CreateOrderStartTransactionRequest(_clientId, "");
                    var startResult = await sut.StartTransactionAsync(startRequest);

                    var finishRequest = CreateOrderFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId, "");
                    var finishResult = await sut.FinishTransactionAsync(finishRequest);

                    finishResult.Should().NotBeNull();
                    finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
                    finishResult.SignatureData.Should().NotBeNull();
                    finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
                    finishResult.ClientId.Should().Be(_clientId);
                    finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
                    finishResult.ProcessType.Should().Be(finishRequest.ProcessType);
                }
                catch (Exception ex)
                {

                }
            }
        }


#if DEBUG
        [Fact]
#endif
        public async Task StartTransactionAsync_Should_Return_Valid_Transaction_Result()
        {
            var sut = GetSut();

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
            var sut = GetSut();
            var serialNumber = Guid.NewGuid().ToString().Substring(0, 30);
            var request = CreateStartTransactionRequest(serialNumber);
            var action = new Func<Task>(async () => await sut.StartTransactionAsync(request));

            await action.Should().ThrowAsync<Exception>().WithMessage($"TSE1_ERROR_CLIENT_NOT_REGISTERED");
        }

#if DEBUG
        [Fact]
#endif
        public async Task StartTransactionAsync_Should_Register_NewClient()
        {
            var sut = GetSut();
            var serialNumber = Guid.NewGuid().ToString().Substring(0, 30);

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
            var sut = GetSut();

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
        public async Task UpdateTransactionAsync_Should_Fail_Because_SerialNumberNotRegistered()
        {
            var sut = GetSut();
            var serialNumber = Guid.NewGuid().ToString().Substring(0, 30);
            var request = CreateUpdateTransactionRequest(0, serialNumber);
            var action = new Func<Task>(async () => await sut.UpdateTransactionAsync(request));

            await action.Should().ThrowAsync<Exception>().WithMessage($"TSE1_ERROR_CLIENT_NOT_REGISTERED");
        }

#if DEBUG
        [Fact]
#endif
        public async Task FinishTransactionAsync_Should_Not_Increment_TransactionNumber()
        {
            var sut = GetSut();

            var startRequest = CreateStartTransactionRequest(_clientId);
            var startResult = await sut.StartTransactionAsync(startRequest);

            var finishRequest = CreateFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var finishResult = await sut.FinishTransactionAsync(finishRequest);

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
        public async Task StartAndFinishTransactionAsync_Should_WorkFor_OrderWithoutContent()
        {
            var sut = GetSut();

            var startRequest = CreateOrderStartTransactionRequest(_clientId, "");
            var startResult = await sut.StartTransactionAsync(startRequest);

            var finishRequest = CreateOrderFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId, "");
            var finishResult = await sut.FinishTransactionAsync(finishRequest);

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
        public async Task FinishTransactionAsync_Should_Fail_Because_SerialNumberNotRegistered()
        {
            var sut = GetSut();
            var serialNumber = Guid.NewGuid().ToString().Substring(0, 30);
            var request = CreateFinishTransactionRequest(0, serialNumber);
            var action = new Func<Task>(async () => await sut.FinishTransactionAsync(request));

            await action.Should().ThrowAsync<Exception>().WithMessage($"TSE1_ERROR_CLIENT_NOT_REGISTERED");
        }

#if DEBUG
        [Fact]
#endif
        public async Task FinishTransactionAsync_Should_Succeed_EvenIf_MemoryIsLost()
        {
            var sut = GetSut();
            var startRequest = CreateStartTransactionRequest(_clientId);
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
            finishResult.ClientId.Should().Be(_clientId);
            finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
            finishResult.ProcessType.Should().Be(finishRequest.ProcessType);
        }

#if DEBUG
        [Fact]
#endif
        public async Task UpdateTransactionAsync_Should_Fail_After_FinishTransactionAsync()
        {
            var sut = GetSut();

            var startRequest = CreateStartTransactionRequest(_clientId);
            var startResult = await sut.StartTransactionAsync(startRequest);

            var finishRequest = CreateFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var finishResult = await sut.FinishTransactionAsync(finishRequest);

            var updateRequest = CreateUpdateTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            Func<Task> act = async () => await sut.UpdateTransactionAsync(updateRequest);
            act.Should().Throw<Exception>();
        }

#if DEBUG
        [Fact]
#endif
        public async Task GetTseInfoAsync_Should_Return_Valid_Data()
        {
            var sut = GetSut();

            var result = await sut.GetTseInfoAsync().ConfigureAwait(false);

            result.CurrentNumberOfClients.Should().BeGreaterThan(0);
            result.SerialNumberOctet.Should().NotBeNullOrEmpty();
            result.PublicKeyBase64.Should().NotBeNullOrEmpty();
            result.CurrentNumberOfStartedTransactions.Should().BeGreaterThan(0);
            result.MaxNumberOfClients.Should().BeGreaterOrEqualTo(result.CurrentNumberOfClients);
            result.MaxNumberOfStartedTransactions.Should().BeGreaterOrEqualTo(result.CurrentNumberOfStartedTransactions);
            result.CertificatesBase64.Should().HaveCount(1);

            var bytes = Convert.FromBase64String(result.CertificatesBase64.ToList().First());
            using (var cert = new X509Certificate2(bytes))
            {
                var publicKey = BitConverter.ToString(Convert.FromBase64String(result.PublicKeyBase64));
                var certPublicKey = BitConverter.ToString(cert.GetPublicKey());

                var serialNumber = BitConverter.ToString(SHA256.Create().ComputeHash(cert.GetPublicKey()));
                serialNumber.Replace("-", "").Should().Be(result.SerialNumberOctet);
            }
        }

#if DEBUG
        [Fact]
#endif
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

#if DEBUG
        [Fact]
#endif
        public async Task StartExportSessionByTransactionAsync_Should_Return_MultipleTransactionLogs()
        {
            var sut = GetSut();

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

#if DEBUG
        [Fact]
#endif
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

        public void Dispose()
        {
            _epsonSCU.Dispose();
            _tcpCommunicationQueue.Dispose();
        }
    }
}
