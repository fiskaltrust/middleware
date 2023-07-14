using System;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client;
using fiskaltrust.Middleware.Interface.Client.Grpc;
using fiskaltrust.Middleware.Interface.Client.Http;
using fiskaltrust.Middleware.Interface.Client.Soap;

namespace fiskaltrust.Middleware.Queue.Test.Launcher.Helpers
{
    public class MESSCDClientFactory : IClientFactory<IMESSCD>
    {
        public IMESSCD CreateClient(ClientConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            switch (configuration.UrlType)
            {
                case "grpc":
                    return GrpcMESSCDFactory.CreateSSCDAsync(new GrpcClientOptions { Url = new Uri(configuration.Url) }).Result;
                case "rest":
                    var url = configuration.Url.Replace("rest://", "http://");
                    return HttpMESSCDFactory.CreateSSCDAsync(new HttpMESSCDClientOptions { Url = new Uri(url), RetryPolicyOptions = RetryPolicyOptions.Default }).Result;
                default:
                    return GrpcMESSCDFactory.CreateSSCDAsync(new GrpcClientOptions { Url = new Uri(configuration.Url), RetryPolicyOptions = RetryPolicyOptions.Default }).Result;

                    //return SoapMESSCDFactory.CreateSSCDAsync(new ClientOptions { Url = new Uri(configuration.Url), RetryPolicyOptions = RetryPolicyOptions.Default }).Result;
            }
        }
    }
}
