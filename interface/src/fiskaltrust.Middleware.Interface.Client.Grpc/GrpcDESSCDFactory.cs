using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Grpc
{
    /// <summary>
    /// A factory to create a gRPC-based IDESSCD client instance for communicating with a German SCU package.
    /// </summary>
    public static class GrpcDESSCDFactory
    {
        public static async Task<IDESSCD> CreateSSCDAsync(GrpcClientOptions options)
        {
#if NET6_0_OR_GREATER
            var connectionhandler = new GrpcProxyConnectionHandler<IDESSCD>(options);
#else
            var connectionhandler = new NativeGrpcProxyConnectionHandler<IDESSCD>(options);
#endif

            if (options.RetryPolicyOptions != null)
            {
                var retryPolicyHelper = new RetryPolicyHandler<IDESSCD>(options.RetryPolicyOptions, connectionhandler);
                return new DESSCDRetryProxyClient(retryPolicyHelper);
            }
            else
            {
                return await connectionhandler.GetProxyAsync();
            }
        }
    }
}
