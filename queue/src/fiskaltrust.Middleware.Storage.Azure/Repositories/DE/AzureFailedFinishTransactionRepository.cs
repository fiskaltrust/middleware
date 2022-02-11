using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories.DE
{
    public class AzureFailedFinishTransactionRepository : BaseAzureTableRepository<string, AzureFailedFinishTransaction, FailedFinishTransaction>, IPersistentTransactionRepository<FailedFinishTransaction>
    {
        public AzureFailedFinishTransactionRepository(Guid queueId, string connectionString)
            : base(queueId, connectionString, nameof(ftJournalDE)) { }

        public async Task InsertOrUpdateTransactionAsync(FailedFinishTransaction transaction) => await InsertOrUpdateAsync(transaction).ConfigureAwait(false);

        public async Task<bool> ExistsAsync(string cbReceiptReference) => await GetAsync(cbReceiptReference).ConfigureAwait(false) != null;

        protected override void EntityUpdated(FailedFinishTransaction entity) { }

        protected override string GetIdForEntity(FailedFinishTransaction entity) => entity.cbReceiptReference;

        protected override AzureFailedFinishTransaction MapToAzureEntity(FailedFinishTransaction entity) => Mapper.Map(entity);

        protected override FailedFinishTransaction MapToStorageEntity(AzureFailedFinishTransaction entity) => Mapper.Map(entity);
    }
}
