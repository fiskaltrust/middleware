using System;
using System.Collections.Generic;
using System.Reflection;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Queue.Bootstrapper;
using fiskaltrust.Middleware.Storage.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Queue.InMemory
{
    public class PosBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            var logger = serviceCollection.BuildServiceProvider().GetRequiredService<ILogger<IMiddlewareBootstrapper>>();

            var storageBootStrapper = new InMemoryStorageBootstrapper(Id, Configuration, logger);
            storageBootStrapper.ConfigureStorageServices(serviceCollection);

            var assemblyName = new AssemblyName(typeof(PosBootstrapper).Assembly.GetName().ToString());
            var version = typeof(PosBootstrapper).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion.Split(new char[] { '+', '-' })[0];
            assemblyName.Version = System.Version.TryParse(version, out var result) ? new Version(result.Major, result.Minor, result.Build, 0) : new Version(assemblyName.Version.Major, assemblyName.Version.Minor, assemblyName.Version.Build, 0);
            Configuration.Add("assemblyname", assemblyName);

            var queueBootstrapper = new QueueBootstrapper(Id, Configuration);
            queueBootstrapper.ConfigureServices(serviceCollection);
        }
    }
}
