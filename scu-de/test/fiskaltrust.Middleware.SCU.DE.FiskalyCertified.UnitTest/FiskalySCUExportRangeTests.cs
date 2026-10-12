using System;
using System.Collections.Generic;
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
    public class FiskalySCUExportRangeTests
    {
        private readonly Mock<IFiskalyApiProvider> _apiProviderMock = new Mock<IFiskalyApiProvider>();
        private readonly Guid _tssId = Guid.NewGuid();
        private readonly FiskalySCUConfiguration _configuration;

        public FiskalySCUExportRangeTests()
        {
            _configuration = new FiskalySCUConfiguration
            {
                TssId = _tssId,
                EnableTarFileExport = true,
                MaxExportTransaction = 10000,
                RetriesOnTarExportWebException = 3,
                DelayOnRetriesInMs = 100
            };
        }

        private FiskalySCU CreateSut(long transactionCounter, long lastExportedTransactionNumber)
        {
            _apiProviderMock.Setup(x => x.GetTseByIdAsync(_tssId))
                .ReturnsAsync(new TssDto
                {
                    SerialNumber = "test-serial",
                    TransactionCounter = transactionCounter,
                    Metadata = new Dictionary<string, object> { { "LastExportedTransactionNumber", lastExportedTransactionNumber.ToString() } }
                });
            _apiProviderMock.Setup(x => x.RequestExportAsync(_tssId, It.IsAny<ExportTransactions>(), It.IsAny<Guid>(), It.IsAny<long?>(), It.IsAny<long>()))
                .Returns(Task.CompletedTask);
            _apiProviderMock.Setup(x => x.SetExportMetadataAsync(_tssId, It.IsAny<Guid>(), It.IsAny<long?>(), It.IsAny<long>()))
                .Returns(Task.CompletedTask);

            return new FiskalySCU(Mock.Of<ILogger<FiskalySCU>>(), _apiProviderMock.Object, new ClientCache(_apiProviderMock.Object), _configuration);
        }

        [Fact]
        public async Task StartExportSessionAsync_ShouldReturnEmptyExport_WhenAllTransactionsAreAlreadyExported()
        {
            var sut = CreateSut(transactionCounter: 3, lastExportedTransactionNumber: 3);

            var session = await sut.StartExportSessionAsync(new StartExportSessionRequest());
            var data = await sut.ExportDataAsync(new ExportDataRequest { TokenId = session.TokenId, MaxChunkSize = 1024 });
            var end = await sut.EndExportSessionAsync(new EndExportSessionRequest { TokenId = session.TokenId });

            Assert.StartsWith("noexport-", session.TokenId);
            Assert.Equal("test-serial", session.TseSerialNumberOctet);
            _apiProviderMock.Verify(x => x.RequestExportAsync(It.IsAny<Guid>(), It.IsAny<ExportTransactions>(), It.IsAny<Guid>(), It.IsAny<long?>(), It.IsAny<long>()), Times.Never);

            Assert.True(data.TotalTarFileSizeAvailable);
            Assert.True(data.TarFileEndOfFile);
            Assert.Empty(Convert.FromBase64String(data.TarFileByteChunkBase64));
            Assert.True(end.IsValid);
            Assert.False(end.IsErased);
        }

        [Fact]
        public async Task StartExportSessionAsync_ShouldRequestExportAfterLastExportedTransaction_WhenNewTransactionsExist()
        {
            var sut = CreateSut(transactionCounter: 4, lastExportedTransactionNumber: 3);

            var session = await sut.StartExportSessionAsync(new StartExportSessionRequest());

            Assert.True(Guid.TryParse(session.TokenId, out _));
            _apiProviderMock.Verify(x => x.RequestExportAsync(_tssId, It.IsAny<ExportTransactions>(), It.IsAny<Guid>(), 4, 4), Times.Once);
        }
    }
}
