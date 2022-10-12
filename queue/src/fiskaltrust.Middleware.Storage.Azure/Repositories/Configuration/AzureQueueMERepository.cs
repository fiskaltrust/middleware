﻿using System;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories.Configuration
{
    public class AzureQueueMERepository : BaseAzureTableRepository<Guid, AzureFtQueueME, ftQueueME>
    {
        public AzureQueueMERepository(Guid queueId, string connectionString)
            : base(queueId, connectionString, nameof(ftQueueME)) { }

        protected override void EntityUpdated(ftQueueME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueME entity) => entity.ftQueueMEId;

        protected override AzureFtQueueME MapToAzureEntity(ftQueueME entity) => Mapper.Map(entity);

        protected override ftQueueME MapToStorageEntity(AzureFtQueueME entity) => Mapper.Map(entity);
    }
}