#if WCF

using fiskaltrust.ifPOS.Tests.Helpers;
using fiskaltrust.Middleware.Interface.Tests.Helpers.Wcf;
using System;
using System.ServiceModel;

namespace fiskaltrust.ifPOS.Tests.v0.IPOS
{
    public class WcfV1ServerAndV1ClientIPOSTests : IPOSTests
    {
        private string _url;
        private ServiceHost _serviceHost;

        public WcfV1ServerAndV1ClientIPOSTests()
        {
            _url = $"net.pipe://localhost/pos/{Guid.NewGuid()}";
        }

        ~WcfV1ServerAndV1ClientIPOSTests()
        {
            _serviceHost.Close();
            _serviceHost = null;
        }

        protected override ifPOS.v0.IPOS CreateClient() => WcfHelper.GetProxy<ifPOS.v1.IPOS>(_url);

        protected override void StartHost() => _serviceHost = WcfHelper.StartHost<ifPOS.v1.IPOS>(_url, new DummyPOSV1());

        protected override void StopHost() => _serviceHost.Close();
    }
}

#endif