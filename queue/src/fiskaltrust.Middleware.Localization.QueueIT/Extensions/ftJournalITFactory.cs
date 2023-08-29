using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.Extensions
{
    public static class ftJournalITFactory
    {
        public static ftJournalIT CreateFrom(ftQueueItem queueItem,  ftQueueIT queueIT, ScuResponse scuResponse)
        {
            var ftJournalIT = new ftJournalIT
            {
                ftJournalITId = Guid.NewGuid(),
                ftQueueId = queueIT.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                cbReceiptReference = queueItem.cbReceiptReference,
                ftSignaturCreationUnitITId = queueIT.ftSignaturCreationUnitId.Value,
                JournalType = scuResponse.ftReceiptCase & 0xFFFF,
                ReceiptDateTime = scuResponse.ReceiptDateTime,
                ReceiptNumber = scuResponse.ReceiptNumber,
                ZRepNumber = scuResponse.ZRepNumber,
                DataJson = scuResponse.DataJson,
                TimeStamp = DateTime.UtcNow.Ticks
            };
            return ftJournalIT;
        }
    }
}
