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
    public class AzureTableStorageOpenTransactionRepository : BaseAzureTableStorageRepository<string, AzureTableStorageOpenTransaction, OpenTransaction>, IPersistentTransactionRepository<OpenTransaction>
    {
        public AzureTableStorageOpenTransactionRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = nameof(OpenTransaction);

        public async Task InsertOrUpdateTransactionAsync(OpenTransaction transaction) => await InsertOrUpdateAsync(transaction).ConfigureAwait(false);

        public async Task<bool> ExistsAsync(string cbReceiptReference) => await GetAsync(cbReceiptReference).ConfigureAwait(false) != null;

        protected override void EntityUpdated(OpenTransaction entity) { }

        protected override string GetIdForEntity(OpenTransaction entity) => entity.cbReceiptReference;

        protected override AzureTableStorageOpenTransaction MapToAzureEntity(OpenTransaction entity) => Mapper.Map(entity);

        protected override OpenTransaction MapToStorageEntity(AzureTableStorageOpenTransaction entity) => Mapper.Map(entity);
    }
}

