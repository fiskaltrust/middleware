using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Dapper;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.storage.encryption.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MySqlConnector;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Storage.MySQL.IntegrationTest
{
    public class MySQLStorageBoostrapperTest
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

            var queueId = Guid.Parse(azureQueue["Id"].ToString());
            var dbName = queueId.ToString().Replace("-", string.Empty);

            var connectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRING_MYSQL_TESTS");
            queueConfiguration["connectionstring"] = Encryption.Encrypt(Encoding.UTF8.GetBytes(connectionString), queueId.ToByteArray());
            
            try
            {
                var efBootSTrapper = new MySQLBootstrapper(queueId, queueConfiguration, Mock.Of<ILogger<IMiddlewareBootstrapper>>());

                var serviceCollection = new ServiceCollection();
                serviceCollection.AddLogging();
                efBootSTrapper.ConfigureStorageServices(serviceCollection);
            }
            finally
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Execute($"DROP DATABASE IF EXISTS {dbName}");
                }
            }
        }
    }
}
