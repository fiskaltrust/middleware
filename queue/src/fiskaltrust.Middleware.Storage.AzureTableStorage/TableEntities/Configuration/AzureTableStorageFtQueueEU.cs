using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration
{
    public class AzureTableStorageFtQueueEU : BaseTableEntity
    {
        public Guid ftQueueEUId { get; set; }
        public string CashBoxIdentification { get; set; }

        public long TimeStamp { get; set; }
    }
}
