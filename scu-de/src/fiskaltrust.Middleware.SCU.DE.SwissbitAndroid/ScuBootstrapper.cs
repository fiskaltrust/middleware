using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitAndroid
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSwissbitScuServices(Configuration);
        }
    }
}
