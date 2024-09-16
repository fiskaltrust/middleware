using System;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Localization.QueueDE.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueDE.Transactions
{
    public class TransactionFactory : ITransactionFactory
    {
        private readonly IDESSCDProvider _deSSCDProvider;
        protected readonly ILogger<RequestCommand> _logger;

        public TransactionFactory(IDESSCDProvider deSSCDProvider, ILogger<RequestCommand> logger)
        {
            _deSSCDProvider = deSSCDProvider;
            _logger = logger;
        }

        public async Task<StartTransactionResponse> PerformStartTransactionRequestAsync(Guid ftQueueItemId, string cashBoxIdentification,  bool isRetry = false)
        {
            _logger.LogTrace("TransactionFactory.PerformStartTransactionRequestAsync [enter].");
            var startTransaction = new StartTransactionRequest
            {
                QueueItemId = ftQueueItemId,
                ClientId = cashBoxIdentification,
                IsRetry = isRetry
            };
            var response = await _deSSCDProvider.Instance.StartTransactionAsync(startTransaction).ConfigureAwait(false);
            _logger.LogTrace("TransactionFactory.PerformStartTransactionRequestAsync [exit].");
            return response;
        }

        public async Task<FinishTransactionResponse> PerformFinishTransactionRequestAsync(string processType, string payload, Guid ftQueueItemId, string cashBoxIdentification, ulong transactionNumber, bool isRetry = false)
        {
            _logger.LogTrace("TransactionFactory.PerformFinishTransactionRequestAsync [enter].");
            var finishTransaction = new FinishTransactionRequest
            {
                QueueItemId = ftQueueItemId,
                TransactionNumber = transactionNumber,
                ClientId = cashBoxIdentification,
                ProcessType = processType,
                ProcessDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)),
                IsRetry = isRetry
            };
            var response = await _deSSCDProvider.Instance.FinishTransactionAsync(finishTransaction).ConfigureAwait(false);
            _logger.LogTrace("TransactionFactory.PerformFinishTransactionRequestAsync [exit].");
            return response;
        }

        public async Task<UpdateTransactionResponse> PerformUpdateTransactionRequestAsync(string processType, string payload, Guid ftQueueItemId, string cashBoxIdentification, ulong transactionNumber, bool isRetry = false)
        {
            _logger.LogTrace("TransactionFactory.PerformUpdateTransactionRequestAsync [enter].");
            var updateTransaction = new UpdateTransactionRequest
            {
                QueueItemId = ftQueueItemId,
                TransactionNumber = transactionNumber,
                ClientId = cashBoxIdentification,
                ProcessType = processType,
                ProcessDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)),
                IsRetry = isRetry
            };
            var response = await _deSSCDProvider.Instance.UpdateTransactionAsync(updateTransaction).ConfigureAwait(false);
            _logger.LogTrace("TransactionFactory.PerformUpdateTransactionRequestAsync [exit].");
            return response;
        }
    }
}
