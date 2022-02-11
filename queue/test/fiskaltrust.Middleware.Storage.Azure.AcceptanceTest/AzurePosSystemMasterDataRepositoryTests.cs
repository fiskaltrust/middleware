using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Azure.Repositories.DE;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.Azure.AcceptanceTest
{
    public class AzurePosSystemMasterDataRepositoryTests : AbstractPosSystemMasterDataRepositoryTests
    {
        public override Task<IMasterDataRepository<PosSystemMasterData>> CreateReadOnlyRepository(IEnumerable<PosSystemMasterData> entries) => CreateRepository(entries);

        public override async Task<IMasterDataRepository<PosSystemMasterData>> CreateRepository(IEnumerable<PosSystemMasterData> entries)
        {
            var repository = new AzurePosSystemMasterDataRepository(Guid.NewGuid(), Constants.AzureStorageConnectionString);
            foreach (var entry in entries)
            {
                await repository.InsertAsync(entry);
            }

            return repository;
        }
    }
}
