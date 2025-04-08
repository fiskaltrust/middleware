using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories
{
    public abstract class BaseAzureTableStorageRepository<TKey, TAzureEntity, TStorageEntity>
        where TAzureEntity : class, ITableEntity, new()
        where TStorageEntity : class
    {
        protected readonly TableClient _tableClient;

        public BaseAzureTableStorageRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient, string storageEntityName)
        {
            var tableName = $"x{queueConfig.QueueId.ToString().Replace("-", "")}{storageEntityName}";
            _tableClient = tableServiceClient.GetTableClient(tableName);
        }

        public virtual async Task<IEnumerable<TStorageEntity>> GetAsync()
        {
            var result = _tableClient.QueryAsync<TAzureEntity>();
            return await Task.FromResult(result.Select(MapToStorageEntity).ToEnumerable());
        }

        public virtual async Task<TStorageEntity> GetAsync(TKey id)
        {
            var entity = await RetrieveAsync(id).ConfigureAwait(false);
            return MapToStorageEntity(entity);
        }

        public virtual async Task InsertAsync(TStorageEntity storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }
        public virtual async Task<TStorageEntity> RemoveAsync(TKey key)
        {
            var entity = await RetrieveAsync(key).ConfigureAwait(false);
            if (entity != null)
            {
                await _tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
            }

            return MapToStorageEntity(entity);
        }

        protected abstract TKey GetIdForEntity(TStorageEntity entity);

        protected abstract void EntityUpdated(TStorageEntity entity);

        protected abstract TStorageEntity MapToStorageEntity(TAzureEntity entity);

        protected abstract TAzureEntity MapToAzureEntity(TStorageEntity entity);

        protected async Task<TAzureEntity> RetrieveAsync(TKey id)
        {
            var result = _tableClient.QueryAsync<TAzureEntity>(x => x.RowKey == id.ToString());
            return await result.FirstOrDefaultAsync();
        }
    }
}
