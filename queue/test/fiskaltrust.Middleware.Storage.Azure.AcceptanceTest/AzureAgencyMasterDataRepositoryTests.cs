using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Azure.Repositories.MasterData;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.Azure.AcceptanceTest
{
    public class AzureAgencyMasterDataRepositoryTests : AbstractAgencyMasterDataRepositoryTests
    {
        public override Task<IMasterDataRepository<AgencyMasterData>> CreateReadOnlyRepository(IEnumerable<AgencyMasterData> entries) => CreateRepository(entries);

        public override async Task<IMasterDataRepository<AgencyMasterData>> CreateRepository(IEnumerable<AgencyMasterData> entries)
        {
            var repository = new AzureAgencyMasterDataRepository(new QueueConfiguration { QueueId = Guid.NewGuid() }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await repository.InsertAsync(entry);
            }

            return repository;
        }
    }
}
