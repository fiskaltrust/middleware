using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Communication;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.IntegrationTest
{
    public class DieboldNixdorfSCUTests
    {
        private const string PublicKey = "04882F10AF9AA619509674469BF7190591C7D55A960A894D3225CEDF7BEF6A4F34D69742E8015BAE9A942C276595C632A71C6B5B3888B7B8AC9FBF1F7DAADD0B6B";
        private const string SerialNumber = "4A3F03A2DEC81878B432548668F603D14F7B7F90D230E30C87C1A705DCE1C890";
        private const string SignatureAlgorithm = "ecdsa-plain-SHA384";
        private const string ProcessType = "Beleg";
        private readonly ISerialCommunicationQueue _serialPortCommunicationProvider;

        public DieboldNixdorfSCUTests()
        {
            //_serialPortCommunicationProvider = new TcpCommunicationQueue(Mock.Of<ILogger<TcpCommunicationQueue>>(), "192.168.5.1", 5001);
            _serialPortCommunicationProvider = new SerialPortCommunicationQueue(Mock.Of<ILogger<SerialPortCommunicationQueue>>(), "COM3", 1500, 1500, true);
        }

#if DEBUG
        [Fact]
#endif
        public async Task GetTseInfoAsync()
        {
            var scu = GetSut();
            var result = await scu.GetTseInfoAsync();
        }

#if DEBUG
        [Fact]
#endif
        public async Task StartExportSessionAsync_ShouldExecuteFullExport()
        {
            var sut = GetSut();

            var exportSession = await sut.StartExportSessionAsync(new StartExportSessionRequest());
            exportSession.Should().NotBeNull();
            using (var fileStream = File.OpenWrite($"client_export_{exportSession.TokenId}.tar"))
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
            using (var fileStream = File.OpenRead($"client_export_{exportSession.TokenId}.tar"))
            {
                endSessionRequest.Sha256ChecksumBase64 = Convert.ToBase64String(SHA256.Create().ComputeHash(fileStream));
            }

            var endExportSessionResult = await sut.EndExportSessionAsync(endSessionRequest);
            endExportSessionResult.IsValid.Should().BeTrue();

            using (var fileStream = File.OpenRead($"client_export_{exportSession.TokenId}.tar"))
            {
                var logs = LogParser.GetLogsFromTarStream(fileStream).ToList();
                logs.Should().HaveCountGreaterThan(0);
            }
        }

#if DEBUG
        [Fact]
#endif
        public async Task StartExportSessionByTransactionAsync_ShouldExecuteRangedExport()
        {
            var sut = GetSut();

            var exportSession = await sut.StartExportSessionByTransactionAsync(new StartExportSessionByTransactionRequest
            {
                From = 1,
                To = 2
            });
            exportSession.Should().NotBeNull();
            using (var fileStream = File.OpenWrite($"client_export_{exportSession.TokenId}.tar"))
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
            using (var fileStream = File.OpenRead($"client_export_{exportSession.TokenId}.tar"))
            {
                endSessionRequest.Sha256ChecksumBase64 = Convert.ToBase64String(SHA256.Create().ComputeHash(fileStream));
            }

            var endExportSessionResult = await sut.EndExportSessionAsync(endSessionRequest);
            endExportSessionResult.IsValid.Should().BeTrue();

            using (var fileStream = File.OpenRead($"client_export_{exportSession.TokenId}.tar"))
            {
                var logs = LogParser.GetLogsFromTarStream(fileStream).ToList();
                logs.Should().HaveCountGreaterThan(0);
            }
        }

#if DEBUG
        [Fact]
#endif
        public async Task Complete_Flow_Init_Start_Update_Finish_Export()
        {
            var scu = GetSut();

            var fixture = new Fixture();
            var utcNow = DateTime.UtcNow.Ticks;
            var clientId = Guid.NewGuid().ToString().Substring(0, 30);

            await scu.SetTseStateAsync(new TseState
            {
                CurrentState = TseStates.Initialized
            });

            await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = clientId
            });

            var tseInfo = await scu.GetTseInfoAsync();
            tseInfo.CurrentState.Should().Be(TseStates.Initialized);
            var processData = fixture.CreateMany<byte>(100).ToArray();
            var startTransactionResult = await scu.StartTransactionAsync(new StartTransactionRequest
            {
                ClientId = clientId,
                ProcessType = ProcessType,
                ProcessDataBase64 = Convert.ToBase64String(processData),
            });

            startTransactionResult.TransactionNumber.Should().BeGreaterOrEqualTo(0);
            startTransactionResult.SignatureData.SignatureAlgorithm.Should().Be(SignatureAlgorithm);
            startTransactionResult.ClientId.Should().Be(clientId);

            var updateTransactionResult = await scu.UpdateTransactionAsync(new UpdateTransactionRequest
            {
                ClientId = clientId,
                ProcessType = ProcessType,
                ProcessDataBase64 = Convert.ToBase64String(processData),
                TransactionNumber = startTransactionResult.TransactionNumber
            });
            updateTransactionResult.TransactionNumber.Should().Be(startTransactionResult.TransactionNumber);
            updateTransactionResult.ProcessDataBase64.Should().Be(Convert.ToBase64String(processData));
            updateTransactionResult.SignatureData.SignatureAlgorithm.Should().Be(SignatureAlgorithm);
            updateTransactionResult.ProcessType.Should().Be(ProcessType);
            updateTransactionResult.ClientId.Should().Be(clientId);

            var finishTransactionResult = await scu.FinishTransactionAsync(new FinishTransactionRequest
            {
                ClientId = clientId,
                ProcessType = ProcessType,
                ProcessDataBase64 = Convert.ToBase64String(processData),
                TransactionNumber = startTransactionResult.TransactionNumber
            });

            finishTransactionResult.TransactionNumber.Should().Be(startTransactionResult.TransactionNumber);
            finishTransactionResult.ProcessDataBase64.Should().Be(Convert.ToBase64String(processData));
            finishTransactionResult.SignatureData.SignatureAlgorithm.Should().Be(SignatureAlgorithm);
            finishTransactionResult.ProcessType.Should().Be(ProcessType);
            finishTransactionResult.ClientId.Should().Be(clientId);
        }

