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
            if (Data.ContainsKey(transaction.cbReceiptReference))
            {
                Data.Remove(transaction.cbReceiptReference);
            }
            Data.Add(transaction.cbReceiptReference, transaction);
            return Task.CompletedTask;
        }

        public Task<OpenTransaction> RemoveAsync(string cbReceiptReference)
        {
            if (Data.TryGetValue(cbReceiptReference, out var transaction))
            {
                Data.Remove(cbReceiptReference);
            }
            return Task.FromResult(transaction);
        }

        public Task<bool> ExistsAsync(string cbReceiptReference) => Task.FromResult(Data.ContainsKey(cbReceiptReference));

        protected override void EntityUpdated(OpenTransaction entity) { }

        protected override string GetIdForEntity(OpenTransaction entity) => entity.cbReceiptReference;
    }
}
