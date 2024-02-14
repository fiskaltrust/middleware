using fiskaltrust.ifPOS.v1.at;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace fiskaltrust.Middleware.SCU.AT.InMemory
{
    public class InMemorySCU : IATSSCD
    {
        private const string ZDA_NAME = "PFX";
        private const string CERT_PASSWORD = "password";

        private readonly AsymmetricKeyParameter _key;
        private readonly byte[] _certificate;

        private delegate byte[] Sign_Delegate(byte[] data);
        private delegate byte[] Certificate_Delegate();
        private delegate string Echo_Delegate(string message);
        private delegate string Zda_Delegate();

        public InMemorySCU()
        {
            var randomCertificate = CreateRandomSignatureCertificate();

            using var ms = new MemoryStream(randomCertificate);
            var pkcs = new Pkcs12Store(ms, CERT_PASSWORD.ToCharArray());

            foreach (var alias in pkcs.Aliases)
            {
                if (pkcs.IsKeyEntry(alias as string))
                {
                    var key = pkcs.GetKey(alias as string).Key;
                    var certChain = pkcs.GetCertificateChain(alias as string);
                    var cert = certChain[certChain.Length - 1].Certificate;

                    if (cert.SigAlgOid.Equals("1.2.840.10045.4.3.2"))
                    {
                        _key = key;
                        _certificate = cert.GetEncoded();
                        return;
                    }
                }
            }

            if(_certificate == null)
            {
                throw new Exception("An error occurred while creating the random certificate.");
            }
        }

        public async Task<CertificateResponse> CertificateAsync() => await Task.FromResult(new CertificateResponse { Certificate = _certificate }).ConfigureAwait(false);

        public byte[] Certificate() => _certificate;

        public IAsyncResult BeginCertificate(AsyncCallback callback, object state)
        {
            var d = new Certificate_Delegate(Certificate);
            var r = d.BeginInvoke(callback, d);
            return r;
        }

        public byte[] EndCertificate(IAsyncResult result)
        {
            var d = (Certificate_Delegate) result.AsyncState;
            return d.EndInvoke(result);
        }

        public async Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => await Task.FromResult(new EchoResponse { Message = echoRequest.Message }).ConfigureAwait(false);

        public string Echo(string message) => message;

        public IAsyncResult BeginEcho(string message, AsyncCallback callback, object state)
        {
            var d = new Echo_Delegate(Echo);
            var r = d.BeginInvoke(message, callback, d);
            return r;
        }

        public string EndEcho(IAsyncResult result)
        {
            var d = (Echo_Delegate) result.AsyncState;
            return d.EndInvoke(result);
        }

        public async Task<SignResponse> SignAsync(SignRequest signRequest) => await Task.FromResult(new SignResponse { SignedData = Sign(signRequest.Data) }).ConfigureAwait(false);

        public byte[] Sign(byte[] data)
        {
            var signer = SignerUtilities.GetSigner("SHA-256withECDSA");
            signer.Init(true, _key);
            signer.BlockUpdate(data, 0, data.Length);
            var signature = signer.GenerateSignature();

            signer.Init(false, new X509CertificateParser().ReadCertificate(_certificate).GetPublicKey());
            signer.BlockUpdate(data, 0, data.Length);
            var rawSignature = new byte[64];

            var rLength = signature[3];
            if (rLength >= 32)
            {
                Array.Copy(signature, 4 + rLength - 32, rawSignature, 0, 32);
            }
            else
            {
                Array.Copy(signature, 4, rawSignature, 32 - rLength, rLength);
            }

            var sLength = signature[4 + rLength + 1];
            if (sLength >= 32)
            {
                Array.Copy(signature, 4 + rLength + 2 + sLength - 32, rawSignature, 32, 32);
            }
            else
            {
                Array.Copy(signature, 4 + rLength + 2, rawSignature, 32 + 32 - sLength, sLength);
            }

            return rawSignature;
        }
        
        public IAsyncResult BeginSign(byte[] data, AsyncCallback callback, object state)
        {
            var d = new Sign_Delegate(Sign);
            var r = d.BeginInvoke(data, callback, d);
            return r;
        }
        
        public byte[] EndSign(IAsyncResult result)
        {
            var d = (Sign_Delegate) result.AsyncState;
            return d.EndInvoke(result);
        }

        public async Task<ZdaResponse> ZdaAsync() => await Task.FromResult(new ZdaResponse { ZDA = ZDA_NAME }).ConfigureAwait(false);
        
        public string ZDA() => ZDA_NAME;
        
        public IAsyncResult BeginZDA(AsyncCallback callback, object state)
        {
            var d = new Zda_Delegate(ZDA);
            var r = d.BeginInvoke(callback, d);
            return r;
        }
        
        public string EndZDA(IAsyncResult result)
        {
            var d = (Zda_Delegate) result.AsyncState;
            return d.EndInvoke(result);
        }

        private byte[] CreateRandomSignatureCertificate()
        {
            var random = new SecureRandom();
            var kpg = new Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator();
            kpg.Init(new KeyGenerationParameters(random, 256));

            var signingKeyPair = kpg.GenerateKeyPair();

            var signingBuilder = new X509V3CertificateGenerator();
            signingBuilder.SetSerialNumber(BigInteger.One);
            signingBuilder.SetNotBefore(DateTime.Now.AddDays(-1));
            signingBuilder.SetNotAfter(DateTime.Now.AddYears(20));
            signingBuilder.SetPublicKey(signingKeyPair.Public);
            signingBuilder.SetIssuerDN(new X509Name("CN=fiskaltrust DEMO"));
            signingBuilder.SetSubjectDN(new X509Name("CN=fiskaltrust DEMO Signing Cert"));
            signingBuilder.SetSignatureAlgorithm("SHA256withECDSA");
            signingBuilder.AddExtension(X509Extensions.BasicConstraints, true, new BasicConstraints(false));
            signingBuilder.AddExtension(X509Extensions.KeyUsage, true, new KeyUsage(X509KeyUsage.DigitalSignature));
            var signingCertificate = signingBuilder.Generate(signingKeyPair.Private);

            var store = new Pkcs12Store();
            var signingCertificateEntry = new X509CertificateEntry(signingCertificate);
            store.SetKeyEntry("sign-pk", new AsymmetricKeyEntry(signingKeyPair.Private), new[] { signingCertificateEntry });

            var ms = new MemoryStream();
            store.Save(ms, CERT_PASSWORD.ToCharArray(), random);

            return ms.ToArray();
        }
    }
}