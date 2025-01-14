using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Helpers;
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

        public async Task<bool> ExistsAsync(string cbReceiptReference) => await GetAsync(ConversionHelper.ReplaceNonAllowedKeyCharacters(cbReceiptReference)).ConfigureAwait(false) != null;

        protected override void EntityUpdated(OpenTransaction entity) { }

        protected override string GetIdForEntity(OpenTransaction entity) => ConversionHelper.ReplaceNonAllowedKeyCharacters(entity.cbReceiptReference);

        public override Task<OpenTransaction> GetAsync(string id) => base.GetAsync(ConversionHelper.ReplaceNonAllowedKeyCharacters(id));

        public async Task InsertOrUpdateAsync(OpenTransaction storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        protected override AzureTableStorageOpenTransaction MapToAzureEntity(OpenTransaction src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageOpenTransaction
            {
                PartitionKey = Mapper.GetHashString(src.StartMoment.Ticks),
                RowKey = GetIdForEntity(src),
                cbReceiptReference = src.cbReceiptReference,
                StartMoment = src.StartMoment.ToUniversalTime(),
                StartTransactionSignatureBase64 = src.StartTransactionSignatureBase64,
                TransactionNumber = src.TransactionNumber.ToString()
            };
        }

        protected override OpenTransaction MapToStorageEntity(AzureTableStorageOpenTransaction src)
        {
            if (src == null)
            {
                return null;
            }

            return new OpenTransaction
            {
                cbReceiptReference = src.cbReceiptReference,
                StartMoment = src.StartMoment,
                StartTransactionSignatureBase64 = src.StartTransactionSignatureBase64,
                TransactionNumber = Convert.ToInt64(src.TransactionNumber)
            };
        }
    }
}

