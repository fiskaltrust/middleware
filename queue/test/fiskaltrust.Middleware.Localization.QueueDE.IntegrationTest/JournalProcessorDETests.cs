using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Queue;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest
{
    public class JournalProcessorDETests
    {
        [Fact]
        public async Task ProcessAsync_ShouldStreamByte_WhenTarFileIsRequested()
        {
            var chunk = Guid.NewGuid();
            var chunkBytes = chunk.ToByteArray();
            var exportTokenId = Guid.NewGuid().ToString();
            var sha256CheckSum = Convert.ToBase64String(SHA256.Create().ComputeHash(chunkBytes));
            var desscdMock = new Mock<IDESSCD>(MockBehavior.Strict);
            desscdMock.Setup(x => x.StartExportSessionAsync(It.IsAny<StartExportSessionRequest>())).ReturnsAsync(new StartExportSessionResponse { TokenId = exportTokenId });
            desscdMock.Setup(x => x.ExportDataAsync(It.Is<ExportDataRequest>(y => y.TokenId == exportTokenId))).ReturnsAsync(new ExportDataResponse
            {
                TarFileByteChunkBase64 = Convert.ToBase64String(chunkBytes),
                TokenId = exportTokenId,
                TarFileEndOfFile = true,
                TotalTarFileSize = chunk.ToByteArray().Length,
                TotalTarFileSizeAvailable = true
            });
            desscdMock.Setup(x => x.EndExportSessionAsync(It.Is<EndExportSessionRequest>(y => y.TokenId == exportTokenId && y.Sha256ChecksumBase64 == sha256CheckSum))).ReturnsAsync(new EndExportSessionResponse
            {
                IsValid = true,
                TokenId = exportTokenId
            });
            var deSSCDProviderMock = new Mock<IDESSCDProvider>();
            deSSCDProviderMock.SetupGet(x => x.Instance).Returns(desscdMock.Object);

            var configurationRepositoryMock = new Mock<IReadOnlyConfigurationRepository>(MockBehavior.Strict);

            var mwConfiguration = new MiddlewareConfiguration
            {
                Configuration = new Dictionary<string, object>()
            };

            var sut = new JournalProcessorDE(Mock.Of<ILogger<JournalProcessorDE>>(), configurationRepositoryMock.Object, null, null, null, null, null, null, deSSCDProviderMock.Object, mwConfiguration, Mock.Of<IMasterDataService>(), null);

            var chunks = await sut.ProcessAsync(new JournalRequest
            {
                ftJournalType = 0x4445000000000001
            }).ToListAsync();

            var resultBytes = chunks.SelectMany(x => x.Chunk);
            resultBytes.Should().BeEquivalentTo(chunkBytes);
        }
    }
}
