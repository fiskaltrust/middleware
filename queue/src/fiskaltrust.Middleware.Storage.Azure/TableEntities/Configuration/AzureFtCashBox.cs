using System;
using Microsoft.Azure.Cosmos.Table;

namespace fiskaltrust.Middleware.Storage.Azure.TableEntities.Configuration
{
    public class AzureFtCashBox : TableEntity
    {
        public Guid ftCashBoxId { get; set; }
        public long TimeStamp { get; set; }
    }
}
