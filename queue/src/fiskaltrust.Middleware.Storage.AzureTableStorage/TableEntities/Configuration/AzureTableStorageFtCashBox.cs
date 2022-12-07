using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration
{
    public class AzureTableStorageFtCashBox : BaseTableEntity
    {
        public Guid ftCashBoxId { get; set; }
        public long TimeStamp { get; set; }
    }
}
