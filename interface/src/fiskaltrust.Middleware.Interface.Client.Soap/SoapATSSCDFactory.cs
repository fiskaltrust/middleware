using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Soap
{
    /// <summary>
    /// A factory to create a SOAP-based IATSSCD client instance for communicating with an Austrian SCU package.
    /// </summary>
    public static class SoapATSSCDFactory
    {
        public static async Task<IATSSCD> CreateSSCDAsync(ClientOptions options)
        {
            var soapClientOptions = new SoapClientOptions
            {
                RetryPolicyOptions = options.RetryPolicyOptions,
                Url = options.Url
            };
            return await CreateSSCDAsync(soapClientOptions);
        }

        public static async Task<IATSSCD> CreateSSCDAsync(SoapClientOptions options)
        {
            var connectionhandler = new SoapProxyConnectionHandler<IATSSCD>(options);

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
