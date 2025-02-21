#if WCF

using fiskaltrust.Middleware.Interface.Client.Http;
using fiskaltrust.Middleware.Interface.Client.Tests.Helpers;
using fiskaltrust.Middleware.Interface.Tests.Helpers.Wcf;
using System;
using System.ServiceModel;

namespace fiskaltrust.Middleware.Interface.Client.Tests.IPOS.Wcf
{
    // If these tests are failing you have to execute the following command as an Administrator
    // netsh http add urlacl url=http://+:8008/ user=Everyone listen=yes
    // To add the url that is used for binding
    public class HttpIPOSV0Tests : IPOSV0Tests
    {
        private string _url;
        private ServiceHost _serviceHost;

        public HttpIPOSV0Tests()
        {
            _url = $"http://localhost:12080/pos/{Guid.NewGuid()}";
        }

        ~HttpIPOSV0Tests()
        {
            _serviceHost.Close();
            _serviceHost = null;
        }

        protected override ifPOS.v0.IPOS CreateClient() => HttpPosFactory.CreatePosAsync(new HttpPosClientOptions { Url = new Uri(_url), UseUnversionedLegacyUrls = true, CommunicationType = HttpCommunicationType.Json }).Result;

        protected override void StartHost() => _serviceHost = WcfHelper.StartRestHost<IDummyPOS>(_url, new DummyPOS());

        protected override void StopHost() => _serviceHost.Close();
    }
}

#endif