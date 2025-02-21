using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Soap
{
    /// <summary>
    /// Create SOAP SSCD.
    /// </summary>
    public static class SoapITSSCDFactory
    {
        public static async Task<IITSSCD> CreateSSCDAsync(ClientOptions options)
        {
            var soapClientOptions = new SoapClientOptions
            {
                RetryPolicyOptions = options.RetryPolicyOptions,
                Url = options.Url
            };
            return await CreateSSCDAsync(soapClientOptions);
        }

        public static async Task<IITSSCD> CreateSSCDAsync(SoapClientOptions options)
        {
            var connectionhandler = new SoapProxyConnectionHandler<IITSSCD>(options);

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
