using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.Factories
{
    public static class ActionJournalFactory
    {
        public static ftActionJournal Create(ftQueue queue, ftQueueItem queueItem, string message, string dataJson, int priority = 0) =>
            new()
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                Moment = DateTime.UtcNow,
                Message = message,
                DataJson = dataJson,
                Priority = priority
            };
    }
}