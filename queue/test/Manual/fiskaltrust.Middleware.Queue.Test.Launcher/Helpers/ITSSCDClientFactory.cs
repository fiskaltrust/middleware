using System;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client;
using fiskaltrust.Middleware.Interface.Client.Grpc;
using fiskaltrust.Middleware.Interface.Client.Http;

namespace fiskaltrust.Middleware.Queue.Test.Launcher.Helpers
{
    public class ITSSCDClientFactory : IClientFactory<IITSSCD>
    {
        public IITSSCD CreateClient(ClientConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            switch (configuration.UrlType)
            {
                case "grpc":
                    return GrpcITSSCDFactory.CreateSSCDAsync(new GrpcClientOptions { Url = new Uri(configuration.Url) }).Result;
                case "rest":
                    var url = configuration.Url.Replace("rest://", "http://");
                    return HttpITSSCDFactory.CreateSSCDAsync(new ClientOptions { Url = new Uri(url), RetryPolicyOptions = RetryPolicyOptions.Default }).Result;
                default:
                    return GrpcITSSCDFactory.CreateSSCDAsync(new GrpcClientOptions { Url = new Uri(configuration.Url), RetryPolicyOptions = RetryPolicyOptions.Default }).Result;

                    //return SoapMESSCDFactory.CreateSSCDAsync(new ClientOptions { Url = new Uri(configuration.Url), RetryPolicyOptions = RetryPolicyOptions.Default }).Result;
            }
        }
    }
}
