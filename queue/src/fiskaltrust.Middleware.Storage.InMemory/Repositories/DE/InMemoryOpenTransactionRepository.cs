using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.DE
{
    public class InMemoryOpenTransactionRepository : AbstractInMemoryRepository<string, OpenTransaction>, IPersistentTransactionRepository<OpenTransaction>
    {
        public InMemoryOpenTransactionRepository() : base(new List<OpenTransaction>()) { }
        public InMemoryOpenTransactionRepository(IEnumerable<OpenTransaction> data) : base(data) { }

        public Task InsertOrUpdateTransactionAsync(OpenTransaction transaction)
        {
            Data.TryRemove(transaction.cbReceiptReference, out var _);
            Data.TryAdd(transaction.cbReceiptReference, transaction);
            return Task.CompletedTask;
        }

        public Task<OpenTransaction> RemoveAsync(string cbReceiptReference)
        {
            if (Data.TryRemove(cbReceiptReference, out var transaction))
            {
                return Task.FromResult(transaction);
            }
            return null;
        }

        public Task<bool> ExistsAsync(string cbReceiptReference) => Task.FromResult(Data.ContainsKey(cbReceiptReference));

        protected override void EntityUpdated(OpenTransaction entity) { }

        protected override string GetIdForEntity(OpenTransaction entity) => entity.cbReceiptReference;
    }
}
