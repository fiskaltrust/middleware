using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Abstractions
{
    public interface IMiddlewareBootstrapper
    {
        Guid Id { get; set; }

        Dictionary<string, object> Configuration { get; set; }

        void ConfigureServices(IServiceCollection serviceCollection);
    }
}
