using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.DE
{
    public class InMemoryFailedFinishTransactionRepository : AbstractInMemoryRepository<string, FailedFinishTransaction>, IPersistentTransactionRepository<FailedFinishTransaction>
    {
        public InMemoryFailedFinishTransactionRepository() : base(new List<FailedFinishTransaction>()) { }
        public InMemoryFailedFinishTransactionRepository(IEnumerable<FailedFinishTransaction> data) : base(data) { }

        public Task InsertOrUpdateTransactionAsync(FailedFinishTransaction transaction)
        {
            if (Data.ContainsKey(transaction.cbReceiptReference))
            {
                Data.Remove(transaction.cbReceiptReference);
            }
            Data.Add(transaction.cbReceiptReference, transaction);
            return Task.CompletedTask;
        }

        public Task<FailedFinishTransaction> RemoveAsync(string cbReceiptReference)
        {
            if (Data.TryGetValue(cbReceiptReference, out var transaction))
            {
                Data.Remove(cbReceiptReference);
            }
            return Task.FromResult(transaction);
        }

        public Task<bool> ExistsAsync(string cbReceiptReference) => Task.FromResult(Data.ContainsKey(cbReceiptReference));

        protected override void EntityUpdated(FailedFinishTransaction entity) { }

        protected override string GetIdForEntity(FailedFinishTransaction entity) => entity.cbReceiptReference;
    }
}
