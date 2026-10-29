using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.Factories;

public static class ftJournalITFactory
{
    public static ftJournalIT CreateFrom(ReceiptResponse receiptResponse, ftQueueIT queueIT, ScuResponse scuResponse)
    {
        return new ftJournalIT
        {
            ftJournalITId = Guid.NewGuid(),
            ftQueueId = queueIT.ftQueueId,
            ftQueueItemId = receiptResponse.ftQueueItemID,
            cbReceiptReference = receiptResponse.cbReceiptReference,
            ftSignaturCreationUnitITId = queueIT.ftSignaturCreationUnitITId ?? throw new InvalidOperationException(ErrorMessagesIT.NoSignaturCreationUnitAssigned(queueIT.ftQueueId)),
            JournalType = (long) scuResponse.ftReceiptCase.Case(),
            ReceiptDateTime = scuResponse.ReceiptDateTime,
            ReceiptNumber = scuResponse.ReceiptNumber,
            ZRepNumber = scuResponse.ZRepNumber,
            DataJson = scuResponse.DataJson,
            TimeStamp = DateTime.UtcNow.Ticks
        };
    }
}
