﻿using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Queue.Bootstrapper;
using fiskaltrust.Middleware.Storage.MySQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Queue.MySQL
{
    public class PosBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            var logger = serviceCollection.BuildServiceProvider().GetRequiredService<ILogger<IMiddlewareBootstrapper>>();

            var storageConfiguration = MySQLStorageConfiguration.FromConfigurationDictionary(Configuration);
            serviceCollection.AddSingleton(sp => storageConfiguration);

            var storageBootStrapper = new MySQLBootstrapper(Id, Configuration, storageConfiguration, logger);
            storageBootStrapper.ConfigureStorageServices(serviceCollection);

            Configuration.Add("assemblytype", typeof(PosBootstrapper));

            var queueBootstrapper = new QueueBootstrapper(Id, Configuration);
            queueBootstrapper.ConfigureServices(serviceCollection);
        }
    }
}
