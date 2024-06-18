using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.DE
{
    public class AzureTableStorageJournalDERepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtJournalDE, ftJournalDE>, IJournalDERepository, IMiddlewareRepository<ftJournalDE>, IMiddlewareJournalDERepository
    {
        public const string BLOB_CONTAINER_NAME = "ftjournalde";
        public const string TABLE_NAME = "JournalDE";
        private readonly QueueConfiguration _queueConfig;
        private readonly BlobContainerClient _blobContainerClient;

        public AzureTableStorageJournalDERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient, BlobServiceClient blobServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME)
        {
            _queueConfig = queueConfig;
            _blobContainerClient = blobServiceClient.GetBlobContainerClient(BLOB_CONTAINER_NAME);
        }

        protected override void EntityUpdated(ftJournalDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalDE entity) => entity.ftJournalDEId;

        protected override AzureTableStorageFtJournalDE MapToAzureEntity(ftJournalDE entity) => Mapper.Map(entity);

        protected override ftJournalDE MapToStorageEntity(AzureTableStorageFtJournalDE entity) => Mapper.Map(entity);

        public IAsyncEnumerable<ftJournalDE> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = _tableClient
                .QueryAsync<AzureTableStorageFtJournalDE>(filter: TableClient.CreateQueryFilter($"PartitionKey le {Mapper.GetHashString(fromInclusive)}"))
                .SelectAwait(async x =>
                {
                    var entity = MapToStorageEntity(x);
                    entity.FileContentBase64 = await DownloadJournalDEFromBlobAsync(entity);
                    return entity;
                });


            return take.HasValue ? result.TakeLast(take.Value).OrderBy(x => x.TimeStamp) : result.OrderBy(x => x.TimeStamp);
        }

        public IAsyncEnumerable<ftJournalDE> GetByFileName(string fileName)
        {
            return _tableClient
                .QueryAsync<AzureTableStorageFtJournalDE>(x => x.FileName == fileName)
                .SelectAwait(async x =>
                {
                    var entity = MapToStorageEntity(x);
                    entity.FileContentBase64 = await DownloadJournalDEFromBlobAsync(entity);
                    return entity;
                });
        }

        public override async Task InsertAsync(ftJournalDE entity)
        {
            var fileContent = entity.FileContentBase64;
            try
            {
                entity.FileContentBase64 = null;
                await base.InsertAsync(entity);
            }
            finally
            {
                entity.FileContentBase64 = fileContent;
            }

            if (!string.IsNullOrEmpty(fileContent))
            {
                var blob = _blobContainerClient.GetBlobClient($"{_queueConfig.QueueId}/{entity.ftJournalDEId}/{entity.FileName}.{entity.FileExtension}");
                var file = Convert.FromBase64String(entity.FileContentBase64);
                using var ms = new MemoryStream(file);
                await blob.UploadAsync(ms, overwrite: true);
            }
        }

        public override async Task<ftJournalDE> GetAsync(Guid id)
        {
            var entity = await base.GetAsync(id);
            if (entity != null)
            {
                entity.FileContentBase64 = await DownloadJournalDEFromBlobAsync(entity);
            }

            return entity;
        }

        public override Task<IEnumerable<ftJournalDE>> GetAsync()
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtJournalDE>();
            return Task.FromResult(result.SelectAwait(async x =>
            {
                var entity = MapToStorageEntity(x);
                entity.FileContentBase64 = await DownloadJournalDEFromBlobAsync(entity);
                return entity;
            }).ToEnumerable());
        }

        public override async IAsyncEnumerable<ftJournalDE> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive)
        {
            var journals = base.GetByTimeStampRangeAsync(fromInclusive, toInclusive);
            await foreach (var journal in journals)
            {
                journal.FileContentBase64 = await DownloadJournalDEFromBlobAsync(journal);
                yield return journal;
            }
        }

        private async Task<string> DownloadJournalDEFromBlobAsync(ftJournalDE entity)
        {
            var blob = _blobContainerClient.GetBlobClient($"{_queueConfig.QueueId}/{entity.ftJournalDEId}/{entity.FileName}.{entity.FileExtension}");
            if (await blob.ExistsAsync())
            {
                using var ms = new MemoryStream();
                var content = await blob.DownloadToAsync(ms);
                var n = Convert.ToBase64String(ms.ToArray());
                return n;
            }

            return null;
        }
    }
}

