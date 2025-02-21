using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Grpc
{
    /// <summary>
    /// A factory to create a gRPC-based IDESSCD client instance for communicating with a Montenegrin SCU package.
    /// </summary>
    public static class GrpcITSSCDFactory
    {
        public static async Task<IITSSCD> CreateSSCDAsync(GrpcClientOptions options)
        {
#if NET6_0_OR_GREATER
            var connectionhandler = new GrpcProxyConnectionHandler<IITSSCD>(options);
#else
            var connectionhandler = new NativeGrpcProxyConnectionHandler<IITSSCD>(options);
#endif

            if (options.RetryPolicyOptions != null)
            {
                var retryPolicyHelper = new RetryPolicyHandler<IITSSCD>(options.RetryPolicyOptions, connectionhandler);
                return new ITSSCDRetryProxyClient(retryPolicyHelper);
            }
            else
            {
                return await connectionhandler.GetProxyAsync();
            }
        }
    }
}
