#if GRPC
using Grpc.Core;
using ProtoBuf.Grpc.Server;
using ProtoBuf.Grpc.Client;

namespace fiskaltrust.Middleware.Interface.Tests.Helpers.Grpc
{
    public static class GrpcHelper
    {
        public static Server StartHost<T>(string url, int port, T component) where T : class
        {
            Server server = new Server
            {
                Ports = { new ServerPort(url, port, ServerCredentials.Insecure) }
            };
            server.Services.AddCodeFirst(component);
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
#endif