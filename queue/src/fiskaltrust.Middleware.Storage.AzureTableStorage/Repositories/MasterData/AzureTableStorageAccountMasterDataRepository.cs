using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.MasterData;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.MasterData
{
    public class AzureTableStorageAccountMasterDataRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageAccountMasterData, AccountMasterData>, IMasterDataRepository<AccountMasterData>
    {
        public AzureTableStorageAccountMasterDataRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = nameof(AccountMasterData);

        public async Task ClearAsync() => await ClearTableAsync().ConfigureAwait(false);

        public async Task CreateAsync(AccountMasterData entity) => await InsertAsync(entity).ConfigureAwait(false);

        protected override void EntityUpdated(AccountMasterData entity) { }

        protected override Guid GetIdForEntity(AccountMasterData entity) => entity.AccountId;

        protected override AzureTableStorageAccountMasterData MapToAzureEntity(AccountMasterData entity) => Mapper.Map(entity);

        protected override AccountMasterData MapToStorageEntity(AzureTableStorageAccountMasterData entity) => Mapper.Map(entity);
    }
}

