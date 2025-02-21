using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Grpc
{
    /// <summary>
    /// A factory to create a gRPC-based IPOS client instance for communicating with a Queue package.
    /// </summary>
    public static class GrpcPosFactory
    {
        public static async Task<IPOS> CreatePosAsync(GrpcClientOptions options)
        {
#if NET6_0_OR_GREATER
            var connectionhandler = new GrpcProxyConnectionHandler<IPOS>(options);
#else
            var connectionhandler = new NativeGrpcProxyConnectionHandler<IPOS>(options);
#endif
            if (options.RetryPolicyOptions != null)
            {
                var retryPolicyHelper = new RetryPolicyHandler<IPOS>(options.RetryPolicyOptions, connectionhandler);
                return new PosRetryProxyClient(retryPolicyHelper);
            }
            else
            {
                return await connectionhandler.GetProxyAsync();
            }
        }
    }
}
