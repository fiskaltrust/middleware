using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Queue.Bootstrapper;
using fiskaltrust.Middleware.Storage.Ef;
using fiskaltrust.Middleware.Storage.EF;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Queue.EF
{
    public class PosBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            var logger = serviceCollection.BuildServiceProvider().GetRequiredService<ILogger<IMiddlewareBootstrapper>>();

            var storageConfiguration = EfStorageConfiguration.FromConfigurationDictionary(Configuration);
            serviceCollection.AddSingleton(sp => storageConfiguration);

            var storageBootStrapper = new EfStorageBootstrapper(Id, Configuration, storageConfiguration, logger);
            storageBootStrapper.ConfigureStorageServices(serviceCollection);

            var queueBootstrapper = new QueueBootstrapper(Id, Configuration);
            queueBootstrapper.ConfigureServices(serviceCollection);
        }
    }
}
