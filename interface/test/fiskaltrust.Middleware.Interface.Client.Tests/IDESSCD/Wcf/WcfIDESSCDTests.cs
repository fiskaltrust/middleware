#if WCF

using fiskaltrust.Middleware.Interface.Client.Soap;
using fiskaltrust.Middleware.Interface.Client.Tests.Helpers;
using fiskaltrust.Middleware.Interface.Tests.Helpers.Wcf;
using System;
using System.ServiceModel;

namespace fiskaltrust.Middleware.Interface.Client.Tests.IDESSCD.Wcf
{
    public class WcfIDESSCDTests : IDESSCDTests
    {
        private string _url;
        private ServiceHost _serviceHost;

        public WcfIDESSCDTests()
        {
            _url = $"net.pipe://localhost/pos/{Guid.NewGuid()}";
        }

        ~WcfIDESSCDTests()
        {
            if (_serviceHost != null)
                StopHost();
        }

        protected override ifPOS.v1.de.IDESSCD CreateClient() => SoapDESSCDFactory.CreateSSCDAsync(new ClientOptions { Url = new Uri(_url), RetryPolicyOptions = _retryPolicyOptions }).Result;

        protected override void StartHost()
        {
            if (_serviceHost == null)
                _serviceHost = WcfHelper.StartHost<ifPOS.v1.de.IDESSCD>(_url, new DummyDESSCD());
        }

        protected override void StopHost()
        {
            _serviceHost.Close();
            _serviceHost = null;
        }
    }
}

#endif