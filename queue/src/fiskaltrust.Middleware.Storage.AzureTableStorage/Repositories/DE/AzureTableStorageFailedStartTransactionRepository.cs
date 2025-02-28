﻿using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Helpers;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.DE
{
    public class AzureTableStorageFailedStartTransactionRepository : BaseAzureTableStorageRepository<string, TableEntity, FailedStartTransaction>, IPersistentTransactionRepository<FailedStartTransaction>
    {
        public AzureTableStorageFailedStartTransactionRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = nameof(FailedStartTransaction);

        public async Task InsertOrUpdateTransactionAsync(FailedStartTransaction transaction) => await InsertOrUpdateAsync(transaction).ConfigureAwait(false);

        public async Task<bool> ExistsAsync(string cbReceiptReference) => await GetAsync(ConversionHelper.ReplaceNonAllowedKeyCharacters(cbReceiptReference)).ConfigureAwait(false) != null;

        protected override void EntityUpdated(FailedStartTransaction entity) { }

        protected override string GetIdForEntity(FailedStartTransaction entity) => ConversionHelper.ReplaceNonAllowedKeyCharacters(entity.cbReceiptReference);

        public override Task<FailedStartTransaction> GetAsync(string id) => base.GetAsync(ConversionHelper.ReplaceNonAllowedKeyCharacters(id));

        public override async Task<FailedStartTransaction> RemoveAsync(string key)
        {
            var entity = await RetrieveAsync(ConversionHelper.ReplaceNonAllowedKeyCharacters(key)).ConfigureAwait(false);
            if (entity != null)
            {
                await _tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
            }

            return MapToStorageEntity(entity);
        }

        public async Task InsertOrUpdateAsync(FailedStartTransaction storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        protected override TableEntity MapToAzureEntity(FailedStartTransaction src)
        {
            if (src == null)
            {
                return null;
            }

            var entity = new TableEntity(Mapper.GetHashString(src.StartMoment.Ticks), GetIdForEntity(src))
            {
                { nameof(FailedStartTransaction.cbReceiptReference), src.cbReceiptReference },
                { nameof(FailedStartTransaction.CashBoxIdentification), src.CashBoxIdentification },
                { nameof(FailedStartTransaction.StartMoment), src.StartMoment.ToUniversalTime() },
                { nameof(FailedStartTransaction.ftQueueItemId), src.ftQueueItemId },
            };

            entity.SetOversized(nameof(FailedStartTransaction.Request), src.Request);

            return entity;
        }

        protected override FailedStartTransaction MapToStorageEntity(TableEntity src)
        {
            if (src == null)
            {
                return null;
            }

            return new FailedStartTransaction
            {
                cbReceiptReference = src.GetString(nameof(FailedStartTransaction.cbReceiptReference)),
                CashBoxIdentification = src.GetString(nameof(FailedStartTransaction.CashBoxIdentification)),
                StartMoment = src.GetDateTime(nameof(FailedStartTransaction.StartMoment)).GetValueOrDefault(),
                ftQueueItemId = src.GetGuid(nameof(FailedStartTransaction.ftQueueItemId)).GetValueOrDefault(),
                Request = src.GetOversized(nameof(FailedStartTransaction.Request)),
            };
        }
    }
}


