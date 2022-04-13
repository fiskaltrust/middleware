using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR
{
    public class ActionJournalFactory
    {
        public ftActionJournal Create(ftQueue queue, ftQueueItem queueItem, string message, string dataJson, int priority = 0)
        {
            return new ftActionJournal
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
}