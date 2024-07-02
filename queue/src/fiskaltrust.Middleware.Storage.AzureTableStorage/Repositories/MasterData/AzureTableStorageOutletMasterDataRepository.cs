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
    public class AzureTableStorageOutletMasterDataRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageOutletMasterData, OutletMasterData>, IMasterDataRepository<OutletMasterData>
    {
        public AzureTableStorageOutletMasterDataRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = nameof(OutletMasterData);

        public async Task ClearAsync()
        {
            var result = _tableClient.QueryAsync<TableEntity>(select: new List<string>() { "PartitionKey", "RowKey" });
            await foreach (var item in result)
            {
                await _tableClient.DeleteEntityAsync(item.PartitionKey, item.RowKey);
            }
        }

        public async Task CreateAsync(OutletMasterData entity) => await InsertAsync(entity).ConfigureAwait(false);

        protected override void EntityUpdated(OutletMasterData entity) { }

        protected override Guid GetIdForEntity(OutletMasterData entity) => entity.OutletId;

        protected override AzureTableStorageOutletMasterData MapToAzureEntity(OutletMasterData src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageOutletMasterData
            {
                PartitionKey = src.OutletId.ToString(),
                RowKey = src.OutletId.ToString(),
                OutletId = src.OutletId,
                OutletName = src.OutletName,
                Street = src.Street,
                Zip = src.Zip,
                City = src.City,
                Country = src.Country,
                VatId = src.VatId,
                LocationId = src.LocationId
            };
        }


        protected override OutletMasterData MapToStorageEntity(AzureTableStorageOutletMasterData src)
        {
            if (src == null)
            {
                return null;
            }

            return new OutletMasterData
            {
                OutletId = src.OutletId,
                OutletName = src.OutletName,
                Street = src.Street,
                Zip = src.Zip,
                City = src.City,
                Country = src.Country,
                VatId = src.VatId,
                LocationId = src.LocationId
            };
        }
    }
}
