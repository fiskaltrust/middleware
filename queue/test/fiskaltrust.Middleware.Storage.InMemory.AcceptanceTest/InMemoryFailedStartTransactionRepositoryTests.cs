using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.DE;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.InMemory.AcceptanceTest
{
    public class InMemoryFailedStartTransactionRepositoryTests : AbstractFailedStartTransactionRepositoryTests
    {
        public override Task<IPersistentTransactionRepository<FailedStartTransaction>> CreateReadOnlyRepository(IEnumerable<FailedStartTransaction> entries) => Task.FromResult<IPersistentTransactionRepository<FailedStartTransaction>>(new InMemoryFailedStartTransactionRepository(entries));

        public override Task<IPersistentTransactionRepository<FailedStartTransaction>> CreateRepository(IEnumerable<FailedStartTransaction> entries) => Task.FromResult<IPersistentTransactionRepository<FailedStartTransaction>>(new InMemoryFailedStartTransactionRepository(entries));
    }
}
