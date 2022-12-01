using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories.DE
{
    public class AzureJournalDERepository : BaseAzureTableRepository<Guid, AzureFtJournalDE, ftJournalDE>, IJournalDERepository, IMiddlewareRepository<ftJournalDE>, IMiddlewareJournalDERepository
    {
        private const string JOURNALDE_BLOB_CONTAINER = "ftjournalde";
        private readonly QueueConfiguration _queueConfig;
        private readonly BlobContainerClient _blobContainerClient;

        public AzureJournalDERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient, BlobServiceClient blobServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftJournalDE))
        {
            _queueConfig = queueConfig;
            _blobContainerClient = blobServiceClient.GetBlobContainerClient(JOURNALDE_BLOB_CONTAINER);
        }

        protected override void EntityUpdated(ftJournalDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalDE entity) => entity.ftJournalDEId;

        protected override AzureFtJournalDE MapToAzureEntity(ftJournalDE entity) => Mapper.Map(entity);

        protected override ftJournalDE MapToStorageEntity(AzureFtJournalDE entity) => Mapper.Map(entity);

        public IAsyncEnumerable<ftJournalDE> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = base.GetEntriesOnOrAfterTimeStampAsync(fromInclusive).OrderBy(x => x.TimeStamp);
            return take.HasValue ? result.Take(take.Value) : result;
        }

        public IAsyncEnumerable<ftJournalDE> GetByFileName(string fileName)
        {
            var result = _tableClient.QueryAsync<AzureFtJournalDE>(filter: TableClient.CreateQueryFilter($"FileName eq {fileName}"));
            return result.Select(MapToStorageEntity);
        }

        public override async Task InsertAsync(ftJournalDE entity)
        {
            if (!string.IsNullOrEmpty(entity.FileContentBase64))
            {
                var blob = _blobContainerClient.GetBlobClient($"{_queueConfig.QueueId}/{entity.ftJournalDEId}/{entity.FileName}.{entity.FileExtension}");

                var file = Convert.FromBase64String(entity.FileContentBase64);
                using var ms = new MemoryStream(file);
                await blob.UploadAsync(ms);
            }

            await base.InsertAsync(new ftJournalDE
            {
                FileExtension = entity.FileExtension,
                FileName = entity.FileName,
                ftJournalDEId = entity.ftJournalDEId,
                ftQueueId = entity.ftQueueId,
                ftQueueItemId = entity.ftQueueItemId,
                Number = entity.Number,
                TimeStamp = entity.TimeStamp,
                FileContentBase64 = null
            });
        }

        public override async Task<ftJournalDE> GetAsync(Guid id)
        {
            var entity = await base.GetAsync(id);
            entity.FileContentBase64 = await DownloadJournalDEFromBlobAsync(entity);

            return entity;
        }

        public override async Task<IEnumerable<ftJournalDE>> GetAsync()
        {
            var journals = await base.GetAsync();
#if NET461 || NETSTANDARD2_0 || NETSTANDARD2_1
            foreach (var journal in journals)
            {
                journal.FileContentBase64 = await DownloadJournalDEFromBlobAsync(journal);
            }
#else
            await Parallel.ForEachAsync(journals, async (journal, _) => journal.FileContentBase64 = await DownloadJournalDEFromBlobAsync(journal));
#endif
            return journals;
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
                using var blobStream = await blob.OpenReadAsync();
                using var base64Stream = new CryptoStream(blobStream, new ToBase64Transform(), CryptoStreamMode.Read);
                using var streamReader = new StreamReader(base64Stream);
                return streamReader.ReadToEnd();
            }

            return null;
        }
    }
}
