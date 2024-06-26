﻿using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Queue.Test.Launcher.Grpc;
using fiskaltrust.Middleware.Queue.Test.Launcher.Wcf;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Queue.Test.Launcher.Helpers
{
    public static class HostingHelper
    {
        public static List<IDisposable> SetupServiceForObject(PackageConfiguration config, object serviceInstance, ILoggerFactory loggerFactory)
        {
            var nutShells = new List<IDisposable>();
            var serviceType = GetMiddlewareComponentTypeForPackage(config, serviceInstance);
            if (config.Url.Length == 0)
            {
                var service = new WCFService(loggerFactory.CreateLogger<WCFService>());
                service.ConfigureService(config, serviceType, serviceInstance, "");
                nutShells.Add(service);
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
                    case "rest" or "http":
                        var restService = new RestService(loggerFactory.CreateLogger<RestService>());
                        restService.ConfigureService(config, serviceType, serviceInstance, serviceUrl);
                        nutShells.Add(restService);
                        break;
                    default:
                        var service = new WCFService(loggerFactory.CreateLogger<WCFService>());
                        service.ConfigureService(config, serviceType, serviceInstance, serviceUrl);
                        nutShells.Add(service);
                        break;
                }
            }
            return nutShells;
        }

        private static Type GetMiddlewareComponentTypeForPackage(PackageConfiguration config, object serviceInstance)
        {
            if (config.Package.Contains("service")|| config.Package.Contains("Queue"))
            { 
                return serviceInstance.GetType().GetInterfaces().First(x => x.FullName == typeof(ifPOS.v1.IPOS).FullName);
            }
            else if (config.Package.Contains("signing"))
            {
                return serviceInstance.GetType().GetInterfaces().First(x => x.FullName == typeof(ifPOS.v0.IATSSCD).FullName);
            }
            else if (config.Package.Contains("SCU.DE"))
            {
                return serviceInstance.GetType().GetInterfaces().First(x => x.FullName == typeof(ifPOS.v1.de.IDESSCD).FullName);
            }
            throw new NotSupportedException($"The package {config.Package} is currently not supported.");
        }
    }
}
