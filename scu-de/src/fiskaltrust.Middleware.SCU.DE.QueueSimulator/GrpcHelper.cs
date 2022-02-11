using System;
using System.Collections.Generic;
using System.Linq;
using Grpc.Core;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Server;

namespace fiskaltrust.Middleware.SCU.DE.QueueSimulator
{
#pragma warning disable
    public static class GrpcHelper
    {
        public static Server StartHost<T>(string url, T service) where T : class
        {
            var baseAddresse = new Uri(url);
            var server = new Server();
            server.Ports.Add(new ServerPort(baseAddresse.Host, baseAddresse.Port, ServerCredentials.Insecure));
            server.Services.AddCodeFirst(service);
            server.Start();
            return server;
        }

        public static T GetClient<T>(string url, int port) where T : class
        {
            var channel = new Channel(url, port, ChannelCredentials.Insecure);
            return channel.CreateGrpcService<T>();
        }
    }
}
