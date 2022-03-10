using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Storage.Ef;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Storage.EF.AcceptanceTest
{
    public class EfStorageBootstrapperTests
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

            var efBootSTrapper = new EfStorageBootstrapper(Guid.Parse(azureQueue["Id"].ToString()), queueConfiguration, EfStorageConfiguration.FromConfigurationDictionary(queueConfiguration), Mock.Of<ILogger<IMiddlewareBootstrapper>>());

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            efBootSTrapper.ConfigureStorageServices(serviceCollection);
        }
    }
}
