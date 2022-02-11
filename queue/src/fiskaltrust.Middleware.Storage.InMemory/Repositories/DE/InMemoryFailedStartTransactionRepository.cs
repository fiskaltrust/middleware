using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.DE
{
    public class InMemoryFailedStartTransactionRepository : AbstractInMemoryRepository<string, FailedStartTransaction>, IPersistentTransactionRepository<FailedStartTransaction>
    {
        public InMemoryFailedStartTransactionRepository() : base(new List<FailedStartTransaction>()) { }
        public InMemoryFailedStartTransactionRepository(IEnumerable<FailedStartTransaction> data) : base(data) { }

        public Task InsertOrUpdateTransactionAsync(FailedStartTransaction transaction)
        {
            if (Data.ContainsKey(transaction.cbReceiptReference))
            {
                Data.Remove(transaction.cbReceiptReference);
            }
            Data.Add(transaction.cbReceiptReference, transaction);
            return Task.CompletedTask;
        }

        public Task<FailedStartTransaction> RemoveAsync(string cbReceiptReference)
        {
            if (Data.TryGetValue(cbReceiptReference, out var transaction))
            {
                Data.Remove(cbReceiptReference);
            }
            return Task.FromResult(transaction);
        }

        public Task<bool> ExistsAsync(string cbReceiptReference) => Task.FromResult(Data.ContainsKey(cbReceiptReference));

        protected override void EntityUpdated(FailedStartTransaction entity) { }

        protected override string GetIdForEntity(FailedStartTransaction entity) => entity.cbReceiptReference;
    }
}
