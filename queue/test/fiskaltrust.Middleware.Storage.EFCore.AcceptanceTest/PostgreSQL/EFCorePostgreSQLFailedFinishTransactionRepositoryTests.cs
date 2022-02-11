using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.EFCore.AcceptanceTest.PostgreSQL.Fixtures;
using fiskaltrust.Middleware.Storage.EFCore.Repositories.DE;
using Xunit;

namespace fiskaltrust.Middleware.Storage.EFCore.AcceptanceTest.PostgreSQL
{
    [Collection(EFCorePostgreSQLStorageCollectionFixture.CollectionName)]
    public class EFCorePostgreSQLFailedFinishTransactionRepositoryTests : AbstractFailedFinishTransactionRepositoryTests
    {
        private readonly EFCorePostgreSQLStorageCollectionFixture _fixture;

        public override Task<IPersistentTransactionRepository<FailedFinishTransaction>> CreateReadOnlyRepository(IEnumerable<FailedFinishTransaction> entries) => CreateRepository(entries);

        public EFCorePostgreSQLFailedFinishTransactionRepositoryTests(EFCorePostgreSQLStorageCollectionFixture fixture) => _fixture = fixture;

        public override async Task<IPersistentTransactionRepository<FailedFinishTransaction>> CreateRepository(IEnumerable<FailedFinishTransaction> entries)
        {           
            var repository = new EFCoreFailedFinishTransactionRepository(_fixture.Context);
            foreach (var item in entries ?? new List<FailedFinishTransaction>())
            {
                await repository.InsertAsync(item);
            }

            return repository;
        }

        public override void DisposeDatabase() => _ = EFCorePostgreSQLStorageCollectionFixture.TruncateTableAsync("FailedFinishTransaction");
    }
}
