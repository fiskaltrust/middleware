using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories.MasterData
{
    public class AzureAccountMasterDataRepository : BaseAzureTableRepository<Guid, AzureAccountMasterData, AccountMasterData>, IMasterDataRepository<AccountMasterData>
    {
        public AzureAccountMasterDataRepository(Guid queueId, string connectionString)
            : base(queueId, connectionString, nameof(AccountMasterData)) { }

        public async Task ClearAsync() => await ClearTableAsync().ConfigureAwait(false);

        public async Task CreateAsync(AccountMasterData entity) => await InsertAsync(entity).ConfigureAwait(false);

        protected override void EntityUpdated(AccountMasterData entity) { }

        protected override Guid GetIdForEntity(AccountMasterData entity) => entity.AccountId;

        protected override AzureAccountMasterData MapToAzureEntity(AccountMasterData entity) => Mapper.Map(entity);

        protected override AccountMasterData MapToStorageEntity(AzureAccountMasterData entity) => Mapper.Map(entity);
    }
}
