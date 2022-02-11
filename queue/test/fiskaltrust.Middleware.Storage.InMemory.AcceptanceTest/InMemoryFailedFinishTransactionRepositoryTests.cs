using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.DE;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.InMemory.AcceptanceTest
{
    public class InMemoryFailedFinishTransactionRepositoryTests : AbstractFailedFinishTransactionRepositoryTests
    {
        public override Task<IPersistentTransactionRepository<FailedFinishTransaction>> CreateReadOnlyRepository(IEnumerable<FailedFinishTransaction> entries) => Task.FromResult<IPersistentTransactionRepository<FailedFinishTransaction>>(new InMemoryFailedFinishTransactionRepository(entries));

        public override Task<IPersistentTransactionRepository<FailedFinishTransaction>> CreateRepository(IEnumerable<FailedFinishTransaction> entries) => Task.FromResult<IPersistentTransactionRepository<FailedFinishTransaction>>(new InMemoryFailedFinishTransactionRepository(entries));
    }
}
