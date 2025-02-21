using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Soap
{
    /// <summary>
    /// Create SOAP SSCD.
    /// </summary>
    public static class SoapMESSCDFactory
    {
        public static async Task<IMESSCD> CreateSSCDAsync(ClientOptions options)
        {
            var soapClientOptions = new SoapClientOptions
            {
                RetryPolicyOptions = options.RetryPolicyOptions,
                Url = options.Url
            };
            return await CreateSSCDAsync(soapClientOptions);
        }

        public static async Task<IMESSCD> CreateSSCDAsync(SoapClientOptions options)
        {
            var connectionhandler = new SoapProxyConnectionHandler<IMESSCD>(options);

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
