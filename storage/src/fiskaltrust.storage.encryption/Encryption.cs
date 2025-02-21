using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using System;
using System.Linq;

namespace fiskaltrust.storage.encryption.V0
{
    public static class Encryption
    {
        private static readonly SecureRandom secureRandom = new SecureRandom();
        private static readonly X9ECParameters curve = SecNamedCurves.GetByName("secp256r1");
        private static readonly ECDomainParameters domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

        public static void GenerateKeys(out string privKey, out string pubKey)
        {
            var generator = new ECKeyPairGenerator();
            var keygenParams = new ECKeyGenerationParameters(domain, secureRandom);
            generator.Init(keygenParams);
            var keypair = generator.GenerateKeyPair();
            var privParams = (ECPrivateKeyParameters)keypair.Private;
            var pubParams = (ECPublicKeyParameters)keypair.Public;

            privKey = Convert.ToBase64String(privParams.D.ToByteArray());
            pubKey = Convert.ToBase64String(pubParams.Q.GetEncoded());
        }

        public static string GetPublicKey(string privKey)
        {
            var d = new BigInteger(Convert.FromBase64String(privKey));
            var q = domain.G.Multiply(d);
            return Convert.ToBase64String(q.GetEncoded());
        }

        public static byte[] ComputeSharedSecret(string yourPubKey, string myPrivKey)
        {
            var keyAgree = new ECDHBasicAgreement();
            keyAgree.Init(new ECPrivateKeyParameters(new BigInteger(Convert.FromBase64String(myPrivKey)), domain));

            var secret = keyAgree.CalculateAgreement(new ECPublicKeyParameters(
                curve.Curve.DecodePoint(Convert.FromBase64String(yourPubKey)), domain));

            return secret.ToByteArray();
        }

        public static byte[] Encrypt(byte[] data, byte[] secret)
        {
            var engine = new AesFastEngine();
            var cipher = new PaddedBufferedBlockCipher(
                new CbcBlockCipher(engine),
                new Pkcs7Padding());

            var iv = Guid.NewGuid().ToByteArray();

            var keyParam = new KeyParameter(secret.Take(16).ToArray());
            var parameters = new ParametersWithIV(keyParam, iv);

            cipher.Init(true, parameters);

            return iv.Concat(cipher.DoFinal(data)).ToArray();
        }

        public static byte[] Decrypt(byte[] data, byte[] secret)
        {
            var engine = new AesFastEngine();
            var cipher = new PaddedBufferedBlockCipher(
                new CbcBlockCipher(engine),
                new Pkcs7Padding());

            var keyParam = new KeyParameter(secret.Take(16).ToArray());
            var parameters = new ParametersWithIV(keyParam, data, 0, 16);

            cipher.Init(false, parameters);

            return cipher.DoFinal(data, 16, data.Length - 16);
        }

        public static byte[] Sign(byte[] data, string privKey)
        {
            var privKeyParams =
                new ECPrivateKeyParameters(
                    new BigInteger(Convert.FromBase64String(privKey)), domain);
            var signer = SignerUtilities.GetSigner("SHA-256withECDSA");
            signer.Init(true, privKeyParams);
            signer.BlockUpdate(data, 0, data.Length);
            return signer.GenerateSignature();
        }

        public static bool Verify(byte[] data, byte[] signatur, string pubKey)
        {
            var pubKeyParams = new ECPublicKeyParameters(
                curve.Curve.DecodePoint(Convert.FromBase64String(pubKey)), domain);
            var signer = SignerUtilities.GetSigner("SHA-256withECDSA");
            signer.Init(false, pubKeyParams);
            signer.BlockUpdate(data, 0, data.Length);
            var valid = signer.VerifySignature(signatur);
            return valid;
        }

        public static bool VerifyWithCertificateBase64(byte[] data, byte[] signature, string certificateBase64)
        {
            var certificate = new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(Convert.FromBase64String(certificateBase64));
            var pubKeyParams = (ECPublicKeyParameters) certificate.GetPublicKey();
            var signer = SignerUtilities.GetSigner("SHA-256withECDSA");
            signer.Init(false, pubKeyParams);
            signer.BlockUpdate(data, 0, data.Length);
            var valid = signer.VerifySignature(signature);
            return valid;
        }
    }
}