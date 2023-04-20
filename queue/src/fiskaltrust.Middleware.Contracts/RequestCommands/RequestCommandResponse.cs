using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public class RequestCommandResponse
    {
        public ReceiptResponse ReceiptResponse { get; set; }
        public List<SignaturItem> Signatures { get; set; }= new List<SignaturItem>();
        public List<ftActionJournal> ActionJournals { get; set; } = new List<ftActionJournal>();
    }
}
