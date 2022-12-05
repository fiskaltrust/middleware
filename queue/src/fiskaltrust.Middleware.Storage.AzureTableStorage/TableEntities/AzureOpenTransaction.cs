using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities
{
    public class AzureOpenTransaction : BaseTableEntity
    {
        public string cbReceiptReference { get; set; }
        public string TransactionNumber { get; set; }
        public string StartTransactionSignatureBase64 { get; set; }
        public DateTime StartMoment { get; set; }
    }
}
