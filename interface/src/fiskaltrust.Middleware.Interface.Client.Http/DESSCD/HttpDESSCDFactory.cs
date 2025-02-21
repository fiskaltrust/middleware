using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Http
{
    /// <summary>
    /// Create Http SSCD.
    /// </summary>
    public static class HttpDESSCDFactory
    {
        public static async Task<IDESSCD> CreateSSCDAsync(HttpDESSCDClientOptions options)
        {
            var connectionhandler = new HttpProxyConnectionHandler<IDESSCD>(new HttpDESSCD(options));

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
