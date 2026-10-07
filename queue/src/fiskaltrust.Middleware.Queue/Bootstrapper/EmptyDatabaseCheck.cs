using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Queue.Bootstrapper
{
    /// <summary>
    /// Detects a queue that is already in operation (StartMoment is set) but starts with a local database that contains no queue items.
    /// This only happens when the database was deleted or replaced by a new one: the queue is then recreated from the configuration,
    /// so the receipt numbering restarts and local state that is not part of the configuration (e.g. a completed SCU switch) is lost.
    /// The check only warns; it never blocks the start of the queue.
    /// </summary>
    public class EmptyDatabaseCheck : IEmptyDatabaseCheck
    {
        public const string ActionJournalType = "QueueStartedWithEmptyDatabase";

        private readonly IReadOnlyConfigurationRepository _configurationRepository;
        private readonly IMiddlewareQueueItemRepository _queueItemRepository;
        private readonly IMiddlewareActionJournalRepository _actionJournalRepository;
        private readonly ILogger<EmptyDatabaseCheck> _logger;

        public EmptyDatabaseCheck(IReadOnlyConfigurationRepository configurationRepository, IMiddlewareQueueItemRepository queueItemRepository,
            IMiddlewareActionJournalRepository actionJournalRepository, ILogger<EmptyDatabaseCheck> logger)
        {
            _configurationRepository = configurationRepository;
            _queueItemRepository = queueItemRepository;
            _actionJournalRepository = actionJournalRepository;
            _logger = logger;
        }

        public async Task<bool> WarnIfStartedQueueHasEmptyDatabaseAsync(Guid queueId)
        {
            try
            {
                var queue = await _configurationRepository.GetQueueAsync(queueId).ConfigureAwait(false);
                if (queue?.StartMoment == null)
                {
                    return false;
                }

                if (await _queueItemRepository.GetLastQueueItemAsync().ConfigureAwait(false) != null)
                {
                    return false;
                }

                _logger.LogError("Queue {QueueId} has been in operation since {StartMoment:u}, but its local database contains no data. " +
                    "The database was deleted or replaced by a new one. The receipt numbering restarts and local state that is not part of the configuration " +
                    "(e.g. a completed SCU switch) is lost. The original database must not be deleted or replaced; restore it if it is still available.",
                    queueId, queue.StartMoment.Value);

                await _actionJournalRepository.InsertAsync(new ftActionJournal
                {
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueId = queueId,
                    ftQueueItemId = Guid.Empty,
                    Moment = DateTime.UtcNow,
                    Priority = -1,
                    Type = ActionJournalType,
                    Message = $"Queue {queueId} has been in operation since {queue.StartMoment.Value:u}, but its local database contains no data. " +
                        "The database was deleted or replaced by a new one. The receipt numbering restarts and local state that is not part of the configuration " +
                        "(e.g. a completed SCU switch) is lost.",
                    TimeStamp = DateTime.UtcNow.Ticks
                }).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check whether queue {QueueId} was started with an empty database.", queueId);
                return false;
            }
        }
    }
}