#if DEBUG
        [Fact]
#endif
        public async Task Initialize_Terminate_Flow()
        {
            var scu = GetSut();
            var tseInfo = await scu.GetTseInfoAsync();
            tseInfo.CurrentState.Should().Be(TseStates.Initialized);

            var state = await scu.SetTseStateAsync(new TseState() { CurrentState = TseStates.Terminated });
            state.CurrentState.Should().Be(TseStates.Terminated);
        }

#if DEBUG
        [Fact]
#endif
        public async Task StartTransaction_FinishTransaction()
        {
            var scu = GetSut();
            var clientId = "POS001";

            await scu.SetTseStateAsync(new TseState
            {
                CurrentState = TseStates.Initialized
            });

            await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = clientId
            });

            while (true)
            {
                try
                {
                    var startTransactionRequest = new StartTransactionRequest
                    {
                        ClientId = clientId,
                        ProcessType = "KassenBeleg-V1",
                        ProcessDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("SuperCoolesZeug"))
                    };
                    var startTransactionResponse = await scu.StartTransactionAsync(startTransactionRequest);
                    startTransactionResponse.TransactionNumber.Should().BeGreaterThan(0);
                    startTransactionResponse.SignatureData.SignatureCounter.Should().BeGreaterThan(startTransactionResponse.TransactionNumber);

                    var finishTransactionRequest = new FinishTransactionRequest
                    {
                        ClientId = clientId,
                        TransactionNumber = startTransactionResponse.TransactionNumber,
                        ProcessType = "KassenBeleg-V1",
                        ProcessDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("SuperCoolesZeug"))
                    };

                    var finishTransactionResponse = await scu.FinishTransactionAsync(finishTransactionRequest);
                    startTransactionResponse.SignatureData.SignatureCounter.Should().BeGreaterThan(startTransactionResponse.SignatureData.SignatureCounter);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

#if DEBUG
        [Fact]
