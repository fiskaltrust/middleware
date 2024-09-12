using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePT.Interface.Models
{
    public record ProcessCommandResponse(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals);
}
