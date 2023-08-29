using System;
using fiskaltrust.Middleware.Contracts.Repositories.FR.TempSpace;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueFR.Helpers
{
    public static class JournalConverter
    {
        public static ftJournalFR ConvertToFtJournalFR(ftJournalFRCopyPayload copyPayload, string jwt)
        {
            return new ftJournalFR
            {
                ftJournalFRId = Guid.NewGuid(),
                JWT = jwt,
                JsonData = JsonConvert.SerializeObject(copyPayload),
                ReceiptType = "C",
                Number = 0,
                ftQueueItemId = copyPayload.QueueItemId,
                ftQueueId = copyPayload.QueueId,
                TimeStamp = ((DateTimeOffset)copyPayload.ReceiptMoment).ToUnixTimeSeconds()
            };
        }
    }
}