using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.UnitTest
{
    public class JournalProcessorDEUnitTests
    {
        private readonly string _tar01FileBase64 = "UEsDBBQAAAAIABtmXFGRutY7ZQAAAAAMAAAIAAAAbXVqby50YXLt08EJgDAMheGM0gnkJbZkHPEgXgRBIzi+iqgTWArNd8klkEv+aR47bmw3+g8YUFXC7ZsPFuJWY4Qg8bWnKQkFUAbbav1CyHKrQDasFs4nCEyuQlf/UlL/bfT+M3r7F3LOOVeRA1BLAQI/ABQAAAAIABtmXFGRutY7ZQAAAAAMAAAIACQAAAAAAAAAIAAAAAAAAABtdWpvLnRhcgoAIAAAAAAAAQAYAOJQjk4grdYBAsHDXyCt1gH3TnNOIK3WAVBLBQYAAAAAAQABAFoAAACLAAAAAAA=";
        private readonly string _tar02FileBase64 = "UEsDBBQAAAAIAKlmXFGXplRPagAAAAAMAAAIAAAAaGFzby50YXLt08EJgDAMQNGM0gkkqWkzjngQL4KgERxfpVQnsBSad8klkEP4Ou06LOscOj0V/oGEKCKAyTcz8kC9MCN7YXr2ORI4hAKOXccNsMitCj3/d/f/XQDToNx/rKX/gKl/b/2X8PYfwRhjTEMuUEsBAj8AFAAAAAgAqWZcUZemVE9qAAAAAAwAAAgAJAAAAAAAAAAgAAAAAAAAAGhhc28udGFyCgAgAAAAAAABABgAkolZ7CCt1gGzSFbxIK3WAarzPOwgrdYBUEsFBgAAAAABAAEAWgAAAJAAAAAAAA==";
        
        [Fact]
        public async Task ProcessAsync_ShouldStreamByte_WhenTarFileIsRequested()
        {
            var expectedByteLength = 3072;

            var configurationRepositoryMock = new Mock<IReadOnlyConfigurationRepository>(MockBehavior.Strict);
            var journalDERepository = new Mock<IMiddlewareJournalDERepository>(MockBehavior.Strict);

            var journals = new List<ftJournalDE>() {

                new ftJournalDE
                {
                    FileContentBase64 = _tar01FileBase64

                }
            };

            journalDERepository.Setup(x => x.GetAsync()).ReturnsAsync(journals);

            var middlewareConfiguration = new MiddlewareConfiguration
            {
                QueueId = Guid.NewGuid(),
                ServiceFolder = @".\",
                Configuration = new Dictionary<string, object>()
            };

            var middlewareRepo = new Mock<IMiddlewareRepository<ftJournalDE>>();
            middlewareRepo.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>())).Returns(journals.ToAsyncEnumerable());

            var tarFileCleanupService = Mock.Of<ITarFileCleanupService>();
            var sut = new JournalProcessorDE(Mock.Of<ILogger<JournalProcessorDE>>(), configurationRepositoryMock.Object, null, null, journalDERepository.Object, null, middlewareRepo.Object, null, null, middlewareConfiguration, Mock.Of<IMasterDataService>(), null, tarFileCleanupService);

            var chunks = await sut.ProcessAsync(new JournalRequest
            {
                ftJournalType = 0x4445000000000003,
                MaxChunkSize = 1024
            }).ToListAsync();

            var resultBytes = chunks.SelectMany(x => x.Chunk);
            resultBytes.Should().HaveCount(expectedByteLength);
        }

        [Fact]
        public async Task ProcessAsync_ShouldStreamByte_WhenMultipleTarFilesAreRequested()
        {
            var expectedByteLength = 5120;

            var configurationRepositoryMock = new Mock<IReadOnlyConfigurationRepository>(MockBehavior.Strict);
            var journalDERepository = new Mock<IMiddlewareJournalDERepository>(MockBehavior.Strict);

            var journals = new List<ftJournalDE>() {

                new ftJournalDE
                {
                    FileContentBase64 = _tar01FileBase64

                },
                new ftJournalDE
                {
                    FileContentBase64 = _tar02FileBase64
                }
            };

            journalDERepository.Setup(x => x.GetAsync()).ReturnsAsync(journals);

            var middlewareConfiguration = new MiddlewareConfiguration
            {
                QueueId = Guid.NewGuid(),
                ServiceFolder = @".\",
                Configuration = new Dictionary<string, object>()
            };

            var middlewareRepo = new Mock<IMiddlewareRepository<ftJournalDE>>();
            middlewareRepo.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>())).Returns(journals.ToAsyncEnumerable());

            var tarFileCleanupService = Mock.Of<ITarFileCleanupService>();
            var sut = new JournalProcessorDE(Mock.Of<ILogger<JournalProcessorDE>>(), configurationRepositoryMock.Object, null, null, journalDERepository.Object, null, middlewareRepo.Object, null, null, middlewareConfiguration, Mock.Of<IMasterDataService>(), null, tarFileCleanupService);

            var chunks = await sut.ProcessAsync(new JournalRequest
            {
                ftJournalType = 0x4445000000000003,
                MaxChunkSize = 1024
            }).ToListAsync();

            var resultBytes = chunks.SelectMany(x => x.Chunk);
            resultBytes.Should().HaveCount(expectedByteLength);
        }

        [Fact]
        public async Task ProcessAsync_ShouldStreamByte_WhenTarFilesAreRequestedByFromToRange()
        {
            var expectedByteLength = 5120;

            var configurationRepositoryMock = new Mock<IReadOnlyConfigurationRepository>(MockBehavior.Strict);
            var journalDERepository = new Mock<IMiddlewareJournalDERepository>(MockBehavior.Strict);

            var journals = new List<ftJournalDE>() {

                new ftJournalDE
                {
                    FileContentBase64 = _tar01FileBase64

                },
                new ftJournalDE
                {
                    FileContentBase64 = _tar02FileBase64
                }
            };

            journalDERepository.Setup(x => x.GetAsync()).ReturnsAsync(journals);

            var middlewareConfiguration = new MiddlewareConfiguration
            {
                QueueId = Guid.NewGuid(),
                ServiceFolder = @".\",
                Configuration = new Dictionary<string, object>()
            };

            var middlewareRepo = new Mock<IMiddlewareRepository<ftJournalDE>>();
            middlewareRepo.Setup(x => x.GetByTimeStampRangeAsync(It.IsAny<long>(), It.IsAny<long>())).Returns(journals.ToAsyncEnumerable());

            var tarFileCleanupService = Mock.Of<ITarFileCleanupService>();
            var sut = new JournalProcessorDE(Mock.Of<ILogger<JournalProcessorDE>>(), configurationRepositoryMock.Object, null, null, journalDERepository.Object, null, middlewareRepo.Object, null, null, middlewareConfiguration, Mock.Of<IMasterDataService>(), null, tarFileCleanupService);

            var chunks = await sut.ProcessAsync(new JournalRequest
            {
                ftJournalType = 0x4445000000000003,
                MaxChunkSize = 1024,
                From = 1,
                To = 10
            }).ToListAsync();

            var resultBytes = chunks.SelectMany(x => x.Chunk);
            resultBytes.Should().HaveCount(expectedByteLength);
        }

        [Fact]
        public async Task ProcessAsync_ShouldStreamNoBytes_WhenThereAreNoTarFilesToExport()
        {
            var chunkBytes = Convert.FromBase64String(string.Empty);

            var configurationRepositoryMock = new Mock<IReadOnlyConfigurationRepository>(MockBehavior.Strict);
            var journalDERepository = new Mock<IMiddlewareJournalDERepository>(MockBehavior.Strict);

            var journals = new List<ftJournalDE>() {};

            journalDERepository.Setup(x => x.GetAsync()).ReturnsAsync(journals);

            var middlewareConfiguration = new MiddlewareConfiguration
            {
                QueueId = Guid.NewGuid(),
                ServiceFolder = @".\",
                Configuration = new Dictionary<string, object>()
            };

            var middlewareRepo = new Mock<IMiddlewareRepository<ftJournalDE>>();
            middlewareRepo.Setup(x => x.GetByTimeStampRangeAsync(It.IsAny<long>(), It.IsAny<long>())).Returns(journals.ToAsyncEnumerable());

            var tarFileCleanupService = Mock.Of<ITarFileCleanupService>();
            var sut = new JournalProcessorDE(Mock.Of<ILogger<JournalProcessorDE>>(), configurationRepositoryMock.Object, null, null, journalDERepository.Object, null, middlewareRepo.Object, null, null, middlewareConfiguration, Mock.Of<IMasterDataService>(), null, tarFileCleanupService);

            var chunks = await sut.ProcessAsync(new JournalRequest
            {
                ftJournalType = 0x4445000000000003,
                MaxChunkSize = 1024,
                From = 1,
                To = 10
            }).ToListAsync();

            var resultBytes = chunks.SelectMany(x => x.Chunk);
            resultBytes.Should().BeEquivalentTo(chunkBytes);
        }
    }
}
