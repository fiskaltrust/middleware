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
    public class AzureTableStorageAgencyMasterDataRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageAgencyMasterData, AgencyMasterData>, IMasterDataRepository<AgencyMasterData>
    {
        public AzureTableStorageAgencyMasterDataRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = nameof(AgencyMasterData);

        public async Task ClearAsync()
        {
            var result = _tableClient.QueryAsync<TableEntity>(select: new List<string>() { "PartitionKey", "RowKey" });
            await foreach (var item in result)
            {
                await _tableClient.DeleteEntityAsync(item.PartitionKey, item.RowKey);
            }
        }

        public async Task CreateAsync(AgencyMasterData entity) => await InsertAsync(entity).ConfigureAwait(false);

        protected override void EntityUpdated(AgencyMasterData entity) { }

        protected override Guid GetIdForEntity(AgencyMasterData entity) => entity.AgencyId;

        protected override AzureTableStorageAgencyMasterData MapToAzureEntity(AgencyMasterData src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageAgencyMasterData
            {
                PartitionKey = src.AgencyId.ToString(),
                RowKey = src.AgencyId.ToString(),
                Street = src.Street,
                Zip = src.Zip,
                City = src.City,
                Country = src.Country,
                VatId = src.VatId,
                AgencyId = src.AgencyId,
                Name = src.Name,
                TaxId = src.TaxId
            };
        }

        protected override AgencyMasterData MapToStorageEntity(AzureTableStorageAgencyMasterData src)
        {
            if (src == null)
            {
                return null;
            }

            return new AgencyMasterData
            {
                Street = src.Street,
                Zip = src.Zip,
                City = src.City,
                Country = src.Country,
                VatId = src.VatId,
                AgencyId = src.AgencyId,
                Name = src.Name,
                TaxId = src.TaxId
            };
        }
    }
}
