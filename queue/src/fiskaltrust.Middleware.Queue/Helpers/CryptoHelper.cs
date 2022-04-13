using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.storage.encryption.V0;
using fiskaltrust.storage.V0;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace fiskaltrust.Middleware.Queue.Helpers
{
    public class CryptoHelper : ICryptoHelper
    {
        private const string ES256_JWS_HEADER = "eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCJ9";
        
        private static readonly X9ECParameters _curve = SecNamedCurves.GetByName("secp256r1");
        private static readonly ECDomainParameters _domainParameters = new ECDomainParameters(_curve.Curve, _curve.G, _curve.N, _curve.H);

        public (string hashBase64, string jwsData) CreateJwsToken(string payload, string privateKeyBase64, byte[] encryptionKey)
        {
            var jwsPayload = StringUtilities.ToBase64UrlString(Encoding.UTF8.GetBytes(payload));
            var data = Encoding.UTF8.GetBytes($"{ES256_JWS_HEADER}.{jwsPayload}");
            var sha256 = new Sha256Digest();
            sha256.Reset();
            sha256.BlockUpdate(data, 0, data.Length);

            var hash = new byte[sha256.GetDigestSize()];
            sha256.DoFinal(hash, 0);

            var decrypted = Convert.FromBase64String(Encoding.UTF8.GetString(Encryption.Decrypt(Convert.FromBase64String(privateKeyBase64), encryptionKey)));
            var privKeyParams = new ECPrivateKeyParameters(new Org.BouncyCastle.Math.BigInteger(decrypted), _domainParameters);

            var signer = SignerUtilities.GetSigner("SHA-256withECDSA");
            signer.Init(true, privKeyParams);
            signer.BlockUpdate(data, 0, data.Length);
            var signature = signer.GenerateSignature();
            var jwsSignature = StringUtilities.ToBase64UrlString(signature);

            return (Convert.ToBase64String(hash), $"{ES256_JWS_HEADER}.{jwsPayload}.{jwsSignature}");
        }

        public string GenerateBase64ChainHash(string previousReceiptHash, ftReceiptJournal receiptJournal, ftQueueItem queueItem)
        {
            using (var sha256 = SHA256.Create())
            {
                var input = new List<byte>();

                if (!string.IsNullOrWhiteSpace(previousReceiptHash))
                {
                    input.AddRange(Convert.FromBase64String(previousReceiptHash));
                }
                input.AddRange(receiptJournal.ftReceiptJournalId.ToByteArray());
                input.AddRange(BitConverter.GetBytes(receiptJournal.ftReceiptMoment.Ticks));
                input.AddRange(BitConverter.GetBytes(receiptJournal.ftReceiptNumber));
                input.AddRange(Convert.FromBase64String(queueItem.requestHash));
                input.AddRange(Convert.FromBase64String(queueItem.responseHash));

                var hash = sha256.ComputeHash(input.ToArray());
                return Convert.ToBase64String(hash);
            }
        }

        public string GenerateBase64Hash(string content)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
                return Convert.ToBase64String(hash);
            }
        }
    }
}
