using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;

namespace fiskaltrust.Middleware.Localization.QueueDE.Transactions
{
    public interface ITransactionFactory
    {
        Task<StartTransactionResponse> PerformStartTransactionRequestAsync(Guid ftQueueItemId, string cashBoxIdentification, bool isRetry = false);
        Task<FinishTransactionResponse> PerformFinishTransactionRequestAsync(string processType, string payload, Guid ftQueueItemId, string cashBoxIdentification, ulong transactionNumber, bool isRetry = false);
        Task<UpdateTransactionResponse> PerformUpdateTransactionRequestAsync(string processType, string payload, Guid ftQueueItemId, string cashBoxIdentification, ulong transactionNumber, bool isRetry = false);
 }
}
