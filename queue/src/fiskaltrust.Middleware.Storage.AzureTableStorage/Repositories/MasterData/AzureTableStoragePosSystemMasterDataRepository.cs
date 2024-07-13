using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.MasterData;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.MasterData
{
    public class AzureTableStoragePosSystemMasterDataRepository : BaseAzureTableStorageRepository<Guid, AzureTableStoragePosSystemMasterData, PosSystemMasterData>, IMasterDataRepository<PosSystemMasterData>
    {
        public AzureTableStoragePosSystemMasterDataRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = nameof(PosSystemMasterData);

        public async Task ClearAsync() => await ClearTableAsync().ConfigureAwait(false);

        public async Task CreateAsync(PosSystemMasterData entity) => await InsertAsync(entity).ConfigureAwait(false);

        protected override void EntityUpdated(PosSystemMasterData entity) { }

        protected override Guid GetIdForEntity(PosSystemMasterData entity) => entity.PosSystemId;

        protected override AzureTableStoragePosSystemMasterData MapToAzureEntity(PosSystemMasterData src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStoragePosSystemMasterData
            {
                PartitionKey = src.PosSystemId.ToString(),
                RowKey = src.PosSystemId.ToString(),
                BaseCurrency = src.BaseCurrency,
                Brand = src.Brand,
                Model = src.Model,
                PosSystemId = src.PosSystemId,
                SoftwareVersion = src.SoftwareVersion,
                Type = src.Type
            };
        }

        protected override PosSystemMasterData MapToStorageEntity(AzureTableStoragePosSystemMasterData src)
        {
            if (src == null)
            {
                return null;
            }

            return new PosSystemMasterData
            {
                BaseCurrency = src.BaseCurrency,
                Brand = src.Brand,
                Model = src.Model,
                PosSystemId = src.PosSystemId,
                SoftwareVersion = src.SoftwareVersion,
                Type = src.Type
            };
        }
    }
}
