using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.MasterData;
using fiskaltrust.storage.V0.MasterData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.InMemory.AcceptanceTest
{
    public class InMemoryOutletMasterDataRepositoryTests : AbstractOutletMasterDataRepositoryTests
    {
        public override Task<IMasterDataRepository<OutletMasterData>> CreateReadOnlyRepository(IEnumerable<OutletMasterData> entries) => Task.FromResult<IMasterDataRepository<OutletMasterData>>(new InMemoryOutletMasterDataRepository(entries));

        public override Task<IMasterDataRepository<OutletMasterData>> CreateRepository(IEnumerable<OutletMasterData> entries) => Task.FromResult<IMasterDataRepository<OutletMasterData>>(new InMemoryOutletMasterDataRepository(entries));

    }
}
