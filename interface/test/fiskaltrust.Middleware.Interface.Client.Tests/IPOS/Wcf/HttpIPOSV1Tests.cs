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
    public class HttpIPOSV1Tests : IPOSV1Tests
    {
        private string _url;
        private ServiceHost _serviceHost;

        public HttpIPOSV1Tests()
        {
            _url = $"http://localhost:12080/pos/{Guid.NewGuid()}";
        }

        ~HttpIPOSV1Tests()
        {
            _serviceHost.Close();
            _serviceHost = null;
        }

        protected override ifPOS.v1.IPOS CreateClient() => HttpPosFactory.CreatePosAsync(new HttpPosClientOptions { Url = new Uri(_url) }).Result;

        protected override void StartHost() => _serviceHost = WcfHelper.StartRestHost<ifPOS.v1.IPOS>(_url, new DummyPOSV1());

        protected override void StopHost() => _serviceHost.Close();
    }
}

#endif