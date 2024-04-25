using System;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.IntegrationTest
{
    [Collection("DeutscheFiskalSCUTests")]
    public class FccDownloaderTests
    {
        [Fact]
        public async Task DownloadAsync_Should_DownloadFileDependingOnPlatform()
        {
            var notExistingPath = "notExistingFolder";
            var config = new DeutscheFiskalSCUConfiguration
            {
                FccId = Guid.NewGuid().ToString(),
                FccDirectory = notExistingPath
            };
            DeleteNotExistingFolder(notExistingPath);
            var sut = new DeutscheFiskalFccDownloadService(config, Mock.Of<ILogger<DeutscheFiskalFccDownloadService>>());
            await sut.DownloadFccAsync(config.FccDirectory, null);
        }

        [Fact]
        public void IsLatestVersion_SameVersion_IsTrue()
        {
            var config = new DeutscheFiskalSCUConfiguration
            {
                FccVersion = "3.2.3"
            };
            var sut = new DeutscheFiskalFccDownloadService(config, Mock.Of<ILogger<DeutscheFiskalFccDownloadService>>());
            sut.IsLatestVersion("./", new Version(config.FccVersion)).Should().BeTrue();
        }

        [Fact]
        public void IsInstalled_NotExistingFolder_IsFalse()
        {
            var config = new DeutscheFiskalSCUConfiguration
            {
                FccVersion = "3.2.3"
            };
            var sut = new DeutscheFiskalFccDownloadService(config, Mock.Of<ILogger<DeutscheFiskalFccDownloadService>>());
            var notExistingPath = "notExistingFolder";
            DeleteNotExistingFolder(notExistingPath);
            sut.IsInstalled(notExistingPath).Should().BeFalse();
        }

        [Fact]
        public void IsLatestVersion_SmallerIniVersion_IsTrue()
        {
            var config = new DeutscheFiskalSCUConfiguration
            {
                FccVersion = "1.0.0"
            };
            var sut = new DeutscheFiskalFccDownloadService(config, Mock.Of<ILogger<DeutscheFiskalFccDownloadService>>());
            sut.IsLatestVersion("./", new Version(config.FccVersion)).Should().BeTrue();
        }

        [Fact]
        public void TestIsPathInRunFccIdent_WindowsPath_ExcpectTrue()
        {
            var sut = new DeutscheFiskalFccDownloadService(Mock.Of<DeutscheFiskalSCUConfiguration>(), Mock.Of<ILogger<DeutscheFiskalFccDownloadService>>());

            var content = File.ReadAllText(Path.Combine("lib", "run_fcc.bat"));
            sut.IsPathInRunFccIdent("C:\\ProgramData\\fiskaltrust\\FCC\\sfcc-ftde-6rdx-so0d", content, out _).Should().BeTrue();
        }

        [Fact]
        public void TestIsPathInRunFccIdent_WindowsPathEnbdingSlash_ExcpectTrue()
        {
            var sut = new DeutscheFiskalFccDownloadService(Mock.Of<DeutscheFiskalSCUConfiguration>(), Mock.Of<ILogger<DeutscheFiskalFccDownloadService>>());
            var content = File.ReadAllText(Path.Combine("lib", "run_fcc.bat"));
            sut.IsPathInRunFccIdent("C:\\ProgramData\\fiskaltrust\\FCC\\sfcc-ftde-6rdx-so0d", content, out _).Should().BeTrue();
        }

        [Fact]
        public void TestComparePathInFileContent_LinuxPath_ExcpectTrueAsync()
        {
            var sut = new DeutscheFiskalFccDownloadService(Mock.Of<DeutscheFiskalSCUConfiguration>(), Mock.Of<ILogger<DeutscheFiskalFccDownloadService>>());
            var content = "slkdjlajsdlkjalksjd -DFCC_ROOT_DIR=/usr/share/fiskaltrust/service/fiskaltrust/FCC/sfcc-ftde-et94-gqzh sldkjflwefjölawkef";
            sut.IsPathInRunFccIdent("/usr/share/fiskaltrust/service/fiskaltrust/FCC/sfcc-ftde-et94-gqzh", content, out _).Should().BeTrue();
        }

        [Fact]
        public void TestComparePathInFileContent_LinuxPathEndingSlash_ExcpectTrueAsync()
        {
            var sut = new DeutscheFiskalFccDownloadService(Mock.Of<DeutscheFiskalSCUConfiguration>(), Mock.Of<ILogger<DeutscheFiskalFccDownloadService>>());
            var content = "slkdjlajsdlkjalksjd -DFCC_ROOT_DIR=/usr/share/fiskaltrust/service/fiskaltrust/FCC/sfcc-ftde-et94-gqzh sldkjflwefjölawkef";
            sut.IsPathInRunFccIdent("/usr/share/fiskaltrust/service/fiskaltrust/FCC/sfcc-ftde-et94-gqzh/", content, out _).Should().BeTrue();
            content = "slkdjlajsdlkjalksjd -DFCC_ROOT_DIR=/usr/share/fiskaltrust/service/fiskaltrust/FCC/sfcc-ftde-et94-gqzh/ sldkjflwefjölawkef";
            sut.IsPathInRunFccIdent("/usr/share/fiskaltrust/service/fiskaltrust/FCC/sfcc-ftde-et94-gqzh", content, out _).Should().BeTrue();

        }

        private void DeleteNotExistingFolder(string notExistingPath)
        {
            if (Directory.Exists(notExistingPath))
            {
                Directory.Delete(notExistingPath, true);
            }
        }
    }
}
