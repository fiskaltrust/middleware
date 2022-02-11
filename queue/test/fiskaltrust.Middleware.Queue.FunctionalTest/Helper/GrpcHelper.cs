using Grpc.Core;
using ProtoBuf.Grpc.Client;

namespace fiskaltrust.Middleware.Queue.FunctionalTest.Helper
{
    public static class GrpcHelper
    {
        public static T GetClient<T>(string url, int port) where T : class
        {
            var channel = new Channel(url, port, ChannelCredentials.Insecure);
            return channel.CreateGrpcService<T>();
        }
    }
}
