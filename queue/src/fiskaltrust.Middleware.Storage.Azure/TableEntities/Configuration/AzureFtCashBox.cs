using System;

namespace fiskaltrust.Middleware.Storage.Azure.TableEntities.Configuration
{
    public class AzureFtCashBox : TableEntity
    {
        public Guid ftCashBoxId { get; set; }
        public long TimeStamp { get; set; }
    }
}
