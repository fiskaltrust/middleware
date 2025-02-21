#if WCF

using fiskaltrust.Middleware.Interface.Client.Http;
using fiskaltrust.Middleware.Interface.Client.Tests.Helpers;
using fiskaltrust.Middleware.Interface.Tests.Helpers.Wcf;
using System;
using System.ServiceModel;

namespace fiskaltrust.Middleware.Interface.Client.Tests.IDESSCD.Wcf
{
    // If these tests are failing you have to execute the following command as an Administrator
    // netsh http add urlacl url=http://+:12080/ user=Everyone listen=yes
    // To add the url that is used for binding
    public class HttpIDESSCDTests : IDESSCDTests
    {
        private string _url;
        private ServiceHost _serviceHost;

        public HttpIDESSCDTests()
        {
            _url = $"http://localhost:12080/pos/{Guid.NewGuid()}";
        }

        ~HttpIDESSCDTests()
        {
            if (_serviceHost != null)
                StopHost();
        }

        protected override ifPOS.v1.de.IDESSCD CreateClient() => HttpDESSCDFactory.CreateSSCDAsync(new HttpDESSCDClientOptions { Url = new Uri(_url), RetryPolicyOptions = _retryPolicyOptions }).Result;

        protected override void StartHost()
        {
            if (_serviceHost == null)
                _serviceHost = WcfHelper.StartRestHost<ifPOS.v1.de.IDESSCD>(_url, new DummyDESSCD());
        }

        protected override void StopHost()
        {
            _serviceHost.Close();
            _serviceHost = null;
        }
    }
}

#endif