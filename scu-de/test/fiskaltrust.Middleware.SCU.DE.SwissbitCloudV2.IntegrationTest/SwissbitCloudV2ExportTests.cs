using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.IntegrationTest
{
    [Collection("SwissbitCloudV2SCUTests")]
    public class SwissbitCloudV2ExportTests : IClassFixture<SwissbitCloudV2Fixture>
    {
        private readonly SwissbitCloudV2Fixture _fixture;

        public SwissbitCloudV2ExportTests(SwissbitCloudV2Fixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task ExportDataAsync_Should_Return_MultipleTransactionLogs()
        {
            var sut = await _fixture.GetSut();

            var exportSession = await sut.StartExportSessionAsync(new StartExportSessionRequest
            {
                ClientId = _fixture.TestClientId
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
    }
}
