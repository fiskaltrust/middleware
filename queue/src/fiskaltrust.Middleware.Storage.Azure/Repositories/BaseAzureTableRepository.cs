using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using Microsoft.Azure.Cosmos.Table;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories
{
    public abstract class BaseAzureTableRepository<TKey, TAzureEntity, TStorageEntity> : BaseAzureRepository
        where TAzureEntity : class, ITableEntity, new()
        where TStorageEntity : class
    {
        private readonly CloudTableClient _tableClient;
        private readonly string _tableName;

        private bool _tableCreated = false;

        public BaseAzureTableRepository(Guid queueId, string connStr, string storageEntityName) : base(connStr)
        {
            _tableClient = CloudStorageAccount.CreateCloudTableClient();
            _tableName = $"x{queueId.ToString().Replace("-", "")}{storageEntityName}";
        }

        public virtual async Task<IEnumerable<TStorageEntity>> GetAsync()
        {
            if (!_tableCreated)
            {
                _tableCreated = await CreateTableAsync(_tableName).ConfigureAwait(false);
            }

            var entities = await GetAllAsync().ToListAsync().ConfigureAwait(false);
            var result = entities.Select(MapToStorageEntity);

            return await Task.FromResult(result).ConfigureAwait(false);
        }

        public virtual async Task<TStorageEntity> GetAsync(TKey id)
        {
            if (!_tableCreated)
            {
                _tableCreated = await CreateTableAsync(_tableName).ConfigureAwait(false);
            }

            var entity = await RetrieveAsync(id).ConfigureAwait(false);
            var result = MapToStorageEntity(entity);

            return result;
        }

        public virtual async Task InsertAsync(TStorageEntity storageEntity)
        {
            if (!_tableCreated)
            {
                _tableCreated = await CreateTableAsync(_tableName).ConfigureAwait(false);
            }

            var entityId = GetIdForEntity(storageEntity);
            var existingEntity = await GetAsync(entityId).ConfigureAwait(false);
            if (existingEntity != null)
            {
                throw new Exception("The key already exists");
            }

            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await InsertOrMergeAsync(entity, _tableName).ConfigureAwait(false);
        }

        public async Task InsertOrUpdateAsync(TStorageEntity storageEntity)
        {
            if (!_tableCreated)
            {
                _tableCreated = await CreateTableAsync(_tableName).ConfigureAwait(false);
            }

            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await InsertOrMergeAsync(entity, _tableName).ConfigureAwait(false);
        }

        public async Task<TStorageEntity> RemoveAsync(TKey key)
        {
            if (!_tableCreated)
            {
                _tableCreated = await CreateTableAsync(_tableName).ConfigureAwait(false);
            }

            var entity = await RetrieveAsync(key).ConfigureAwait(false);
            if (entity != null)
            {
                var table = _tableClient.GetTableReference(_tableName);
                var delteOperation = TableOperation.Delete(entity);
                table.Execute(delteOperation);
            }

            return MapToStorageEntity(entity);
        }

        public virtual IAsyncEnumerable<TStorageEntity> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive)
        {
            var tableQuery = new TableQuery<TAzureEntity>();
            tableQuery = tableQuery.Where(TableQuery.CombineFilters(
              TableQuery.GenerateFilterConditionForLong("TimeStamp", QueryComparisons.GreaterThanOrEqual, fromInclusive),
              TableOperators.And,
              TableQuery.GenerateFilterConditionForLong("TimeStamp", QueryComparisons.LessThanOrEqual, toInclusive)));
            return GetAllByTableFilterAsync(tableQuery);
        }

        public IAsyncEnumerable<TStorageEntity> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive)
        {
            var tableQuery = new TableQuery<TAzureEntity>();
            tableQuery = tableQuery.Where(TableQuery.GenerateFilterConditionForLong("TimeStamp", QueryComparisons.GreaterThanOrEqual, fromInclusive));
            return GetAllByTableFilterAsync(tableQuery);
        }

        protected abstract TKey GetIdForEntity(TStorageEntity entity);

        protected abstract void EntityUpdated(TStorageEntity entity);

        protected abstract TStorageEntity MapToStorageEntity(TAzureEntity entity);

        protected abstract TAzureEntity MapToAzureEntity(TStorageEntity entity);

        protected async IAsyncEnumerable<TAzureEntity> GetAllAsync(string filter = "")
        {
            var table = _tableClient.GetTableReference(_tableName);
            TableContinuationToken token = null;
            do
            {
                var query = new TableQuery<TAzureEntity>();
                if (!string.IsNullOrEmpty(filter))
                {
                    query = query.Where(filter);
                }

                var queryResult = await table.ExecuteQuerySegmentedAsync(query, token).ConfigureAwait(false);
                foreach (var item in queryResult.Results)
                {
                    yield return item;
                }
                token = queryResult.ContinuationToken;
            } while (token != null);
        }

        protected async IAsyncEnumerable<TStorageEntity> GetAllByTableFilterAsync(TableQuery<TAzureEntity> query)
        {
            var table = _tableClient.GetTableReference(_tableName);
            TableContinuationToken token = null;
            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(query, token).ConfigureAwait(false);
                foreach (var item in queryResult.Results)
                {
                    yield return MapToStorageEntity(item);
                }
                token = queryResult.ContinuationToken;
            } while (token != null);
        }

        protected async Task ClearTableAsync()
        {
            var table = _tableClient.GetTableReference(_tableName);

            if (await table.ExistsAsync().ConfigureAwait(false))
            {
                await foreach (var item in GetAllAsync().ConfigureAwait(false))
                {
                    await table.ExecuteAsync(TableOperation.Delete(item)).ConfigureAwait(false);                     
                }
            }
        }

        private async Task<TAzureEntity> RetrieveAsync(TKey id)
        {
            var table = _tableClient.GetTableReference(_tableName);
            var query = new TableQuery<TAzureEntity>();
            query = query.Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, id.ToString()));
            TableContinuationToken token = null;
            var queryResult = await table.ExecuteQuerySegmentedAsync(query, token).ConfigureAwait(false);
            return queryResult.Results.FirstOrDefault();
        }

        private async Task<bool> CreateTableAsync(string tableName)
        {
            var table = _tableClient.GetTableReference(tableName);
            return await table.CreateIfNotExistsAsync().ConfigureAwait(false);
        }

        private async Task<bool> InsertOrMergeAsync(TAzureEntity azureEntity, string tableName)
        {
            var table = _tableClient.GetTableReference(tableName);

            var insertOperation = TableOperation.InsertOrMerge(azureEntity);

            var result = await table.ExecuteAsync(insertOperation).ConfigureAwait(false);

            return (result.HttpStatusCode == (int) HttpStatusCode.OK)
                || (result.HttpStatusCode == (int) HttpStatusCode.NoContent);
        }
    }
}