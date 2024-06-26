﻿using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories
{
    public class AzureTableStorageReceiptJournalRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtReceiptJournal, ftReceiptJournal>, IReceiptJournalRepository, IMiddlewareRepository<ftReceiptJournal>
    {
        public AzureTableStorageReceiptJournalRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "ReceiptJournal";

        protected override void EntityUpdated(ftReceiptJournal entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftReceiptJournal entity) => entity.ftReceiptJournalId;

        protected override AzureTableStorageFtReceiptJournal MapToAzureEntity(ftReceiptJournal src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtReceiptJournal
            {
                PartitionKey = Mapper.GetHashString(src.TimeStamp),
                RowKey = src.ftReceiptJournalId.ToString(),
                ftReceiptJournalId = src.ftReceiptJournalId,
                ftQueueId = src.ftQueueId,
                ftQueueItemId = src.ftQueueItemId,
                ftReceiptHash = src.ftReceiptHash,
                ftReceiptMoment = src.ftReceiptMoment.ToUniversalTime(),
                ftReceiptNumber = src.ftReceiptNumber,
                ftReceiptTotal = Convert.ToDouble(src.ftReceiptTotal),
                TimeStamp = src.TimeStamp
            };
        }

        protected override ftReceiptJournal MapToStorageEntity(AzureTableStorageFtReceiptJournal src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftReceiptJournal
            {
                ftReceiptJournalId = src.ftReceiptJournalId,
                ftQueueId = src.ftQueueId,
                ftQueueItemId = src.ftQueueItemId,
                ftReceiptHash = src.ftReceiptHash,
                ftReceiptMoment = src.ftReceiptMoment,
                ftReceiptNumber = src.ftReceiptNumber,
                ftReceiptTotal = Convert.ToDecimal(src.ftReceiptTotal),
                TimeStamp = src.TimeStamp
            };
        }

        public IAsyncEnumerable<ftReceiptJournal> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = base.GetEntriesOnOrAfterTimeStampAsync(fromInclusive).OrderBy(x => x.TimeStamp);
            return take.HasValue ? result.Take(take.Value) : result;
        }
    }
}

