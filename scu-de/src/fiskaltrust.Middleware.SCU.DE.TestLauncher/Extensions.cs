using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace fiskaltrust.Middleware.SCU.DE.TestLauncher
{
    public static class Extensions
    {

        public static void SetGlobalLogger(LogLevel verbosity = LogLevel.Debug)
        {
            var configuration = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}");

            switch (verbosity)
            {
                case LogLevel.Trace:
                    configuration = configuration.MinimumLevel.Verbose();
                    break;
                case LogLevel.Debug:
                    configuration = configuration.MinimumLevel.Debug();
                    break;
                case LogLevel.Information:
                    configuration = configuration.MinimumLevel.Information();
                    break;
                case LogLevel.Warning:
                    configuration = configuration.MinimumLevel.Warning();
                    break;
                case LogLevel.Error:
                    configuration = configuration.MinimumLevel.Error();
                    break;
                case LogLevel.Critical:
                    configuration = configuration.MinimumLevel.Fatal();
                    break;
                case LogLevel.None:
                    break;
                default:
                    configuration = configuration.MinimumLevel.Information();
                    break;
            }
            Log.Logger = configuration.CreateLogger();
        }

        public static IServiceCollection AddStandardLoggers(this IServiceCollection services, LogLevel verbosity)
        {
            SetGlobalLogger(verbosity);

            services.AddLogging(builder =>
            {
                builder.AddSerilog(dispose: true);
                builder.SetMinimumLevel(verbosity);
            });
            return services;
        }

        public static IEnumerable<Exception> Flatten(this Exception exception)
        {
            yield return exception;
            while (exception.InnerException != null)
            {
                yield return exception.InnerException;
                exception = exception.InnerException;
            }
        }
    }
}
