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
    public class AzureTableStorageFailedStartTransactionRepository : BaseAzureTableStorageRepository<string, AzureTableStorageFailedStartTransaction, FailedStartTransaction>, IPersistentTransactionRepository<FailedStartTransaction>
    {
        public AzureTableStorageFailedStartTransactionRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }
            
        public const string TABLE_NAME = nameof(FailedStartTransaction);

        public async Task InsertOrUpdateTransactionAsync(FailedStartTransaction transaction) => await InsertOrUpdateAsync(transaction).ConfigureAwait(false);

        public async Task<bool> ExistsAsync(string cbReceiptReference) => await GetAsync(cbReceiptReference).ConfigureAwait(false) != null;

        protected override void EntityUpdated(FailedStartTransaction entity) { }

        protected override string GetIdForEntity(FailedStartTransaction entity) => entity.cbReceiptReference;

        protected override AzureTableStorageFailedStartTransaction MapToAzureEntity(FailedStartTransaction entity) => Mapper.Map(entity);

        protected override FailedStartTransaction MapToStorageEntity(AzureTableStorageFailedStartTransaction entity) => Mapper.Map(entity);
    }
}


