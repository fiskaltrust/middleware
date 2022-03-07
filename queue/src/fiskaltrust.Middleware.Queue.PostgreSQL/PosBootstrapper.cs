using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Queue.Bootstrapper;
using fiskaltrust.Middleware.Storage.EFCore.PostgreSQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Queue.PostgreSQL
{
    public class PosBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            var logger = serviceCollection.BuildServiceProvider().GetRequiredService<ILogger<IMiddlewareBootstrapper>>();

            var storageBootStrapper = new EFCorePostgreSQLStorageBootstrapper(Id, Configuration, logger);
            storageBootStrapper.ConfigureStorageServices(serviceCollection);

            var queueBootstrapper = new QueueBootstrapper(Id, Configuration);
            queueBootstrapper.ConfigureServices(serviceCollection);
            
            serviceCollection.AddSingleton(sp => JsonConvert.DeserializeObject<PostgreSQLQueueConfiguration>(JsonConvert.SerializeObject(sp.GetRequiredService<MiddlewareConfiguration>().Configuration)));
        }
    }
}
