using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.DE
{
    public class AzureTableStorageFailedFinishTransactionRepository : BaseAzureTableStorageRepository<string, TableEntity, FailedFinishTransaction>, IPersistentTransactionRepository<FailedFinishTransaction>
    {
        public AzureTableStorageFailedFinishTransactionRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = nameof(FailedFinishTransaction);

        public async Task InsertOrUpdateTransactionAsync(FailedFinishTransaction transaction) => await InsertOrUpdateAsync(transaction).ConfigureAwait(false);

        public async Task<bool> ExistsAsync(string cbReceiptReference) => await GetAsync(cbReceiptReference).ConfigureAwait(false) != null;

        protected override void EntityUpdated(FailedFinishTransaction entity) { }

        protected override string GetIdForEntity(FailedFinishTransaction entity) => entity.cbReceiptReference;

        protected override TableEntity MapToAzureEntity(FailedFinishTransaction src)
        {
            if (src == null)
            {
                return null;
            }

            var entity = new TableEntity(Mapper.GetHashString(src.FinishMoment.Ticks), src.cbReceiptReference)
            {
                { nameof(FailedFinishTransaction.cbReceiptReference), src.cbReceiptReference },
                { nameof(FailedFinishTransaction.CashBoxIdentification), src.CashBoxIdentification },
                { nameof(FailedFinishTransaction.FinishMoment), src.FinishMoment },
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

            return new FailedFinishTransaction
            {
                cbReceiptReference = src.GetString(nameof(FailedFinishTransaction.cbReceiptReference)),
                CashBoxIdentification = src.GetString(nameof(FailedFinishTransaction.CashBoxIdentification)),
                FinishMoment = src.GetDateTime(nameof(FailedFinishTransaction.FinishMoment)).GetValueOrDefault(),
                ftQueueItemId = src.GetGuid(nameof(FailedFinishTransaction.ftQueueItemId)).GetValueOrDefault(),
                Request = src.GetOversized(nameof(FailedFinishTransaction.Request)),
                TransactionNumber = src.GetInt64(nameof(FailedFinishTransaction.TransactionNumber))
            };
        }
    }
}

