#if WCF

using System;
using System.ServiceModel;
using fiskaltrust.ifPOS.Tests.Helpers;
using fiskaltrust.Middleware.Interface.Tests.Helpers.Wcf;
using FluentAssertions;
using NUnit.Framework;

namespace fiskaltrust.ifPOS.Tests.v0.IATSSCD
{
    public class IATSSCDTests
    {
        private ServiceHost _host;
        private ifPOS.v0.IATSSCD _client;

        [OneTimeSetUp]
        public void BaseSetUp()
        {
            string url = "net.pipe://localhost/signing";
            _host = WcfHelper.StartHost<ifPOS.v0.IATSSCD>(url, new DummyATSSCD());
            _client = WcfHelper.GetProxy<ifPOS.v0.IATSSCD>(url);
        }


        [OneTimeTearDown]
        public void BaseTearDown()
        {
            _host.Close();
        }

        [Test]
        public void Echo()
        {
            string inMessage = "Hello World!";
            string outMessage = _client.Echo(inMessage);
            outMessage.Should().Be(inMessage);
        }

        [Test]
        public void EchoAsync()
        {

            string inMessage = "Hello World!";
            string echoAsyncMessage = null;

            var ar = _client.BeginEcho(inMessage, new AsyncCallback(result =>
            {
                var asyncState = (ifPOS.v0.IATSSCD)result.AsyncState;
                echoAsyncMessage = asyncState.EndEcho(result);
            }), _client);

            while (echoAsyncMessage == null)
                System.Threading.Thread.Sleep(0);

            echoAsyncMessage.Should().Be(inMessage);
        }

        [Test]
        public void ZDA()
        {
            string outMessage = _client.ZDA();
            outMessage.Should().Be(DummyATSSCD.zda);
        }

        [Test]
        public void ZDAAsync()
        {
            string zdaAsyncMessage = null;

            var ar = _client.BeginZDA(new AsyncCallback(result =>
            {
                var asyncState = (ifPOS.v0.IATSSCD)result.AsyncState;
                zdaAsyncMessage = asyncState.EndZDA(result);
            }), _client);
            while (zdaAsyncMessage == null)
                System.Threading.Thread.Sleep(0);
            zdaAsyncMessage.Should().Be(DummyATSSCD.zda);
        }

        [Test]
        public void Certificate()
        {
            byte[] outData = _client.Certificate();
            Convert.ToBase64String(outData).Should().Be(Convert.ToBase64String(DummyATSSCD.certificate));
        }

        [Test]
        public void CertificateAsync()
        {
            byte[] CertificateAsyncData = null;
            var ar = _client.BeginCertificate(new AsyncCallback(result =>
            {
                var asyncState = (ifPOS.v0.IATSSCD)result.AsyncState;
                CertificateAsyncData = asyncState.EndCertificate(result);
            }), _client);
            while (CertificateAsyncData == null)
                System.Threading.Thread.Sleep(0);
            Convert.ToBase64String(CertificateAsyncData).Should().Be(Convert.ToBase64String(DummyATSSCD.certificate));
        }


        [Test]
        public void Sign()
        {
            byte[] inData = Guid.NewGuid().ToByteArray();
            byte[] outData = _client.Sign(inData);
            Convert.ToBase64String(outData).Should().Be(Convert.ToBase64String(inData));
        }

        [Test]
        public void SignAsync()
        {

            byte[] inData = Guid.NewGuid().ToByteArray();
            byte[] SignAsyncData = null;
            var ar = _client.BeginSign(inData, new AsyncCallback(result =>
            {
                var asyncState = (ifPOS.v0.IATSSCD)result.AsyncState;
                SignAsyncData = asyncState.EndSign(result);
            }), _client);
            while (SignAsyncData == null)
                System.Threading.Thread.Sleep(0);
            Convert.ToBase64String(SignAsyncData).Should().Be(Convert.ToBase64String(inData));
        }
    }
}

#endif