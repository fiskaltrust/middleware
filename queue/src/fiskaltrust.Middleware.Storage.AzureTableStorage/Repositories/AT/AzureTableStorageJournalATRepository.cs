using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.AT
{
    public class AzureTableStorageJournalATRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtJournalAT, ftJournalAT>, IJournalATRepository, IMiddlewareRepository<ftJournalAT>
    {
        public AzureTableStorageJournalATRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "JournalAT";

        protected override void EntityUpdated(ftJournalAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalAT entity) => entity.ftJournalATId;

        protected override AzureTableStorageFtJournalAT MapToAzureEntity(ftJournalAT src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtJournalAT
            {
                PartitionKey = Mapper.GetHashString(src.TimeStamp),
                RowKey = src.ftJournalATId.ToString(),
                ftJournalATId = src.ftJournalATId,
                ftQueueId = src.ftQueueId,
                ftSignaturCreationUnitId = src.ftSignaturCreationUnitId,
                Number = src.Number,
                JWSHeaderBase64url = src.JWSHeaderBase64url,
                JWSPayloadBase64url = src.JWSPayloadBase64url,
                JWSSignatureBase64url = src.JWSSignatureBase64url,
                TimeStamp = src.TimeStamp
            };
        }

        protected override ftJournalAT MapToStorageEntity(AzureTableStorageFtJournalAT src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftJournalAT
            {
                ftJournalATId = src.ftJournalATId,
                ftQueueId = src.ftQueueId,
                ftSignaturCreationUnitId = src.ftSignaturCreationUnitId,
                Number = src.Number,
                JWSHeaderBase64url = src.JWSHeaderBase64url,
                JWSPayloadBase64url = src.JWSPayloadBase64url,
                JWSSignatureBase64url = src.JWSSignatureBase64url,
                TimeStamp = src.TimeStamp
            };
        }

        public IAsyncEnumerable<ftJournalAT> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = base.GetEntriesOnOrAfterTimeStampAsync(fromInclusive).OrderBy(x => x.TimeStamp);
            return take.HasValue ? result.Take(take.Value) : result;
        }
    }
}