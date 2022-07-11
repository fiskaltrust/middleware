using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.MasterData;
using fiskaltrust.storage.V0.MasterData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.InMemory.AcceptanceTest
{
    public class InMemoryAgencyMasterDataRepositoryTests : AbstractAgencyMasterDataRepositoryTests
    {
        public override Task<IMasterDataRepository<AgencyMasterData>> CreateReadOnlyRepository(IEnumerable<AgencyMasterData> entries) => Task.FromResult<IMasterDataRepository<AgencyMasterData>>(new InMemoryAgencyMasterDataRepository(entries));

        public override Task<IMasterDataRepository<AgencyMasterData>> CreateRepository(IEnumerable<AgencyMasterData> entries) => Task.FromResult<IMasterDataRepository<AgencyMasterData>>(new InMemoryAgencyMasterDataRepository(entries));

    }
}
