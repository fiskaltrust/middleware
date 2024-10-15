using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.EFCore.AcceptanceTest.PostgreSQL.Fixtures;
using fiskaltrust.Middleware.Storage.EFCore.PostgreSQL;
using fiskaltrust.Middleware.Storage.EFCore.Repositories;
using fiskaltrust.storage.V0;
using Xunit;

namespace fiskaltrust.Middleware.Storage.EFCore.AcceptanceTest.PostgreSQL
{
    [Collection(EFCorePostgreSQLStorageCollectionFixture.CollectionName)]
    public class EFCorePostgreSQLQueueItemRespositoryTests : AbstractQueueItemRepositoryTests
    {
        private readonly EFCorePostgreSQLStorageCollectionFixture _fixture;

        public EFCorePostgreSQLQueueItemRespositoryTests(EFCorePostgreSQLStorageCollectionFixture fixture) => _fixture = fixture;

        public override async Task<IReadOnlyQueueItemRepository> CreateReadOnlyRepository(IEnumerable<ftQueueItem> entries) => await CreateRepository(entries);

        public override async Task<IMiddlewareQueueItemRepository> CreateRepository(IEnumerable<ftQueueItem> entries)
        {
            var repository = new EFCoreQueueItemRepository(_fixture.Context);
            await SetQueueRowAndTimeStamp(entries.ToList());
            foreach (var item in entries ?? new List<ftQueueItem>())
            {
                await repository.InsertOrUpdateAsync(item);
            }
            return repository;
        }
        public override void DisposeDatabase() => _ = EFCorePostgreSQLStorageCollectionFixture.TruncateTableAsync("ftQueueItem");
    }
}
