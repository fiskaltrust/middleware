using System;
using System.Collections.Generic;
using System.Reflection;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Queue.Bootstrapper;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Queue.AzureTableStorage
{
    public class PosBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            var logger = serviceCollection.BuildServiceProvider().GetRequiredService<ILogger<IMiddlewareBootstrapper>>();
            
            var storageConfiguration = AzureTableStorageConfiguration.FromConfigurationDictionary(Configuration);
            serviceCollection.AddSingleton(sp => storageConfiguration);
            
            var storageBootStrapper = new AzureTableStorageBootstrapper(Id, Configuration, storageConfiguration, logger);
            storageBootStrapper.ConfigureStorageServices(serviceCollection);

            Configuration.Add("assemblytype", typeof(PosBootstrapper));

            var queueBootstrapper = new QueueBootstrapper(Id, Configuration);
            queueBootstrapper.ConfigureServices(serviceCollection);
        }
    }
}
