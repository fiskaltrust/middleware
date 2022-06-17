using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueAT.Models
{
    public class RequestCommandResponse
    {
        public ReceiptResponse ReceiptResponse { get; set; }
        public List<ftActionJournal> ActionJournals { get; set; } = new List<ftActionJournal>();
        public ftJournalAT JournalAT { get; internal set; }
    }
}
