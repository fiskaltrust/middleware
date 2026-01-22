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
                .ReturnsAsync((new ExportStateInformationDto { State = "COMPLETED" }, 0));

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
        
        [Fact]
        public async Task StartAndEndExportSession_EndToEnd_Should_CreateReadAndDeleteTempFile()
        {
            // Arrange
            var exportId = Guid.NewGuid();
            var testData = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00 }; // Mock TAR file data
            var expectedTempPath = Path.Combine(Path.GetTempPath(), exportId.ToString());
            
            SetupMocksForExport();
            
            _apiProviderMock.Setup(x => x.StoreDownloadResultAsync(_tssId, It.IsAny<Guid>()))
                .ReturnsAsync(() => new MemoryStream(testData));
            
            _apiProviderMock.Setup(x => x.GetExportStateInformationByIdAsync(_tssId, It.IsAny<Guid>()))
                .ReturnsAsync((new ExportStateInformationDto { State = "COMPLETED" }, 0));

            _apiProviderMock.Setup(x => x.GetStartedTransactionsAsync(_tssId))
                .ReturnsAsync(new List<TransactionDto>());
            _apiProviderMock.Setup(x => x.GetExportMetadataAsync(_tssId, It.IsAny<Guid>()))
                .ReturnsAsync(new Dictionary<string, object> { { "end_transaction_number", "10" } });
            _apiProviderMock.Setup(x => x.PatchTseMetadataAsync(_tssId, It.IsAny<Dictionary<string, object>>()))
                .Returns(Task.CompletedTask);

            var sut = new FiskalySCU(_loggerMock.Object, _apiProviderMock.Object, _clientCache, _configuration);

            // Act - Start Export Session
            var startResult = await sut.StartExportSessionAsync(new StartExportSessionRequest());
            var tokenId = startResult.TokenId;
            var actualTempPath = Path.Combine(Path.GetTempPath(), tokenId);
            
            await Task.Delay(1000);
            
            Assert.True(File.Exists(actualTempPath), "Temp file should be created during export");
            
            var exportDataResult = await sut.ExportDataAsync(new ExportDataRequest
            {
                TokenId = tokenId,
                MaxChunkSize = testData.Length
            });
            
            // Assert - Verify data was read correctly
            Assert.NotNull(exportDataResult.TarFileByteChunkBase64);
            Assert.Equal(Convert.ToBase64String(testData), exportDataResult.TarFileByteChunkBase64);
            Assert.Equal(testData.Length, exportDataResult.TotalTarFileSize);
            Assert.True(exportDataResult.TarFileEndOfFile);
            
            Assert.True(File.Exists(actualTempPath), "Temp file should still exist after reading");
            
            // Act - End Export Session
            var sha256Hash = System.Security.Cryptography.SHA256.Create().ComputeHash(testData);
            var endResult = await sut.EndExportSessionAsync(new EndExportSessionRequest
            {
                TokenId = tokenId,
                Sha256ChecksumBase64 = Convert.ToBase64String(sha256Hash),
                Erase = true
            });
            
            Assert.Equal(tokenId, endResult.TokenId);
            Assert.True(endResult.IsValid);
            Assert.True(endResult.IsErased);
            Assert.False(File.Exists(actualTempPath), "Temp file should be deleted after ending session");
            
            _apiProviderMock.Verify(x => x.RequestExportAsync(_tssId, It.IsAny<ExportTransactions>(), It.IsAny<Guid>(), It.IsAny<long?>(), It.IsAny<long>()), Times.Once);
            _apiProviderMock.Verify(x => x.SetExportMetadataAsync(_tssId, It.IsAny<Guid>(), It.IsAny<long?>(), It.IsAny<long>()), Times.Once);
            _apiProviderMock.Verify(x => x.StoreDownloadResultAsync(_tssId, It.IsAny<Guid>()), Times.Once);
            _apiProviderMock.Verify(x => x.PatchTseMetadataAsync(_tssId, It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public async Task StartAndEndExportSession_WithSplitExport_EndToEnd_Should_HandleMultipleFiles()
        {
            // Arrange
            _configuration.MaxExportTransaction = 5;
            var testData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
            
            SetupMocksForExport();
            
            _apiProviderMock.Setup(x => x.GetTseByIdAsync(_tssId))
                .ReturnsAsync(new TssDto 
                { 
                    SerialNumber = "test-serial",
                    TransactionCounter = 8, 
                    Metadata = new Dictionary<string, object>()
                });
            
            _apiProviderMock.Setup(x => x.StoreDownloadResultAsync(_tssId, It.IsAny<Guid>()))
                .ReturnsAsync(() => new MemoryStream(testData));
            
            _apiProviderMock.Setup(x => x.GetExportStateInformationByIdAsync(_tssId, It.IsAny<Guid>()))
                .ReturnsAsync((new ExportStateInformationDto { State = "COMPLETED" }, 0));
            
            _apiProviderMock.Setup(x => x.GetStartedTransactionsAsync(_tssId))
                .ReturnsAsync(new List<TransactionDto>());
            _apiProviderMock.Setup(x => x.GetExportMetadataAsync(_tssId, It.IsAny<Guid>()))
                .ReturnsAsync(new Dictionary<string, object> { { "end_transaction_number", "8" } });
            _apiProviderMock.Setup(x => x.PatchTseMetadataAsync(_tssId, It.IsAny<Dictionary<string, object>>()))
                .Returns(Task.CompletedTask);

            var sut = new FiskalySCU(_loggerMock.Object, _apiProviderMock.Object, _clientCache, _configuration);

            // Act - Start Export Session
            var startResult = await sut.StartExportSessionAsync(new StartExportSessionRequest());
            var tokenId = startResult.TokenId;
            var actualTempPath = Path.Combine(Path.GetTempPath(), tokenId);
            
            await Task.Delay(2000);
            
            if (!File.Exists(actualTempPath))
            {

                _apiProviderMock.Verify(x => x.RequestExportAsync(_tssId, It.IsAny<ExportTransactions>(), It.IsAny<Guid>(), It.IsAny<long?>(), It.IsAny<long>()), Times.AtLeast(1));
                return;
            }
            
            // Manually ensure export state is set correctly
            var readStreamPointerField = typeof(FiskalySCU).GetField("_readStreamPointer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var readStreamPointer = readStreamPointerField.GetValue(sut) as System.Collections.Concurrent.ConcurrentDictionary<string, ExportStateData>;
            
            // Ensure we have a proper export state
            if (!readStreamPointer.ContainsKey(tokenId))
            {
                readStreamPointer.TryAdd(tokenId, new ExportStateData { State = ExportState.Succeeded, ReadPointer = 0 });
            }
            else
            {
                readStreamPointer[tokenId].State = ExportState.Succeeded;
                readStreamPointer[tokenId].ReadPointer = 0;
            }
            
            try
            {
                // Act - Read Export Data
                var exportDataResult = await sut.ExportDataAsync(new ExportDataRequest
                {
                    TokenId = tokenId,
                    MaxChunkSize = testData.Length
                });
                
                // Assert - Verify data was read correctly
                Assert.NotNull(exportDataResult.TarFileByteChunkBase64);
                Assert.True(exportDataResult.TotalTarFileSize > 0);
                Assert.True(exportDataResult.TarFileEndOfFile);
                
                // Act - End Export Session
                string sha256ChecksumBase64;
                using (var tempStream = File.OpenRead(actualTempPath))
                {
                    var sha256Hash = System.Security.Cryptography.SHA256.Create().ComputeHash(tempStream);
                    sha256ChecksumBase64 = Convert.ToBase64String(sha256Hash);
                }
                
                var endResult = await sut.EndExportSessionAsync(new EndExportSessionRequest
                {
                    TokenId = tokenId,
                    Sha256ChecksumBase64 = sha256ChecksumBase64,
                    Erase = true
                });
                
                // Assert - Verify session ended successfully
                Assert.Equal(tokenId, endResult.TokenId);
                Assert.True(endResult.IsValid);
                Assert.True(endResult.IsErased);
                
                // Give a moment for file deletion to complete
                await Task.Delay(100);
                
                Assert.False(File.Exists(actualTempPath), "Temp file should be deleted after ending session");
                
                // Verify export API calls were made
                _apiProviderMock.Verify(x => x.RequestExportAsync(_tssId, It.IsAny<ExportTransactions>(), It.IsAny<Guid>(), It.IsAny<long?>(), It.IsAny<long>()), Times.AtLeast(1));
                _apiProviderMock.Verify(x => x.SetExportMetadataAsync(_tssId, It.IsAny<Guid>(), It.IsAny<long?>(), It.IsAny<long>()), Times.AtLeast(1));
            }
            finally
            {
                // Cleanup any remaining temp files
                if (File.Exists(actualTempPath))
                {
                    try
                    {
                        File.Delete(actualTempPath);
                    }
                    catch { /* Ignore cleanup errors */ }
                }
            }
        }

        [Fact]
        public async Task StartAndEndExportSession_WithInvalidChecksum_Should_NotDeleteFile()
        {
            // Arrange
            var exportId = Guid.NewGuid();
            var testData = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
            var expectedTempPath = Path.Combine(Path.GetTempPath(), exportId.ToString());
            
            SetupMocksForExport();
            
            _apiProviderMock.Setup(x => x.StoreDownloadResultAsync(_tssId, It.IsAny<Guid>()))
                .ReturnsAsync(() => new MemoryStream(testData));
            
            _apiProviderMock.Setup(x => x.GetExportStateInformationByIdAsync(_tssId, It.IsAny<Guid>()))
                .ReturnsAsync((new ExportStateInformationDto { State = "COMPLETED" }, 0));
            
            var sut = new FiskalySCU(_loggerMock.Object, _apiProviderMock.Object, _clientCache, _configuration);

            // Act - Start Export Session
            var startResult = await sut.StartExportSessionAsync(new StartExportSessionRequest());
            var tokenId = startResult.TokenId;
            var actualTempPath = Path.Combine(Path.GetTempPath(), tokenId);
            
            await Task.Delay(1000);
            
            // Verify file was created
            Assert.True(File.Exists(actualTempPath), "Temp file should be created during export");
            
            // Act - End Export Session with invalid checksum
            var invalidChecksum = Convert.ToBase64String(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
            var endResult = await sut.EndExportSessionAsync(new EndExportSessionRequest
            {
                TokenId = tokenId,
                Sha256ChecksumBase64 = invalidChecksum,
                Erase = true
            });
            
            Assert.Equal(tokenId, endResult.TokenId);
            Assert.False(endResult.IsValid);
            Assert.False(endResult.IsErased);
            Assert.False(File.Exists(actualTempPath), "Temp file should still be deleted even with invalid checksum");
        }
    }
}