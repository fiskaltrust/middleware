using fiskaltrust.Middleware.Localization.QueueIT.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.Factories;

public static class ftJournalITFactory
{
    public static ftJournalIT CreateFrom(Guid queueItemId, string? cbReceiptReference, ftQueueIT queueIT, ScuResponse scuResponse)
        => new()
        {
            ftJournalITId = Guid.NewGuid(),
            ftQueueId = queueIT.ftQueueId,
            ftQueueItemId = queueItemId,
            cbReceiptReference = cbReceiptReference,
            ftSignaturCreationUnitITId = queueIT.ftSignaturCreationUnitId!.Value,
            JournalType = scuResponse.ftReceiptCase & 0xFFFF,
            ReceiptDateTime = scuResponse.ReceiptDateTime,
            ReceiptNumber = scuResponse.ReceiptNumber,
            ZRepNumber = scuResponse.ZRepNumber,
            DataJson = scuResponse.DataJson,
            TimeStamp = DateTime.UtcNow.Ticks,
        };
}
