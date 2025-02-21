using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Http
{
    /// <summary>
    /// Create Http SSCD.
    /// </summary>
    public static class HttpITSSCDFactory
    {
        public static async Task<IITSSCD> CreateSSCDAsync(HttpITSSCDClientOptions options)
        {
            var connectionhandler = new HttpProxyConnectionHandler<IITSSCD>(new HttpITSSCD(options));

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
