using System;
using System.Collections.Generic;
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
            
            var storageBootStrapper = new SQLiteStorageBootstrapper(Id, Configuration, logger);
            storageBootStrapper.ConfigureStorageServices(serviceCollection);

            var queueBootstrapper = new QueueBootstrapper(Id, Configuration);
            queueBootstrapper.ConfigureServices(serviceCollection);

        }
    }
}