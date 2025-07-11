using System;
using System.Net.Http;
using Azure.Core;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client;
using fiskaltrust.Middleware.Interface.Client.Grpc;
using fiskaltrust.Middleware.Interface.Client.Http;
using fiskaltrust.Middleware.Interface.Client.Soap;
using Grpc.Core;
using Grpc.Net.Client;

namespace fiskaltrust.Middleware.Localization.QueueES.UnitTest
{
    public class ESSSCDClientFactory : IClientFactory<IESSSCD>
    {
        private readonly Guid _cashboxId;
        private readonly string _accessToken;
        public ESSSCDClientFactory(Guid cashboxId, string accessToken)
        {
            _cashboxId = cashboxId;
            _accessToken = accessToken;
        }
        public IESSSCD CreateClient(ClientConfiguration configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var retryPolicyoptions = new RetryPolicyOptions
            {
                DelayBetweenRetries = configuration.DelayBetweenRetries != default ? configuration.DelayBetweenRetries : RetryPolicyOptions.Default.DelayBetweenRetries,
                Retries = configuration.RetryCount ?? RetryPolicyOptions.Default.Retries,
                ClientTimeout = configuration.Timeout != default ? configuration.Timeout : RetryPolicyOptions.Default.ClientTimeout
            };

            var isHttps = true;
            var sslValidationDisabled = false;

            return configuration.UrlType switch
            {
                "grpc" => GrpcESSSCDFactory.CreateSSCDAsync(new GrpcClientOptions
                {
                    Url = new Uri(configuration.Url.Replace("grpc://", isHttps ? "https://" : "http://")),
                    RetryPolicyOptions = retryPolicyoptions,
                    ChannelOptions = new GrpcChannelOptions
                    {
                        Credentials = isHttps ? ChannelCredentials.SecureSsl : ChannelCredentials.Insecure,
                        HttpHandler = isHttps && sslValidationDisabled ? new HttpClientHandler { ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true } : null
                    }
                }).Result,
                "https" => HttpESSSCDFactory.CreateSSCDAsync(new HttpESSSCDClientOptions
                {
                    Url = new Uri(configuration.Url.Replace("rest://", isHttps ? "https://" : "http://")),
                    RetryPolicyOptions = retryPolicyoptions,
                    DisableSslValidation = sslValidationDisabled,
                    CashboxId = _cashboxId,
                    AccessToken = _accessToken
                }).Result,
                "http" or "net.tcp" or "wcf" => SoapESSSCDFactory.CreateSSCDAsync(new SoapClientOptions
                {
                    Url = new Uri(configuration.Url),
                    RetryPolicyOptions = retryPolicyoptions
                }).Result,
                _ => throw new ArgumentException("This version of the fiskaltrust Launcher currently only supports gRPC, REST and SOAP communication."),
            };
        }
    }
}
