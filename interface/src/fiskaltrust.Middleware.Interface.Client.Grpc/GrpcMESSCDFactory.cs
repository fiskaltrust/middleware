using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Grpc
{
    /// <summary>
    /// A factory to create a gRPC-based IDESSCD client instance for communicating with a Montenegrin SCU package.
    /// </summary>
    public static class GrpcMESSCDFactory
    {
        public static async Task<IMESSCD> CreateSSCDAsync(GrpcClientOptions options)
        {
#if NET6_0_OR_GREATER
            var connectionhandler = new GrpcProxyConnectionHandler<IMESSCD>(options);
#else
            var connectionhandler = new NativeGrpcProxyConnectionHandler<IMESSCD>(options);
#endif

            if (options.RetryPolicyOptions != null)
            {
                var retryPolicyHelper = new RetryPolicyHandler<IMESSCD>(options.RetryPolicyOptions, connectionhandler);
                return new MESSCDRetryProxyClient(retryPolicyHelper);
            }
            else
            {
                return await connectionhandler.GetProxyAsync();
            }
        }
    }
}
