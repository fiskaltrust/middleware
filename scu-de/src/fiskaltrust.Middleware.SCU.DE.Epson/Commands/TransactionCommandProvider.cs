using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.Epson.ResultModels;

namespace fiskaltrust.Middleware.SCU.DE.Epson.Commands
{
    public class TransactionCommandProvider
    {
        private readonly OperationalCommandProvider _operationalCommandProvider;

        public TransactionCommandProvider(OperationalCommandProvider operationalCommandProvider)
        {
            _operationalCommandProvider = operationalCommandProvider;
        }

        public async Task<StartTransactionResult> StartTransactionAsync(string clientId, string processData, string processType, string additionalData = null) => await _operationalCommandProvider.ExecuteRequestAsync<StartTransactionResult>(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.Transaction.StartTransaction,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"clientId", clientId },
                {"processData", processData },
                {"processType", processType },
                {"additionalData", additionalData }
            }
        });

        public async Task<UpdateTransactionResult> UpdateTransactionAsync(long transactionNumber, string clientId, string processData, string processType, string additionalData = null) => await _operationalCommandProvider.ExecuteRequestAsync<UpdateTransactionResult>(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.Transaction.UpdateTransaction,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"transactionNumber", transactionNumber },
                {"clientId", clientId },
                {"processData", processData },
                {"processType", processType },
                {"additionalData", additionalData }
            }
        });

        public async Task<FinishTransactionResult> FinishTransactionAsync(long transactionNumber, string clientId, string processData, string processType, string additionalData = null) => await _operationalCommandProvider.ExecuteRequestAsync<FinishTransactionResult>(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.Transaction.FinishTransaction,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"transactionNumber", transactionNumber },
                {"clientId", clientId },
                {"processData", processData },
                {"processType", processType },
                {"additionalData", additionalData }
            }
        });

        public async Task<GetStartedTransactionListResult> GetStartedTransactionListAsync(string clientId = null) => await _operationalCommandProvider.ExecuteRequestAsync<GetStartedTransactionListResult>(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.Transaction.GetStartedTransactionList,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"clientId", clientId }
            }
        });
    }
}
