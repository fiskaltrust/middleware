#if WCF

using fiskaltrust.ifPOS.Tests.Helpers;
using fiskaltrust.Middleware.Interface.Tests.Helpers.Wcf;
using System.ServiceModel;

namespace fiskaltrust.ifPOS.Tests.v1.IDESSCD
{
    public class WcfIDESSCDTests : IDESSCDTests
    {
        private string _url;
        private ServiceHost _serviceHost;

        public WcfIDESSCDTests()
        {
            _url = "net.pipe://localhost/signing";
        }

        ~WcfIDESSCDTests()
        {
            _serviceHost.Close();
            _serviceHost = null;
        }

        protected override ifPOS.v1.de.IDESSCD CreateClient() => WcfHelper.GetProxy<ifPOS.v1.de.IDESSCD>(_url);

        protected override void StartHost() => _serviceHost = WcfHelper.StartHost<ifPOS.v1.de.IDESSCD>(_url, new DummyDESSCD());

        protected override void StopHost() => _serviceHost.Close();
    }
}

#endif