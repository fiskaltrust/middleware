using System.Collections.Generic;
#if NET6_0_OR_GREATER
using Grpc.Net.Client;
#else
using Grpc.Core;
#endif

namespace fiskaltrust.Middleware.Interface.Client.Grpc
{
    /// <summary>
    /// Can be used to specify additional details of the gRPC connection
    /// </summary>
    public class GrpcClientOptions : ClientOptions
    {
#if NET6_0_OR_GREATER
        public GrpcChannelOptions ChannelOptions { get; set; } = new GrpcChannelOptions();
        public bool AllowUnencryptedHttp2 { get; set; } = true;
#else
        public ChannelCredentials ChannelCredentials { get; set; } = ChannelCredentials.Insecure;
        public List<ChannelOption> ChannelOptions { get; set; } = new List<ChannelOption>();
#endif
    }
}
