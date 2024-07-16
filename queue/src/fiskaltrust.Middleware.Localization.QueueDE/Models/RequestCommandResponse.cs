using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueDE.Models
{
    public class RequestCommandResponse
    {
        public ReceiptResponse ReceiptResponse { get; set; }
        public List<SignaturItem> Signatures { get; set; }= new List<SignaturItem>();
        public List<ftActionJournal> ActionJournals { get; set; } = new List<ftActionJournal>();
        public ulong? TransactionNumber { get; set; } = null;
        public bool isMigration { get; set; } = false;
    }
}
