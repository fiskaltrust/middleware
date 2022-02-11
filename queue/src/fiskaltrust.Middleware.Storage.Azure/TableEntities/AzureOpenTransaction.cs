using System;
using Microsoft.Azure.Cosmos.Table;

namespace fiskaltrust.Middleware.Storage.Azure.TableEntities
{
    public class AzureOpenTransaction : TableEntity
    {
        public string cbReceiptReference { get; set; }
        public string TransactionNumber { get; set; }
        public string StartTransactionSignatureBase64 { get; set; }
        public DateTime StartMoment { get; set; }
    }
}
