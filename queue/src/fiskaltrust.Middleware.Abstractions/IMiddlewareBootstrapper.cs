using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Abstractions
{
    public interface IMiddlewareBootstrapper
    {
        Guid Id { get; set; }

        Dictionary<string, object> Configuration { get; set; }

        void ConfigureServices(IServiceCollection serviceCollection);
        Task<Func<IServiceProvider, Task>> ConfigureServicesAsync(IServiceCollection serviceCollection);
    }
}
