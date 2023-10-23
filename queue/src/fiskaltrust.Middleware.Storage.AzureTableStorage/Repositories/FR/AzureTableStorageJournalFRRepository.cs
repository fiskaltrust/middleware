﻿using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.FR
{
    public class AzureTableStorageJournalFRRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtJournalFR, ftJournalFR>, IJournalFRRepository, IMiddlewareRepository<ftJournalFR>
    {
        public AzureTableStorageJournalFRRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftJournalFR)) { }

        protected override void EntityUpdated(ftJournalFR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalFR entity) => entity.ftJournalFRId;

        protected override AzureTableStorageFtJournalFR MapToAzureEntity(ftJournalFR entity) => Mapper.Map(entity);

        protected override ftJournalFR MapToStorageEntity(AzureTableStorageFtJournalFR entity) => Mapper.Map(entity);

        public IAsyncEnumerable<ftJournalFR> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = base.GetEntriesOnOrAfterTimeStampAsync(fromInclusive).OrderBy(x => x.TimeStamp);
            return take.HasValue ? result.Take(take.Value) : result;
        }
    }
}

