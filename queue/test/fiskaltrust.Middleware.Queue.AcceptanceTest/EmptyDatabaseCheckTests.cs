using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Queue.Bootstrapper;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Queue.AcceptanceTest
{
    public class EmptyDatabaseCheckTests
    {
        private readonly Guid _queueId = Guid.NewGuid();
        private readonly Mock<IReadOnlyConfigurationRepository> _configurationRepository = new Mock<IReadOnlyConfigurationRepository>();
        private readonly Mock<IMiddlewareQueueItemRepository> _queueItemRepository = new Mock<IMiddlewareQueueItemRepository>();
        private readonly Mock<IMiddlewareActionJournalRepository> _actionJournalRepository = new Mock<IMiddlewareActionJournalRepository>();
        private readonly Mock<ILogger<EmptyDatabaseCheck>> _logger = new Mock<ILogger<EmptyDatabaseCheck>>();

        private Task<bool> RunCheck() => new EmptyDatabaseCheck(_configurationRepository.Object, _queueItemRepository.Object,
            _actionJournalRepository.Object, _logger.Object).WarnIfStartedQueueHasEmptyDatabaseAsync(_queueId);

        private void SetupQueue(DateTime? startMoment) => _configurationRepository
            .Setup(x => x.GetQueueAsync(_queueId))
            .ReturnsAsync(new ftQueue { ftQueueId = _queueId, StartMoment = startMoment });

        private void VerifyLogged(LogLevel level, Times times) => _logger.Verify(x => x.Log(level, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>) It.IsAny<object>()), times);

        [Fact]
        public async Task StartedQueue_WithEmptyDatabase_ShouldLogErrorAndWriteActionJournal()
        {
            SetupQueue(new DateTime(2024, 4, 15, 16, 13, 46, DateTimeKind.Utc));
            _queueItemRepository.Setup(x => x.GetLastQueueItemAsync()).ReturnsAsync((ftQueueItem) null);

            var result = await RunCheck();

            result.Should().BeTrue();
            VerifyLogged(LogLevel.Error, Times.Once());
            _actionJournalRepository.Verify(x => x.InsertAsync(It.Is<ftActionJournal>(aj =>
                aj.ftQueueId == _queueId &&
                aj.Type == EmptyDatabaseCheck.ActionJournalType &&
                aj.Priority == -1 &&
                aj.Message.Contains(_queueId.ToString()))), Times.Once);
        }

        [Fact]
        public async Task StartedQueue_WithExistingData_ShouldNotWarn()
        {
            SetupQueue(new DateTime(2024, 4, 15, 16, 13, 46, DateTimeKind.Utc));
            _queueItemRepository.Setup(x => x.GetLastQueueItemAsync()).ReturnsAsync(new ftQueueItem { ftQueueId = _queueId });

            var result = await RunCheck();

            result.Should().BeFalse();
            VerifyLogged(LogLevel.Error, Times.Never());
            _actionJournalRepository.Verify(x => x.InsertAsync(It.IsAny<ftActionJournal>()), Times.Never);
        }

        [Fact]
        public async Task NewQueue_WithEmptyDatabase_ShouldNotWarn()
        {
            SetupQueue(null);
            _queueItemRepository.Setup(x => x.GetLastQueueItemAsync()).ReturnsAsync((ftQueueItem) null);

            var result = await RunCheck();

            result.Should().BeFalse();
            VerifyLogged(LogLevel.Error, Times.Never());
            _actionJournalRepository.Verify(x => x.InsertAsync(It.IsAny<ftActionJournal>()), Times.Never);
        }

        [Fact]
        public async Task FailingRepository_ShouldNotThrow()
        {
            _configurationRepository.Setup(x => x.GetQueueAsync(_queueId)).ThrowsAsync(new InvalidOperationException("storage not available"));

            var result = await RunCheck();

            result.Should().BeFalse();
            VerifyLogged(LogLevel.Warning, Times.Once());
        }
    }
}
