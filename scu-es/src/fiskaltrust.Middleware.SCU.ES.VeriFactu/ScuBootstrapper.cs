using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.ES.VeriFactuSoap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu;

public class ScuBootstrapper : IMiddlewareBootstrapper
{
    public Guid Id { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = null!;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(VeriFactuSCUConfiguration.FromConfiguration(Configuration));
        services.AddHttpClient<IClient, Client>((sp, client) =>
        {
            var config = sp.GetRequiredService<VeriFactuSCUConfiguration>();
            client.BaseAddress = new Uri(config.BaseUrl);
            client.DefaultRequestHeaders.Add("AcceptCharset", "utf-8");
        }).ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var config = sp.GetRequiredService<VeriFactuSCUConfiguration>();
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(config.Certificate);
            return handler;
        });
        services.AddScoped<IESSSCD, VeriFactuSCU>();
    }

}
