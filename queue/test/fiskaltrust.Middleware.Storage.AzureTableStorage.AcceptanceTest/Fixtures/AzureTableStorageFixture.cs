using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures
{
    public class AzureTableStorageFixture : IDisposable
    {
        public Guid QueueId = Guid.NewGuid();

        public AzureTableStorageFixture()
        {
            var dbMigrator = new DatabaseMigrator(Mock.Of<ILogger<IMiddlewareBootstrapper>>(), new TableServiceClient(Constants.AzureStorageConnectionString), new QueueConfiguration { QueueId = QueueId });
            dbMigrator.MigrateAsync().Wait();
        }

        public void CleanTable(string entityName)
        {
            var tableServiceClient = new TableServiceClient(Constants.AzureStorageConnectionString);
            var tableClient = tableServiceClient.GetTableClient($"x{QueueId.ToString().Replace("-", "")}{entityName}");
            var result = tableClient.Query<TableEntity>(select: new List<string>() { "PartitionKey", "RowKey" });
            foreach (var item in result)
            {
                tableClient.DeleteEntity(item.PartitionKey, item.RowKey);
            }
        }

        public void Dispose()
        {
            var tableServiceClient = new TableServiceClient(Constants.AzureStorageConnectionString);
            var tables = tableServiceClient.Query();
            foreach (var table in tables.Where(x => x.Name.StartsWith($"x{QueueId.ToString().Replace("-", "")}")))
            {
                tableServiceClient.DeleteTable(table.Name);
            }
        }
    }
}
