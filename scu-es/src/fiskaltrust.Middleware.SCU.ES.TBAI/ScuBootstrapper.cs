using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.ES.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.ES.TBAI;

public class ScuBootstrapper : IMiddlewareBootstrapper
{
    public Guid Id { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = null!;

    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        var tbaiScuConfig = JsonConvert.DeserializeObject<TBaiScuConfiguration>(JsonConvert.SerializeObject(Configuration));

        _ = serviceCollection
            .AddSingleton(tbaiScuConfig);
            //.AddScoped<IESSSCD, TBaiSCU>();
    }
}
