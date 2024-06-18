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

        protected override AzureTableStorageFailedFinishTransaction MapToAzureEntity(FailedFinishTransaction entity) => Mapper.Map(entity);

        protected override FailedFinishTransaction MapToStorageEntity(AzureTableStorageFailedFinishTransaction entity) => Mapper.Map(entity);
    }
}

