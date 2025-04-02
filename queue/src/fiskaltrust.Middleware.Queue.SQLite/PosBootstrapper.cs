using System;
using System.Collections.Generic;
using System.Reflection;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Queue.Bootstrapper;
using fiskaltrust.Middleware.Storage.SQLite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Queue.SQLite
{
    public class PosBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            var logger = serviceCollection.BuildServiceProvider().GetRequiredService<ILogger<IMiddlewareBootstrapper>>();

            var storageConfiguration = SQLiteStorageConfiguration.FromConfigurationDictionary(Configuration);
            serviceCollection.AddSingleton(sp => storageConfiguration);

            var storageBootStrapper = new SQLiteStorageBootstrapper(Id, Configuration, storageConfiguration, logger);
            storageBootStrapper.ConfigureStorageServices(serviceCollection);

            Configuration.Add("assemblytype", typeof(PosBootstrapper));

            var queueBootstrapper = new QueueBootstrapper(Id, Configuration);
            queueBootstrapper.ConfigureServices(serviceCollection);
        }
    }
}