using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Grpc
{
    public static class GrpcATSSCDFactory
    {
        public static async Task<IATSSCD> CreateSSCDAsync(GrpcClientOptions options)
        {
#if NET6_0_OR_GREATER
            var connectionhandler = new GrpcProxyConnectionHandler<IATSSCD>(options);
#else
            var connectionhandler = new NativeGrpcProxyConnectionHandler<IATSSCD>(options);
#endif

            if (options.RetryPolicyOptions != null)
            {
                var retryPolicyHelper = new RetryPolicyHandler<IATSSCD>(options.RetryPolicyOptions, connectionhandler);
                return new ATSSCDRetryProxyClient(retryPolicyHelper);
            }
            else
            {
                return await connectionhandler.GetProxyAsync();
            }
        }
    }
}
