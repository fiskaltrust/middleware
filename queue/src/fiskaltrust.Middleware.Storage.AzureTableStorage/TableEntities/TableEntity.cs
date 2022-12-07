using System;
using Azure;
using Azure.Data.Tables;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities
{
    public class BaseTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
