using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos;

public static class ZwarteDoosApiClientFactory
{
    public static ZwarteDoosApiClient Create(
        ZwarteDoosApiClientConfiguration configuration,
        HttpClient? httpClient = null,
        ILogger<ZwarteDoosApiClient>? logger = null)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        if (string.IsNullOrWhiteSpace(configuration.DeviceId))
            throw new ArgumentException("DeviceId is required", nameof(configuration));

        if (string.IsNullOrWhiteSpace(configuration.SharedSecret))
            throw new ArgumentException("SharedSecret is required", nameof(configuration));

        httpClient ??= new HttpClient();
        logger ??= new NullLogger<ZwarteDoosApiClient>();

        return new ZwarteDoosApiClient(configuration, httpClient, logger);
    }


    public static IServiceCollection AddZwarteDoosApiClient(
        this IServiceCollection services,
        ZwarteDoosApiClientConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        services.AddSingleton(configuration);
        // Note: AddHttpClient requires Microsoft.Extensions.Http package
        // services.AddHttpClient<ZwarteDoosApiClient>();
        services.AddScoped<HttpClient>();
        services.AddScoped<ZwarteDoosApiClient>();

        return services;
    }

    public static IServiceCollection AddZwarteDoosApiClient(
        this IServiceCollection services,
        Action<ZwarteDoosApiClientConfiguration> configureOptions)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        var configuration = new ZwarteDoosApiClientConfiguration();
        configureOptions(configuration);

        return services.AddZwarteDoosApiClient(configuration);
    }
}

// Null logger implementation for cases where no logger is provided
internal class NullLogger<T> : ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) => new NullDisposable();
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

    private class NullDisposable : IDisposable
    {
        public void Dispose() { }
    }
}