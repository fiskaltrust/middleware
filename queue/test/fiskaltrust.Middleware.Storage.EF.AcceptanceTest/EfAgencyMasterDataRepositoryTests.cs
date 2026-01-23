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
    public class EfAgencyMasterDataRepositoryTests : AbstractAgencyMasterDataRepositoryTests
    {
        public override Task<IMasterDataRepository<AgencyMasterData>> CreateReadOnlyRepository(IEnumerable<AgencyMasterData> entries) => CreateRepository(entries);

        public override async Task<IMasterDataRepository<AgencyMasterData>> CreateRepository(IEnumerable<AgencyMasterData> entries)
        {
            var queueId = Guid.NewGuid();
            var repository = new EfAgencyMasterDataRepository(new MiddlewareDbContext(EfConnectionStringFixture.DatabaseConnectionString, queueId, 60));
            EfStorageBootstrapper.Update(EfConnectionStringFixture.DatabaseConnectionString, 30 * 60, queueId, Mock.Of<ILogger<IMiddlewareBootstrapper>>());

            foreach (var item in entries)
            {
                await repository.InsertAsync(item);
            }

            return repository;
        }
    }
}
