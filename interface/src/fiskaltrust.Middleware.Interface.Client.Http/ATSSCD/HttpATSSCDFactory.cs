using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Http.ATSSCD
{
    /// <summary>
    /// A factory to create a HTTP-based IATSSCD client instance for communicating with an Austrian SCU package.
    /// </summary>
    public static class HttpATSSCDFactory
    {
        public static async Task<IATSSCD> CreateSSCDAsync(HttpATSSCDClientOptions options)
        {
            var connectionhandler = new HttpProxyConnectionHandler<IATSSCD>(new HttpATSSCD(options));

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
