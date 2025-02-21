using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Http
{
    /// <summary>
    /// Create Http SSCD.
    /// </summary>
    public static class HttpMESSCDFactory
    {
        public static async Task<IMESSCD> CreateSSCDAsync(HttpMESSCDClientOptions options)
        {
            var connectionhandler = new HttpProxyConnectionHandler<IMESSCD>(new HttpMESSCD(options));

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
