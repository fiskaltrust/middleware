using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Extensions
{
    public struct ScuResponse
    {
        public long ftReceiptCase { get; set; }
        public DateTime ReceiptDateTime { get; set; }
        public long ReceiptNumber { get; set; }
        public long ZRepNumber { get; set; }
        public string DataJson { get; set; }
    }
    public static class ftJournalDEFAULTExtensions
    {
        public static ftJournalIT FromResponse(this ftJournalIT ftJournalIT, ICountrySpecificQueue queueIt, ftQueueItem queueItem, ScuResponse scuResponse)
        {
            ftJournalIT.ftJournalITId = Guid.NewGuid();
            ftJournalIT.ftQueueId = queueIt.ftQueueId;
            ftJournalIT.ftQueueItemId = queueItem.ftQueueItemId;
            ftJournalIT.cbReceiptReference = queueItem.cbReceiptReference;
            ftJournalIT.ftSignaturCreationUnitITId = queueIt.ftSignaturCreationUnitId.Value;
            ftJournalIT.JournalType = scuResponse.ftReceiptCase & 0xFFFF;
            ftJournalIT.ReceiptDateTime = scuResponse.ReceiptDateTime;
            ftJournalIT.ReceiptNumber = scuResponse.ReceiptNumber;
            ftJournalIT.ZRepNumber = scuResponse.ZRepNumber;
            ftJournalIT.DataJson = scuResponse.DataJson;
            ftJournalIT.TimeStamp = DateTime.UtcNow.Ticks;
            return ftJournalIT;
        }
    }
}
