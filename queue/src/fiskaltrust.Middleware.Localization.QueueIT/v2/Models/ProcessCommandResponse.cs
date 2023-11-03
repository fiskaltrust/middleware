using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueIT.v2
{
    public record ProcessCommandResponse(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals);
}
