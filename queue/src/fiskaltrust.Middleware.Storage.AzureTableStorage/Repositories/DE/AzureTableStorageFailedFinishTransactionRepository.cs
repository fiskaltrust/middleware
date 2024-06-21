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
    public class AzureTableStorageFailedFinishTransactionRepository : BaseAzureTableStorageRepository<string, AzureTableStorageFailedFinishTransaction, FailedFinishTransaction>, IPersistentTransactionRepository<FailedFinishTransaction>
    {
        public AzureTableStorageFailedFinishTransactionRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = nameof(FailedFinishTransaction);

        public async Task InsertOrUpdateTransactionAsync(FailedFinishTransaction transaction) => await InsertOrUpdateAsync(transaction).ConfigureAwait(false);

        public async Task<bool> ExistsAsync(string cbReceiptReference) => await GetAsync(cbReceiptReference).ConfigureAwait(false) != null;

        protected override void EntityUpdated(FailedFinishTransaction entity) { }

        protected override string GetIdForEntity(FailedFinishTransaction entity) => entity.cbReceiptReference;

        protected override AzureTableStorageFailedFinishTransaction MapToAzureEntity(FailedFinishTransaction src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFailedFinishTransaction
            {
                PartitionKey = Mapper.GetHashString(src.FinishMoment.Ticks),
                RowKey = src.cbReceiptReference,
                cbReceiptReference = src.cbReceiptReference,
                CashBoxIdentification = src.CashBoxIdentification,
                FinishMoment = src.FinishMoment.ToUniversalTime(),
                ftQueueItemId = src.ftQueueItemId,
                Request = src.Request,
                TransactionNumber = src.TransactionNumber?.ToString()
            };
        }

        protected override FailedFinishTransaction MapToStorageEntity(AzureTableStorageFailedFinishTransaction src)
        {
            if (src == null)
            {
                return null;
            }

            return new FailedFinishTransaction
            {
                cbReceiptReference = src.cbReceiptReference,
                CashBoxIdentification = src.CashBoxIdentification,
                FinishMoment = src.FinishMoment,
                ftQueueItemId = src.ftQueueItemId,
                Request = src.Request,
                TransactionNumber = src.TransactionNumber == null ? null : Convert.ToInt64(src.TransactionNumber)
            };
        }
    }
}

