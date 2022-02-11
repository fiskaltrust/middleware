using System;
using System.Net;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers
{
    public static class ConfigurationHelper
    {
        public static WebProxy CreateProxy(FiskalySCUConfiguration config)
        {
            if (string.IsNullOrEmpty(config.ProxyServer))
            {
                return null;
            }

            var proxy = new WebProxy
            {
                Address = new Uri($"http://{config.ProxyServer}:{config.ProxyPort}"),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrEmpty(config.ProxyUsername))
            {
                proxy.Credentials = new NetworkCredential(config.ProxyUsername, config.ProxyPassword);
            }

            return proxy;
        }
    }
}
