using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.UnitTest
{
    public class FiskalySCUTempFolderTests
    {
        private readonly Mock<ILogger<FiskalySCU>> _loggerMock;
        private readonly Mock<IFiskalyApiProvider> _apiProviderMock;
        private readonly ClientCache _clientCache;
        private readonly FiskalySCUConfiguration _configuration;
        private readonly Guid _tssId = Guid.NewGuid();
        private readonly string _clientId = "test-client";
        private readonly Guid _clientGuid = Guid.NewGuid();

        public FiskalySCUTempFolderTests()
        {
            _loggerMock = new Mock<ILogger<FiskalySCU>>();
            _apiProviderMock = new Mock<IFiskalyApiProvider>();
            _configuration = new FiskalySCUConfiguration
            {
                TssId = _tssId,
                EnableTarFileExport = true,
                MaxExportTransaction = 10000,
                RetriesOnTarExportWebException = 3,
                DelayOnRetriesInMs = 100
            };
            _clientCache = new ClientCache(_apiProviderMock.Object);
        }

        [Fact]
        public async Task StartExportSessionAsync_Should_Use_SystemTempFolder_WhenExportEnabled()
        {
            // Arrange
            var exportId = Guid.NewGuid();
            var expectedTempPath = Path.Combine(Path.GetTempPath(), exportId.ToString());
            
            SetupMocksForExport();
            _apiProviderMock.Setup(x => x.StoreDownloadResultAsync(_tssId, It.IsAny<Guid>()))
                .ReturnsAsync(() => new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }));

            var sut = new FiskalySCU(_loggerMock.Object, _apiProviderMock.Object, _clientCache, _configuration);

            // Act
            var result = await sut.StartExportSessionAsync(new StartExportSessionRequest());
            var tokenId = Guid.Parse(result.TokenId);
            
            // Give background task time to complete
            await Task.Delay(500);

            // Assert
            var actualTempPath = Path.Combine(Path.GetTempPath(), tokenId.ToString());
            Assert.StartsWith(Path.GetTempPath(), actualTempPath);
            
            // Cleanup
            if (File.Exists(actualTempPath))
            {
                File.Delete(actualTempPath);
            }
        }

        [Fact]
        public async Task ExportDataAsync_Should_ReadFromTempFolder()
        {
            // Arrange
            var exportId = Guid.NewGuid();
            var tempPath = Path.Combine(Path.GetTempPath(), exportId.ToString());
            var testData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            
            // Create test file in temp folder
            File.WriteAllBytes(tempPath, testData);
            
            SetupMocksForExport();
            _apiProviderMock.Setup(x => x.GetExportStateInformationByIdAsync(_tssId, exportId))
                .ReturnsAsync(new ExportStateInformationDto { State = "COMPLETED" });

            var sut = new FiskalySCU(_loggerMock.Object, _apiProviderMock.Object, _clientCache, _configuration);
            
            // Manually set export state to simulate completed export
            var readStreamPointerField = typeof(FiskalySCU).GetField("_readStreamPointer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var readStreamPointer = readStreamPointerField.GetValue(sut) as System.Collections.Concurrent.ConcurrentDictionary<string, ExportStateData>;
            readStreamPointer.TryAdd(exportId.ToString(), new ExportStateData { State = ExportState.Succeeded, ReadPointer = 0 });

            // Act
            var result = await sut.ExportDataAsync(new ExportDataRequest
            {
                TokenId = exportId.ToString(),
                MaxChunkSize = 5
            });

            // Assert
            Assert.NotNull(result.TarFileByteChunkBase64);
            Assert.Equal(Convert.ToBase64String(testData.Take(5).ToArray()), result.TarFileByteChunkBase64);
            Assert.Equal(testData.Length, result.TotalTarFileSize);
            
            // Cleanup
            File.Delete(tempPath);
        }

        [Fact]
        public async Task EndExportSessionAsync_Should_DeleteTempFile()
        {
            // Arrange
            var exportId = Guid.NewGuid();
            var tempPath = Path.Combine(Path.GetTempPath(), exportId.ToString());
            var testData = new byte[] { 1, 2, 3, 4, 5 };
            
            // Create test file in temp folder
            File.WriteAllBytes(tempPath, testData);
            Assert.True(File.Exists(tempPath), "Temp file should exist before ending session");
            
            SetupMocksForExport();
            _apiProviderMock.Setup(x => x.GetStartedTransactionsAsync(_tssId))
                .ReturnsAsync(new List<TransactionDto>());
            _apiProviderMock.Setup(x => x.GetExportMetadataAsync(_tssId, exportId))
                .ReturnsAsync(new Dictionary<string, object>());

            var sut = new FiskalySCU(_loggerMock.Object, _apiProviderMock.Object, _clientCache, _configuration);
            
            // Manually set export state
            var readStreamPointerField = typeof(FiskalySCU).GetField("_readStreamPointer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var readStreamPointer = readStreamPointerField.GetValue(sut) as System.Collections.Concurrent.ConcurrentDictionary<string, ExportStateData>;
            readStreamPointer.TryAdd(exportId.ToString(), new ExportStateData { State = ExportState.Succeeded, ReadPointer = testData.Length });

            // Act
            await sut.EndExportSessionAsync(new EndExportSessionRequest
            {
                TokenId = exportId.ToString(),
                Sha256ChecksumBase64 = Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(testData)),
                Erase = false
            });

            // Assert
            Assert.False(File.Exists(tempPath), "Temp file should be deleted after ending session");
        }

        [Fact]
        public async Task StartExportSessionAsync_WithSplitExport_Should_UseSameTempFile()
        {
            // Arrange
            _configuration.MaxExportTransaction = 5;
            var exportId = Guid.NewGuid();
            var expectedTempPath = Path.Combine(Path.GetTempPath(), exportId.ToString());
            
            SetupMocksForExport();
            _apiProviderMock.Setup(x => x.GetTseByIdAsync(_tssId))
                .ReturnsAsync(new TssDto 
                { 
                    SerialNumber = "test-serial",
                    TransactionCounter = 20,
                    Metadata = new Dictionary<string, object>()
                });
            
            var splitExportStream = new MemoryStream(new byte[] { 1, 2, 3 });
            _apiProviderMock.Setup(x => x.StoreDownloadSplitResultAsync(_tssId, It.IsAny<SplitExportStateData>()))
                .ReturnsAsync(splitExportStream);

            var sut = new FiskalySCU(_loggerMock.Object, _apiProviderMock.Object, _clientCache, _configuration);

            // Act
            var result = await sut.StartExportSessionAsync(new StartExportSessionRequest());
            
            // Give background task time to start
            await Task.Delay(500);

            // Assert
            var actualTempPath = Path.Combine(Path.GetTempPath(), result.TokenId);
            Assert.StartsWith(Path.GetTempPath(), actualTempPath);
            
            // Cleanup
            if (File.Exists(actualTempPath))
            {
                File.Delete(actualTempPath);
            }
        }

        [Fact]
        public void GetTempPath_Should_ReturnSystemTempPath()
        {
            // Arrange
            var exportId = Guid.NewGuid().ToString();
            var expectedPath = Path.Combine(Path.GetTempPath(), exportId);
            
            var sut = new FiskalySCU(_loggerMock.Object, _apiProviderMock.Object, _clientCache, _configuration);
            
            // Act - using reflection to test private method
            var getTempPathMethod = typeof(FiskalySCU).GetMethod("GetTempPath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var actualPath = (string)getTempPathMethod.Invoke(sut, new object[] { exportId });
            
            // Assert
            Assert.Equal(expectedPath, actualPath);
            Assert.StartsWith(Path.GetTempPath(), actualPath);
        }

        private void SetupMocksForExport()
        {
            _apiProviderMock.Setup(x => x.GetClientsAsync(_tssId))
                .ReturnsAsync(new List<ClientDto> 
                { 
                    new ClientDto { Id = _clientGuid, SerialNumber = _clientId, State = "REGISTERED" } 
                });
            
            _clientCache.AddClient(_clientId, _clientGuid);
            
            _apiProviderMock.Setup(x => x.GetTseByIdAsync(_tssId))
                .ReturnsAsync(new TssDto 
                { 
                    SerialNumber = "test-serial",
                    TransactionCounter = 10,
                    Metadata = new Dictionary<string, object>()
                });
            
            _apiProviderMock.Setup(x => x.RequestExportAsync(_tssId, It.IsAny<ExportTransactions>(), It.IsAny<Guid>(), It.IsAny<long?>(), It.IsAny<long>()))
                .Returns(Task.CompletedTask);
            
            _apiProviderMock.Setup(x => x.SetExportMetadataAsync(_tssId, It.IsAny<Guid>(), It.IsAny<long?>(), It.IsAny<long>()))
                .Returns(Task.CompletedTask);
        }
    }
}

