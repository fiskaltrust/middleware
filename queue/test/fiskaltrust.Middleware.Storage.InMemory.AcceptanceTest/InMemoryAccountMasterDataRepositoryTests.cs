using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.MasterData;
using fiskaltrust.storage.V0.MasterData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.InMemory.AcceptanceTest
{
    public class InMemoryAccountMasterDataRepositoryTests : AbstractAccountMasterDataRepositoryTests
    {
        public override Task<IMasterDataRepository<AccountMasterData>> CreateReadOnlyRepository(IEnumerable<AccountMasterData> entries) => Task.FromResult((IMasterDataRepository<AccountMasterData>)new InMemoryAccountMasterDataRepository(entries));

        public override Task<IMasterDataRepository<AccountMasterData>> CreateRepository(IEnumerable<AccountMasterData> entries) => Task.FromResult((IMasterDataRepository<AccountMasterData>) new InMemoryAccountMasterDataRepository(entries));

    }
}