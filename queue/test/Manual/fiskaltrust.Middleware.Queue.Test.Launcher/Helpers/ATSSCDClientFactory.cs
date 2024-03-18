using System;
using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client;
using fiskaltrust.Middleware.Interface.Client.Grpc;
using fiskaltrust.Middleware.Interface.Client.Http;
using fiskaltrust.Middleware.Interface.Client.Http.ATSSCD;
using fiskaltrust.Middleware.Interface.Client.Soap;

namespace fiskaltrust.Middleware.Queue.Test.Launcher.Helpers
{
    public class ATSSCDClientFactory : IClientFactory<IATSSCD>
    {
        public IATSSCD CreateClient(ClientConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            switch (configuration.UrlType)
            {
                case "grpc":
                    return GrpcATSSCDFactory.CreateSSCDAsync(new GrpcClientOptions { Url = new Uri(configuration.Url), RetryPolicyOptions = RetryPolicyOptions.Default }).Result;
                case "rest":
                    var url = configuration.Url.Replace("rest://", "http://");
                    return HttpATSSCDFactory.CreateSSCDAsync(new HttpATSSCDClientOptions { Url = new Uri(url), RetryPolicyOptions = RetryPolicyOptions.Default }).Result;
                default:
                    return SoapATSSCDFactory.CreateSSCDAsync(new ClientOptions { Url = new Uri(configuration.Url), RetryPolicyOptions = RetryPolicyOptions.Default }).Result;
            }
        }
    }
}
