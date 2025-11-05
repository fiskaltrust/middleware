using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Localization.QueueBE.BESSCD;
using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.ZwartedoosApi;
using System.Text.Json;
using fiskaltrust.ifPOS.v2.be;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos;

public class ScuBootstrapper : IMiddlewareBootstrapper
{
    public Guid Id { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = null!;

    public void ConfigureServices(IServiceCollection services)
    {
        var scuConfig = JsonSerializer.Deserialize<ZwarteDoosScuConfiguration>(JsonSerializer.Serialize(Configuration));
        services.AddSingleton(scuConfig);
        services.AddTransient<IBESSCD, ZwarteDoosScuBe>();
    }
}