using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.Azure.Mapping;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories
{
    public abstract class BaseAzureTableRepository<TKey, TAzureEntity, TStorageEntity>
        where TAzureEntity : class, ITableEntity, new()
        where TStorageEntity : class
    {
        protected readonly TableClient _tableClient;

        public BaseAzureTableRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient, string storageEntityName)
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
            var entityId = GetIdForEntity(storageEntity);
            var existingEntity = await GetAsync(entityId).ConfigureAwait(false);
            if (existingEntity != null)
            {
                throw new Exception("The key already exists");
            }

            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Merge);
        }

        public async Task InsertOrUpdateAsync(TStorageEntity storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Merge);
        }

        public async Task<TStorageEntity> RemoveAsync(TKey key)
        {
            var entity = await RetrieveAsync(key).ConfigureAwait(false);
            if (entity != null)
            {
                await _tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
            }

            return MapToStorageEntity(entity);
        }

        public virtual IAsyncEnumerable<TStorageEntity> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive)
        {
            var result = _tableClient.QueryAsync<TAzureEntity>(filter: TableClient.CreateQueryFilter($"PartitionKey ge {Mapper.GetHashString(fromInclusive)} and PartitionKey le {Mapper.GetHashString(toInclusive)}"));
            return result.Select(MapToStorageEntity).AsAsyncEnumerable();
        }

        public IAsyncEnumerable<TStorageEntity> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive)
        {
            var result = _tableClient.QueryAsync<TAzureEntity>(filter: TableClient.CreateQueryFilter($"PartitionKey ge {Mapper.GetHashString(fromInclusive)}"));
            return result.Select(MapToStorageEntity).AsAsyncEnumerable();
        }

        protected abstract TKey GetIdForEntity(TStorageEntity entity);

        protected abstract void EntityUpdated(TStorageEntity entity);

        protected abstract TStorageEntity MapToStorageEntity(TAzureEntity entity);

        protected abstract TAzureEntity MapToAzureEntity(TStorageEntity entity);

        protected async Task ClearTableAsync()
        {
            await _tableClient.DeleteAsync();
            await _tableClient.CreateAsync();
        }

        private async Task<TAzureEntity> RetrieveAsync(TKey id)
        {
            var result = _tableClient.QueryAsync<TAzureEntity>(filter: TableClient.CreateQueryFilter($"RowKey eq {id}"));
            return await result.FirstOrDefaultAsync();
        }
    }
}