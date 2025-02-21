#if WCF

using fiskaltrust.ifPOS.Tests.Helpers;
using fiskaltrust.ifPOS.v0;
using fiskaltrust.Middleware.Interface.Tests.Helpers.Wcf;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.ServiceModel;

namespace fiskaltrust.ifPOS.Tests.v1.IPOS
{
    public class WcfIPOSV1Tests : IPOSV1Tests
    {
        private string _url;
        private ServiceHost _serviceHost;

        public WcfIPOSV1Tests()
        {
            _url = $"net.pipe://localhost/pos/{Guid.NewGuid()}";
        }

        ~WcfIPOSV1Tests()
        {
            _serviceHost.Close();
            _serviceHost = null;
        }

        protected override ifPOS.v1.IPOS CreateClient() => WcfHelper.GetProxy<ifPOS.v1.IPOS>(_url);

        protected override void StartHost() => _serviceHost = WcfHelper.StartHost<ifPOS.v1.IPOS>(_url, new DummyPOSV1());

        protected override void StopHost() => _serviceHost.Close();

        [Test]
        [Obsolete]
        public void Sign_ShouldReturnSameQueueId_For_v0Client()
        {
            var client = CreateClient();
            var queueId = Guid.NewGuid().ToString();
            var response = client.Sign(new ReceiptRequest
            {
                ftQueueID = queueId
            });
            response.ftQueueID.Should().Be(queueId);
        }

        [Test]
        [Obsolete]
        public void Echo_ShouldReturnSameMessage_For_v0Client()
        {
            var client = CreateClient();
            var inMessage = "Hello World!";
            var outMessage = client.Echo(inMessage);
            outMessage.Should().Be(inMessage);
        }
    }
}

#endif