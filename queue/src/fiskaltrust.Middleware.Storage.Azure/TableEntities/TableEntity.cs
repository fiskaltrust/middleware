using System;
using Azure;
using Azure.Data.Tables;

namespace fiskaltrust.Middleware.Storage.Azure.TableEntities
{
    public class TableEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
