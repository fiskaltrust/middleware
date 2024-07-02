using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.MasterData;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.MasterData
{
    public class AzureTableStorageAccountMasterDataRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageAccountMasterData, AccountMasterData>, IMasterDataRepository<AccountMasterData>
    {
        public AzureTableStorageAccountMasterDataRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = nameof(AccountMasterData);

        public async Task ClearAsync()
        {
            var result = _tableClient.QueryAsync<TableEntity>(select: new List<string>() { "PartitionKey", "RowKey" });
            await foreach (var item in result)
            {
                await _tableClient.DeleteEntityAsync(item.PartitionKey, item.RowKey);
            }
        }

        public async Task CreateAsync(AccountMasterData entity) => await InsertAsync(entity).ConfigureAwait(false);

        protected override void EntityUpdated(AccountMasterData entity) { }

        protected override Guid GetIdForEntity(AccountMasterData entity) => entity.AccountId;

        protected override AzureTableStorageAccountMasterData MapToAzureEntity(AccountMasterData src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageAccountMasterData
            {
                PartitionKey = src.AccountId.ToString(),
                RowKey = src.AccountId.ToString(),
                AccountId = src.AccountId,
                AccountName = src.AccountName,
                Street = src.Street,
                Zip = src.Zip,
                City = src.City,
                Country = src.Country,
                TaxId = src.TaxId,
                VatId = src.VatId
            };
        }

        protected override AccountMasterData MapToStorageEntity(AzureTableStorageAccountMasterData src)
        {
            if (src == null)
            {
                return null;
            }

            return new AccountMasterData
            {
                AccountId = src.AccountId,
                AccountName = src.AccountName,
                Street = src.Street,
                Zip = src.Zip,
                City = src.City,
                Country = src.Country,
                TaxId = src.TaxId,
                VatId = src.VatId
            };
        }
    }
}

