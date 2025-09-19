﻿using System.Collections.Generic;
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
            Data.TryRemove(transaction.cbReceiptReference, out var _);
            Data.TryAdd(transaction.cbReceiptReference, transaction);
            return Task.CompletedTask;
        }

        public Task<FailedStartTransaction> RemoveAsync(string cbReceiptReference)
        {
            if (Data.TryRemove(cbReceiptReference, out var transaction))
            {
                return Task.FromResult(transaction);
            }
            return null;
        }

        public Task<bool> ExistsAsync(string cbReceiptReference) => Task.FromResult(Data.ContainsKey(cbReceiptReference));

        protected override void EntityUpdated(FailedStartTransaction entity) { }

        protected override string GetIdForEntity(FailedStartTransaction entity) => entity.cbReceiptReference;
    }
}
