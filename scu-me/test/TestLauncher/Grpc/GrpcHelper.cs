using fiskaltrust.Middleware.SCU.ME.Test.Launcher.Grpc.Binders;
using fiskaltrust.Middleware.SCU.ME.Test.Launcher.Grpc.Interceptors;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace fiskaltrust.Middleware.SCU.ME.Test.Launcher.Grpc
{
    public static class GrpcHelper
    {
        public static Server StartHost(string url, Type type, object service, ILoggerFactory loggerFactory)
        {
            var baseAddresse = new Uri(url);
            var server = new Server();
            server.Ports.Add(new ServerPort(baseAddresse.Host, baseAddresse.Port, ServerCredentials.Insecure));

            // We use versioned names in our OperationContracts, e.g. v1/Sign. This works fine in C#, but not with regular .proto files, 
            // as they don't support special characters. To work around this issue, we register the methods twice with different 
            // behavior - once with the v1/ prefix, and once without it.
            server.Services.AddCodeFirst(type, service, BinderConfiguration.Create(binder: new RemoveMethodVersionPrefixBinder()), interceptors: new List<Interceptor> { new ServerLoggingInterceptor(loggerFactory), new ExceptionHandlingInterceptor(service.GetType()) });
            server.Services.AddCodeFirst(type, service, BinderConfiguration.Create(binder: new SkipNonVersionedMethodsBinder()), interceptors: new List<Interceptor> { new ServerLoggingInterceptor(loggerFactory), new ExceptionHandlingInterceptor(service.GetType()) });

            server.Start();
            return server;
        }

        public static T GetClient<T>(string url, int port) where T : class
        {
            var channel = new Channel(url, port, ChannelCredentials.Insecure);
            return channel.CreateGrpcService<T>();
        }

        public static int AddCodeFirst(this Server.ServiceDefinitionCollection services, Type type, object service, BinderConfiguration binderConfiguration = null, TextWriter log = null, IEnumerable<Interceptor> interceptors = null)
        {
            var builder = ServerServiceDefinition.CreateBuilder();
            var result = Binder.Create(log).Bind(builder, type, binderConfiguration, service);
            var serverServiceDefinition = builder.Build();
            if (interceptors != null)
            {
                foreach (var interceptor in interceptors)
                {
                    serverServiceDefinition = serverServiceDefinition.Intercept(interceptor);
                }
            }
            services.Add(serverServiceDefinition);
            return result;
        }
    }
}
