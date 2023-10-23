using System;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Localization.QueueDE.Services;

namespace fiskaltrust.Middleware.Localization.QueueDE.Transactions
{
    public class TransactionFactory : ITransactionFactory
    {
        private readonly IDESSCDProvider _deSSCDProvider;

        public TransactionFactory(IDESSCDProvider deSSCDProvider)
        {
            _deSSCDProvider = deSSCDProvider;
        }

        public async Task<StartTransactionResponse> PerformStartTransactionRequestAsync(Guid ftQueueItemId, string cashBoxIdentification,  bool isRetry = false)
        {
            var startTransaction = new StartTransactionRequest
            {
                QueueItemId = ftQueueItemId,
                ClientId = cashBoxIdentification,
                IsRetry = isRetry
            };
            return await _deSSCDProvider.Instance.StartTransactionAsync(startTransaction).ConfigureAwait(false);
        }

        public async Task<FinishTransactionResponse> PerformFinishTransactionRequestAsync(string processType, string payload, Guid ftQueueItemId, string cashBoxIdentification, ulong transactionNumber, bool isRetry = false)
        {
            var finishTransaction = new FinishTransactionRequest
            {
                QueueItemId = ftQueueItemId,
                TransactionNumber = transactionNumber,
                ClientId = cashBoxIdentification,
                ProcessType = processType,
                ProcessDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)),
                IsRetry = isRetry
            };
            return await _deSSCDProvider.Instance.FinishTransactionAsync(finishTransaction).ConfigureAwait(false);
        }

        public async Task<UpdateTransactionResponse> PerformUpdateTransactionRequestAsync(string processType, string payload, Guid ftQueueItemId, string cashBoxIdentification, ulong transactionNumber, bool isRetry = false)
        {
            var updateTransaction = new UpdateTransactionRequest
            {
                QueueItemId = ftQueueItemId,
                TransactionNumber = transactionNumber,
                ClientId = cashBoxIdentification,
                ProcessType = processType,
                ProcessDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)),
                IsRetry = isRetry
            };
            return await _deSSCDProvider.Instance.UpdateTransactionAsync(updateTransaction).ConfigureAwait(false);
        }
    }
}
