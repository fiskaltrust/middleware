using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.DE.MasterData;
using fiskaltrust.storage.V0.MasterData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.InMemory.AcceptanceTest
{
    public class InMemoryPosSystemMasterDataRepositoryTests : AbstractPosSystemMasterDataRepositoryTests
    {
        public override Task<IMasterDataRepository<PosSystemMasterData>> CreateReadOnlyRepository(IEnumerable<PosSystemMasterData> entries) => Task.FromResult<IMasterDataRepository<PosSystemMasterData>>(new InMemoryPosSystemMasterDataRepository(entries));

        public override Task<IMasterDataRepository<PosSystemMasterData>> CreateRepository(IEnumerable<PosSystemMasterData> entries) => Task.FromResult<IMasterDataRepository<PosSystemMasterData>>(new InMemoryPosSystemMasterDataRepository(entries));

    }
}
