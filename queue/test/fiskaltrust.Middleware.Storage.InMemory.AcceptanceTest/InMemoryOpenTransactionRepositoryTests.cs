using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.DE;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.InMemory.AcceptanceTest
{
    public class InMemoryOpenTransactionRepositoryTests : AbstractOpenTransactionRepositoryTests
    {
        public override Task<IPersistentTransactionRepository<OpenTransaction>> CreateReadOnlyRepository(IEnumerable<OpenTransaction> entries) => Task.FromResult<IPersistentTransactionRepository<OpenTransaction>>(new InMemoryOpenTransactionRepository(entries));

        public override Task<IPersistentTransactionRepository<OpenTransaction>> CreateRepository(IEnumerable<OpenTransaction> entries) => Task.FromResult<IPersistentTransactionRepository<OpenTransaction>>(new InMemoryOpenTransactionRepository(entries));
    }
}
