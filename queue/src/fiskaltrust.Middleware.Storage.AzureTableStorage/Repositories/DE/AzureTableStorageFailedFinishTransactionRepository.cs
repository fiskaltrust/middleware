using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Helpers;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.DE
{
    public class AzureTableStorageFailedFinishTransactionRepository : BaseAzureTableStorageRepository<string, TableEntity, FailedFinishTransaction>, IPersistentTransactionRepository<FailedFinishTransaction>
    {
        public AzureTableStorageFailedFinishTransactionRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = nameof(FailedFinishTransaction);

        public async Task InsertOrUpdateTransactionAsync(FailedFinishTransaction transaction) => await InsertOrUpdateAsync(transaction).ConfigureAwait(false);

        public async Task<bool> ExistsAsync(string cbReceiptReference) => await GetAsync(ConversionHelper.ReplaceNonAllowedKeyCharacters(cbReceiptReference)).ConfigureAwait(false) != null;

        protected override void EntityUpdated(FailedFinishTransaction entity) { }

        protected override string GetIdForEntity(FailedFinishTransaction entity) => ConversionHelper.ReplaceNonAllowedKeyCharacters(entity.cbReceiptReference);

        public override Task<FailedFinishTransaction> GetAsync(string id) => base.GetAsync(ConversionHelper.ReplaceNonAllowedKeyCharacters(id));

        public async Task InsertOrUpdateAsync(FailedFinishTransaction storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        protected override TableEntity MapToAzureEntity(FailedFinishTransaction src)
        {
            if (src == null)
            {
                return null;
            }

            var entity = new TableEntity(Mapper.GetHashString(src.FinishMoment.Ticks), GetIdForEntity(src))
            {
                { nameof(FailedFinishTransaction.cbReceiptReference), src.cbReceiptReference },
                { nameof(FailedFinishTransaction.CashBoxIdentification), src.CashBoxIdentification },
                { nameof(FailedFinishTransaction.FinishMoment), src.FinishMoment.ToUniversalTime() },
                { nameof(FailedFinishTransaction.ftQueueItemId), src.ftQueueItemId },
                { nameof(FailedFinishTransaction.TransactionNumber), src.TransactionNumber?.ToString() },
            };

            entity.SetOversized(nameof(FailedFinishTransaction.Request), src.Request);

            return entity;
        }

        protected override FailedFinishTransaction MapToStorageEntity(TableEntity src)
        {
            if (src == null)
            {
                return null;
            }

            var transactionNumber = src.GetString(nameof(FailedFinishTransaction.TransactionNumber));

            return new FailedFinishTransaction
            {
                cbReceiptReference = src.GetString(nameof(FailedFinishTransaction.cbReceiptReference)),
                CashBoxIdentification = src.GetString(nameof(FailedFinishTransaction.CashBoxIdentification)),
                FinishMoment = src.GetDateTime(nameof(FailedFinishTransaction.FinishMoment)).GetValueOrDefault(),
                ftQueueItemId = src.GetGuid(nameof(FailedFinishTransaction.ftQueueItemId)).GetValueOrDefault(),
                Request = src.GetOversized(nameof(FailedFinishTransaction.Request)),
                TransactionNumber = transactionNumber == null ? null : Convert.ToInt64(transactionNumber)
            };
        }
    }
}

