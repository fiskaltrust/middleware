using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.Middleware.Storage.Ef;
using fiskaltrust.Middleware.Storage.EF.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.EF.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Storage.EF.AcceptanceTest
{
    [Collection(EfStorageCollectionFixture.CollectionName)]
    public class EfQueueItemRespositoryTests : AbstractQueueItemRepositoryTests
    {
        public override async Task<IReadOnlyQueueItemRepository> CreateReadOnlyRepository(IEnumerable<ftQueueItem> entries) => await CreateRepository(entries);

        public override async Task<IMiddlewareQueueItemRepository> CreateRepository(IEnumerable<ftQueueItem> entries)
        {
            var queueId = Guid.NewGuid();
            var repository = new EfQueueItemRepository(new MiddlewareDbContext(EfConnectionStringFixture.DatabaseConnectionString, queueId));
            EfStorageBootstrapper.Update(EfConnectionStringFixture.DatabaseConnectionString, 30 * 60, queueId, Mock.Of<ILogger<IMiddlewareBootstrapper>>());

            foreach (var item in entries)
            {
                await repository.InsertAsync(item);
            }

            return repository;
        }
    }
}
