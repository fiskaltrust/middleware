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
    public class AzureFailedStartTransactionRepository : BaseAzureTableRepository<string, AzureFailedStartTransaction, FailedStartTransaction>, IPersistentTransactionRepository<FailedStartTransaction>
    {
        public AzureFailedStartTransactionRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(FailedStartTransaction)) { }

        public async Task InsertOrUpdateTransactionAsync(FailedStartTransaction transaction) => await InsertOrUpdateAsync(transaction).ConfigureAwait(false);

        public async Task<bool> ExistsAsync(string cbReceiptReference) => await GetAsync(cbReceiptReference).ConfigureAwait(false) != null;

        protected override void EntityUpdated(FailedStartTransaction entity) { }

        protected override string GetIdForEntity(FailedStartTransaction entity) => entity.cbReceiptReference;

        protected override AzureFailedStartTransaction MapToAzureEntity(FailedStartTransaction entity) => Mapper.Map(entity);

        protected override FailedStartTransaction MapToStorageEntity(AzureFailedStartTransaction entity) => Mapper.Map(entity);
    }
}
