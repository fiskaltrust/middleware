using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Azure.Repositories.MasterData;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.Azure.AcceptanceTest
{
    public class AzureOutletMasterDataRepositoryTests : AbstractOutletMasterDataRepositoryTests
    {
        public override Task<IMasterDataRepository<OutletMasterData>> CreateReadOnlyRepository(IEnumerable<OutletMasterData> entries) => CreateRepository(entries);

        public override async Task<IMasterDataRepository<OutletMasterData>> CreateRepository(IEnumerable<OutletMasterData> entries)
        {
            var repository = new AzureOutletMasterDataRepository(Guid.NewGuid(), Constants.AzureStorageConnectionString);
            foreach (var entry in entries)
            {
                await repository.InsertAsync(entry);
            }

            return repository;
        }
    }
}
