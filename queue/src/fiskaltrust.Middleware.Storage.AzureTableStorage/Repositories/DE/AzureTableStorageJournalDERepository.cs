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

        protected override AzureTableStorageFtJournalDE MapToAzureEntity(ftJournalDE src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtJournalDE
            {
                PartitionKey = Mapper.GetHashString(src.TimeStamp),
                RowKey = src.ftJournalDEId.ToString(),
                ftJournalDEId = src.ftJournalDEId,
                ftQueueId = src.ftQueueId,
                Number = src.Number,
                ftQueueItemId = src.ftQueueItemId,
                FileContentBase64 = src.FileContentBase64,
                FileExtension = src.FileExtension,
                FileName = src.FileName,
                TimeStamp = src.TimeStamp
            };
        }

        protected override ftJournalDE MapToStorageEntity(AzureTableStorageFtJournalDE src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftJournalDE
            {
                ftJournalDEId = src.ftJournalDEId,
                ftQueueId = src.ftQueueId,
                ftQueueItemId = src.ftQueueItemId,
                FileContentBase64 = src.FileContentBase64,
                FileExtension = src.FileExtension,
                FileName = src.FileName,
                Number = src.Number,
                TimeStamp = src.TimeStamp,
            };
        }

        public IAsyncEnumerable<ftJournalDE> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtJournalDE>(filter: TableClient.CreateQueryFilter<AzureTableStorageFtJournalDE>(x => x.PartitionKey.CompareTo(Mapper.GetHashString(fromInclusive)) <= 0))
                .Select(MapToStorageEntity)
                .OrderBy(x => x.TimeStamp)
                .SelectAwait(async (journal) =>
                {
                    journal.FileContentBase64 = await DownloadJournalDEFromBlobAsync(journal);
                    return journal;
                });

            return take.HasValue ? result.Take(take.Value) : result;
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



        public async IAsyncEnumerable<ftJournalDE> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive)
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtJournalDE>(filter: 
                TableClient.CreateQueryFilter<AzureTableStorageFtJournalDE>(x => x.PartitionKey.CompareTo(Mapper.GetHashString(fromInclusive)) <= 0 && x.PartitionKey.CompareTo(Mapper.GetHashString(toInclusive)) >= 0));
            var journals = result.Select(MapToStorageEntity).OrderBy(x => x.TimeStamp);
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

