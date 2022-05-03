using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace fiskaltrust.Middleware.SCU.ME.FiscalizationService.Helpers
{
    public class SigningBehaviour : IEndpointBehavior
    {
        private readonly X509Certificate2 _certificate;

        public SigningBehaviour(X509Certificate2 certificate) => _certificate = certificate;

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) => clientRuntime.ClientMessageInspectors.Add(new SignatureInjector(_certificate));
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }
        public void Validate(ServiceEndpoint endpoint) { }

        private class SignatureInjector : IClientMessageInspector
        {
            private readonly X509Certificate2 _certificate;

            public SignatureInjector(X509Certificate2 certificate) => _certificate = certificate;

            public void AfterReceiveReply(ref Message reply, object correlationState) { }

            public object BeforeSendRequest(ref Message request, IClientChannel channel)
            {
                var msgbuf = request.CreateBufferedCopy(int.MaxValue);

                var oldMessage = msgbuf.CreateMessage();
                var xdr = oldMessage.GetReaderAtBodyContents();

                var xmlRequest = new XmlDocument() { XmlResolver = new XmlUrlResolver() };
                xmlRequest.Load(xdr);
                xdr.Close();

                var privateKey = _certificate.GetRSAPrivateKey();

                var keyInfo = new KeyInfo();
                var keyInfoData = new KeyInfoX509Data();
                keyInfoData.AddCertificate(_certificate);
                keyInfo.AddClause(keyInfoData);

                var reference = new Reference("");
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform(false));
                reference.AddTransform(new XmlDsigExcC14NTransform(false));
                reference.DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
                reference.Uri = "#Request";

                var xml = new SignedXml(xmlRequest)
                {
                    SigningKey = privateKey
                };
                xml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
                xml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
                xml.KeyInfo = keyInfo;
                xml.AddReference(reference);
                xml.ComputeSignature();

                var signature = xml.GetXml();
                _ = xmlRequest.DocumentElement.AppendChild(signature);

                var ms = new MemoryStream();
                var xw = XmlWriter.Create(ms);
                xmlRequest.Save(xw);
                xw.Flush();
                xw.Close();

                ms.Position = 0;
                var xr = XmlReader.Create(ms);

                var message = Message.CreateMessage(request.Version, null, xr);
                message.Headers.CopyHeadersFrom(request);
                message.Properties.CopyProperties(request.Properties);

                request = message;
                return null;
            }
        }
    }
}