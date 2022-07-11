using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Ef;
using fiskaltrust.Middleware.Storage.EF.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.EF.Repositories.MasterData;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Storage.EF.AcceptanceTest
{
    [Collection(EfStorageCollectionFixture.CollectionName)]
    public class EfAccountMasterDataRepositoryTests : AbstractAccountMasterDataRepositoryTests
    {
        public override Task<IMasterDataRepository<AccountMasterData>> CreateReadOnlyRepository(IEnumerable<AccountMasterData> entries) => CreateRepository(entries);

        public override async Task<IMasterDataRepository<AccountMasterData>> CreateRepository(IEnumerable<AccountMasterData> entries)
        {
            var queueId = Guid.NewGuid();
            var repository = new EfAccountMasterDataRepository(new MiddlewareDbContext(EfConnectionStringFixture.DatabaseConnectionString, queueId));
            EfStorageBootstrapper.Update(EfConnectionStringFixture.DatabaseConnectionString, queueId, Mock.Of<ILogger<IMiddlewareBootstrapper>>());

            foreach (var item in entries)
            {
                await repository.InsertAsync(item).ConfigureAwait(false);
            }

            return repository;
        }
    }
}
