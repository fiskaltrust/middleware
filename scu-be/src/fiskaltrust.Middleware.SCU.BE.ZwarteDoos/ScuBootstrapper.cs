﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Localization.QueueBE.BESSCD;
using System;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos;

public class ScuBootstrapper : IMiddlewareBootstrapper
{
    public Guid Id { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = null!;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ZwarteDoosScuConfiguration>();
        
        // Register both interfaces for the same implementation
        services.AddTransient<IBESSCD, ZwarteDoosScuBe>();
        services.AddTransient<IZwarteDoosScuBe, ZwarteDoosScuBe>();
        
        //services.AddHttpClient<ZwarteDoosFactory>();
    }
}