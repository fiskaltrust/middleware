using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Helpers;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Interop;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace fiskaltrust.Middleware.SCU.DE.Swissbit.IntegrationTest
{
    [Collection("SwissbitSCUTests")]
    public class SwissbitSCUTests
    {
        private IDESSCD GetSutSwissbitSCU() => new SwissbitSCU(new SwissbitSCUConfiguration { DevicePath = "e:" }, new FunctionPointerFactory(), Mock.Of<ILogger<SwissbitSCU>>(), new LockingHelper(Mock.Of<ILogger<LockingHelper>>()));


#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task GetCertificates()
        {
            var sut = GetSutSwissbitSCU();
            var info = await sut.GetTseInfoAsync();
        }


#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task PerformStartAndFinishTransactionAsync()
        {
            var sut = GetSutSwissbitSCU();
            var info = await sut.GetTseInfoAsync();
            var fixture = new Fixture();
            while (true)
            {
                try
                {
                    var request = new StartTransactionRequest
                    {
                        ClientId = "POS001",
                        ProcessDataBase64 = Convert.ToBase64String(fixture.CreateMany<byte>(100).ToArray()),
                        ProcessType = "Kassenbeleg-V1",
                        QueueItemId = Guid.NewGuid(),
                        IsRetry = false,
                    };
                    var result = await sut.StartTransactionAsync(request);
                    var finishRequest = new FinishTransactionRequest
                    {
                        TransactionNumber = result.TransactionNumber,
                        ClientId = "POS001",
                        ProcessDataBase64 = Convert.ToBase64String(fixture.CreateMany<byte>(100).ToArray()),
                        ProcessType = "Kassenbeleg-V1",
                        QueueItemId = Guid.NewGuid(),
                        IsRetry = false,
                    };
                    await sut.FinishTransactionAsync(finishRequest);
                }
                catch (Exception ex)
                {

                }
            }
        }

#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task Async_Should_Return_MultipleTransactionLogs()
        {
            var sut = GetSutSwissbitSCU();
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

#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task ExportFromScu()
        {
            var sut = GetSutSwissbitSCU();

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
                TokenId = exportSession.TokenId
            };
            using (var fileStream = File.OpenRead($"export_{exportSession.TokenId}.tar"))
            {
                endSessionRequest.Sha256ChecksumBase64 = Convert.ToBase64String(SHA256.Create().ComputeHash(fileStream));
            }

            var endExportSessionResult = await sut.EndExportSessionAsync(endSessionRequest);
        }
    }
}