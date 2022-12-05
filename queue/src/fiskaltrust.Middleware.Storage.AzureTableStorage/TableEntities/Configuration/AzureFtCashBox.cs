using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration
{
    public class AzureFtCashBox : BaseTableEntity
    {
        public Guid ftCashBoxId { get; set; }
        public long TimeStamp { get; set; }
    }
}
