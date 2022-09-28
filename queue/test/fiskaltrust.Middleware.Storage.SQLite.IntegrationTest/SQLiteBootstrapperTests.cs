using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Castle.Core.Logging;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Storage.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Storage.SQLite.IntegrationTest
{
    public class SQLiteBootstrapperTests
    {
        [Fact]
        public void InitDatabaseWithConfig_ShouldSucceed()
        {
            var configurationFile = Path.Combine(Directory.GetCurrentDirectory(), "configuration.json");
            var json = File.ReadAllText(configurationFile);
            var configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            var queues = JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(configuration["ftQueues"]));
            var azureQueue = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(queues[0]));
            var queueConfiguration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(azureQueue["Configuration"]));

            queueConfiguration.Add("servicefolder", Directory.GetCurrentDirectory());
            var sqliteBootstrapper = new SQLiteStorageBootstrapper(Guid.Parse(azureQueue["Id"].ToString()), queueConfiguration, new SQLiteStorageConfiguration(), Mock.Of<ILogger<IMiddlewareBootstrapper>>());

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            sqliteBootstrapper.ConfigureStorageServices(serviceCollection);
        }
    }
}
