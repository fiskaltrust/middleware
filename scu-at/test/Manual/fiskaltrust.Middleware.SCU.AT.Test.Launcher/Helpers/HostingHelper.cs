using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.SCU.AT.Test.Launcher.Grpc;
using fiskaltrust.Middleware.SCU.AT.Test.Launcher.Wcf;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.AT.Test.Launcher.Helpers
{
    public static class HostingHelper
    {
        public static List<IDisposable> SetupServiceForObject(PackageConfiguration config, object serviceInstance, ILoggerFactory loggerFactory)
        {
            var nutShells = new List<IDisposable>();
            var serviceType = GetMiddlewareComponentTypeForPackage(config, serviceInstance);
            if (config.Url.Length == 0)
            {
#if NET461
                var service = new WCFService(loggerFactory.CreateLogger<WCFService>());
                service.ConfigureService(config, serviceType, serviceInstance, "");
                nutShells.Add(service);
#endif
            }
            foreach (var serviceUrl in config.Url)
            {
                Uri.TryCreate(serviceUrl, UriKind.Absolute, out var uri);
                switch (uri?.Scheme)
                {
                    case "grpc":
                        var host = new GrpcHost(loggerFactory.CreateLogger<GrpcHost>());
                        host.StartService(config, serviceUrl, serviceType, serviceInstance, loggerFactory);
                        nutShells.Add(host);
                        break;
#if NET461
                    case "rest":
                        var restService = new RestService(loggerFactory.CreateLogger<RestService>());
                        restService.ConfigureService(config, serviceType, serviceInstance, serviceUrl);
                        nutShells.Add(restService);
                        break;
                    default:
                        var service = new WCFService(loggerFactory.CreateLogger<WCFService>());
                        service.ConfigureService(config, serviceType, serviceInstance, serviceUrl);
                        nutShells.Add(service);
                        break;
#endif
                }
            }
            return nutShells;
        }

        private static Type GetMiddlewareComponentTypeForPackage(PackageConfiguration config, object serviceInstance)
        {
            if (config.Package.Contains("service"))
            {
                return serviceInstance.GetType().GetInterfaces().First(x => x.FullName == typeof(ifPOS.v0.IPOS).FullName);
            }
            else if (config.Package.Contains("Queue"))
            {
                return serviceInstance.GetType().GetInterfaces().First(x => x.FullName == typeof(ifPOS.v1.IPOS).FullName);
            }
            else if (config.Package.Contains("SCU.AT"))
            {
                return serviceInstance.GetType().GetInterfaces().First(x => x.FullName == typeof(ifPOS.v1.at.IATSSCD).FullName);
            }
            else if (config.Package.Contains("SCU.DE"))
            {
                return serviceInstance.GetType().GetInterfaces().First(x => x.FullName == typeof(ifPOS.v1.de.IDESSCD).FullName);
            }
            throw new NotSupportedException($"The package {config.Package} is currently not supported.");
        }
    }
}