#endif
        public async Task StartTransaction_VeryBigRequest_ShouldWorkProperly_FinishTransaction()
        {
            var scu = GetSut();
            var clientId = "POS001";

            await scu.SetTseStateAsync(new TseState
            {
                CurrentState = TseStates.Initialized
            });

            await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = clientId
            });


            var startTransactionRequest = new StartTransactionRequest
            {
                ClientId = clientId,
                ProcessType = "KassenBeleg-V1",
                ProcessDataBase64 = Convert.ToBase64String(new Fixture().CreateMany<byte>(10000).ToArray())
            };
            var startTransactionResponse = await scu.StartTransactionAsync(startTransactionRequest);
            startTransactionResponse.TransactionNumber.Should().BeGreaterOrEqualTo(0);
            startTransactionResponse.SignatureData.SignatureCounter.Should().BeGreaterThan(startTransactionResponse.TransactionNumber);

            var updateTransactionRequest = new UpdateTransactionRequest
            {
                ClientId = clientId,
                TransactionNumber = startTransactionResponse.TransactionNumber,
                ProcessType = "KassenBeleg-V1",
                ProcessDataBase64 = Convert.ToBase64String(new Fixture().CreateMany<byte>(10000).ToArray())
            };

            var updateTransactionResponse = await scu.UpdateTransactionAsync(updateTransactionRequest);
            updateTransactionResponse.SignatureData.SignatureCounter.Should().BeGreaterThan(startTransactionResponse.SignatureData.SignatureCounter);

            var finishTransactionRequest = new FinishTransactionRequest
            {
                ClientId = clientId,
                TransactionNumber = startTransactionResponse.TransactionNumber,
                ProcessType = "KassenBeleg-V1",
                ProcessDataBase64 = Convert.ToBase64String(new Fixture().CreateMany<byte>(10000).ToArray())
            };

            var finishTransactionResponse = await scu.FinishTransactionAsync(finishTransactionRequest);
            finishTransactionResponse.SignatureData.SignatureCounter.Should().BeGreaterThan(updateTransactionResponse.SignatureData.SignatureCounter);
        }

#if DEBUG
        [Fact]
#endif
        public async Task FinishTransactionAsync_Should_Succeed_EvenIf_MemoryIsLost()
        {
            var scu = GetSut();
            var clientId = "POS001";

            await scu.SetTseStateAsync(new TseState
            {
                CurrentState = TseStates.Initialized
            });

            await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = clientId
            });

            var startTransactionRequest = new StartTransactionRequest
            {
                ClientId = clientId,
                ProcessType = "KassenBeleg-V1",
                ProcessDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("SuperCoolesZeug"))
            };
            var startTransactionResponse = await scu.StartTransactionAsync(startTransactionRequest);
            startTransactionResponse.TransactionNumber.Should().BeGreaterOrEqualTo(0);
            startTransactionResponse.SignatureData.SignatureCounter.Should().BeGreaterOrEqualTo(0);

            scu = GetSut();

            var finishTransactionRequest = new FinishTransactionRequest
            {
                ClientId = clientId,
                TransactionNumber = startTransactionResponse.TransactionNumber,
                ProcessType = "KassenBeleg-V1",
                ProcessDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("SuperCoolesZeug"))
            };
            var finishTransactionResponse = await scu.FinishTransactionAsync(finishTransactionRequest);
            finishTransactionResponse.TransactionNumber.Should().Be(startTransactionResponse.TransactionNumber);
            finishTransactionResponse.StartTransactionTimeStamp.Should().Be(startTransactionResponse.TimeStamp);
        }

        private DieboldNixdorfSCU GetSut()
        {
            var configuration = new DieboldNixdorfConfiguration
            {
                ComPort = "COM3",
                SlotNumber = 1,
                AdminUser = "1",
                AdminPin = "12345",
                TimeAdminUser = "2",
                TimeAdminPin = "12345"
            };

            var tseCommunicationCommandHelper = new TseCommunicationCommandHelper(Mock.Of<ILogger<TseCommunicationCommandHelper>>(), _serialPortCommunicationProvider, configuration.SlotNumber);
            var authenticationTseCommandProvider = new AuthenticationTseCommandProvider(Mock.Of<ILogger<AuthenticationTseCommandProvider>>(), tseCommunicationCommandHelper);
            var utilityTseCommandsProvider = new UtilityTseCommandsProvider(tseCommunicationCommandHelper);
            var standardTseCommandsProvider = new StandardTseCommandsProvider(tseCommunicationCommandHelper);
            var maintenanceTseCommandProvider = new MaintenanceTseCommandProvider(tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
            var exportTseCommandsProvider = new ExportTseCommandsProvider(tseCommunicationCommandHelper);
            var transactionTseCommandsProvider = new TransactionTseCommandsProvider(tseCommunicationCommandHelper);

            var backgroundScuTasks = new BackgroundSCUTasks(Mock.Of<ILogger<BackgroundSCUTasks>>(), tseCommunicationCommandHelper, utilityTseCommandsProvider, maintenanceTseCommandProvider, standardTseCommandsProvider, exportTseCommandsProvider);
            return new DieboldNixdorfSCU(Mock.Of<ILogger<DieboldNixdorfSCU>>(), configuration, tseCommunicationCommandHelper, backgroundScuTasks, utilityTseCommandsProvider, standardTseCommandsProvider, transactionTseCommandsProvider, exportTseCommandsProvider, maintenanceTseCommandProvider, authenticationTseCommandProvider, _serialPortCommunicationProvider);
        }
    }
}