using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloud.IntegrationTest
{
    [Collection("SwissbitCloudSCUTests")]
    public class FccDownloaderTests : IClassFixture<SwissbitCloudFixture>
    {
        private readonly SwissbitCloudFixture _fixture;

        public FccDownloaderTests(SwissbitCloudFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task DownloadAsync_Should_DownloadFileDependingOnPlatform()
        {
            var sut = new DeutscheFiskalFccDownloadService(_fixture.Configuration, Mock.Of<ILogger<IFccDownloadService>>());
            await sut.DownloadFccAsync(_fixture.Configuration.FccDirectory, null);
        }
    }
}
