using NUnit.Framework;
using FluentAssertions;
using fiskaltrust.storage.encryption.V0;
using System.Text;
using System;

namespace fiskaltrust.storage.encryption.UnitTest
{
    public class EncryptionUnitTest
    {

        [Test]
        public void GenerateKeys_Should_NotBeNull()
        {
            Encryption.GenerateKeys(out var privateKey, out var publicKey);

            privateKey.Should().NotBeNull();
            publicKey.Should().NotBeNull();
        }

        [Test]
        public void GetPublicKey_Should_Match()
        {
            var privateKey = "UnYhOhNnHaMI9WhRo0lEhNACsiLUH4Frb0EM0PLHFOQ=";
            var expectedPublicKey = "BN8Er4HQvP41frQRmGgo0U+NQo+cyThbOlgupxSPfSvXlYmy/RQNYmreDQqQfORpstR8FpkQ0wMtkw/FPD+2AvM=";

            var actualPublicKey = Encryption.GetPublicKey(privateKey);

            actualPublicKey.Should().Be(expectedPublicKey);
        }

        [Test]
        public void GetPublicKey_Should_NotMatch()
        {
            var privateKey = "fxA/TziNTRJDFaUSCRoe7AtzweBxuQYqmfL8i4E/xU4=";
            var expectedPublicKey = "BN8Er4HQvP41frQRmGgo0U+NQo+cyThbOlgupxSPfSvXlYmy/RQNYmreDQqQfORpstR8FpkQ0wMtkw/FPD+2AvM=";

            var actualPublicKey = Encryption.GetPublicKey(privateKey);

            actualPublicKey.Should().NotBe(expectedPublicKey);
        }

        [Test]
        public void ComputeSharedSecret_Should_Match()
        {
            var privateKey = "UnYhOhNnHaMI9WhRo0lEhNACsiLUH4Frb0EM0PLHFOQ=";
            var publicKey = "BN8Er4HQvP41frQRmGgo0U+NQo+cyThbOlgupxSPfSvXlYmy/RQNYmreDQqQfORpstR8FpkQ0wMtkw/FPD+2AvM=";
            var expectedSharedSecret = "AMLuxuYGWba+VfwJrZjhoLLqsDHIdp5mIe707bACUN4M";

            var actualSharedSecret = Encryption.ComputeSharedSecret(publicKey, privateKey);

            Convert.ToBase64String(actualSharedSecret).Should().Be(expectedSharedSecret);
        }

        [Test]
        public void ComputeSharedSecret_Should_NotMatch()
        {
            var privateKey = "fxA/TziNTRJDFaUSCRoe7AtzweBxuQYqmfL8i4E/xU4=";
            var publicKey = "BN8Er4HQvP41frQRmGgo0U+NQo+cyThbOlgupxSPfSvXlYmy/RQNYmreDQqQfORpstR8FpkQ0wMtkw/FPD+2AvM=";
            var expectedSharedSecret = "\"AMLuxuYGWba+VfwJrZjhoLLqsDHIdp5mIe707bACUN4M\"";

            var actualSharedSecret = Encryption.ComputeSharedSecret(publicKey, privateKey);

            Convert.ToBase64String(actualSharedSecret).Should().NotBe(expectedSharedSecret);
        }

        [Test]
        public void Encrypt_Decrypt_Should_Match()
        {
            var secret = "\"AMLuxuYGWba+VfwJrZjhoLLqsDHIdp5mIe707bACUN4M\"";
            var sampleData = Encoding.ASCII.GetBytes("some test data");

            var expectedDescyptedDataAsString = "c29tZSB0ZXN0IGRhdGE=";

            var encryptedData = Encryption.Encrypt(sampleData, Encoding.ASCII.GetBytes(secret));
            var decryptedData = Encryption.Decrypt(encryptedData, Encoding.ASCII.GetBytes(secret));

            Convert.ToBase64String(decryptedData).Should().Be(expectedDescyptedDataAsString);
        }

        [Test]
        public void Encrypt_Decrypt_Should_NotMatch()
        {
            var secret = "\"AMLuxuYGWba+VfwJrZjhoLLqsDHIdp5mIe707bACUN4M\"";
            var sampleData = Encoding.ASCII.GetBytes("negative some test data");

            var expectedDescyptedDataAsString = "c29tZSB0ZXN0IGRhdGE=";

            var encryptedData = Encryption.Encrypt(sampleData, Encoding.ASCII.GetBytes(secret));
            var decryptedData = Encryption.Decrypt(encryptedData, Encoding.ASCII.GetBytes(secret));

            Convert.ToBase64String(decryptedData).Should().NotBe(expectedDescyptedDataAsString);
        }

        [Test]
        public void Sign_Verify_Should_BeTrue()
        {
            var privateKey = "UnYhOhNnHaMI9WhRo0lEhNACsiLUH4Frb0EM0PLHFOQ=";
            var publicKey = "BN8Er4HQvP41frQRmGgo0U+NQo+cyThbOlgupxSPfSvXlYmy/RQNYmreDQqQfORpstR8FpkQ0wMtkw/FPD+2AvM=";
            var sampleData = Encoding.ASCII.GetBytes("some test data");

            var signedData = Encryption.Sign(sampleData, privateKey);
            var verificationResult = Encryption.Verify(sampleData, signedData, publicKey);

            verificationResult.Should().BeTrue();
        }

        [Test]
        public void Sign_Verify_Should_BeFalse()
        {
            var privateKey = "UnYhOhNnHaMI9WhRo0lEhNACsiLUH4Frb0EM0PLHFOQ=";
            var publicKey = "BN8Er4HQvP41frQRmGgo0U+NQo+cyThbOlgupxSPfSvXlYmy/RQNYmreDQqQfORpstR8FpkQ0wMtkw/FPD+2AvM=";
            var sampleData = Encoding.ASCII.GetBytes("some test data");
            var incorrectSampleData = Encoding.ASCII.GetBytes("incorrect test data");

            var signedData = Encryption.Sign(sampleData, privateKey);
            var verificationResult = Encryption.Verify(incorrectSampleData, signedData, publicKey);

            verificationResult.Should().BeFalse();
        }

        [Theory]
        [TestCase("some test data", "MEQCICB13n0PAdkLLW7kbb7mZZnHGVvVVSu3N8x5wxrINMQEAiAL8zJkvlOPosl5Mm11FBkpFd/HTDssXDzujdXoY8/BpA==")]
        public void Sign_Verify_Should_BeCompatibleWithExisting(string sampleDataString, string legacySignature)
        {
            var publicKey = "BN8Er4HQvP41frQRmGgo0U+NQo+cyThbOlgupxSPfSvXlYmy/RQNYmreDQqQfORpstR8FpkQ0wMtkw/FPD+2AvM=";
            var sampleData = Encoding.ASCII.GetBytes(sampleDataString);
            var signature = Convert.FromBase64String(legacySignature);

            var verificationResult = Encryption.Verify(sampleData, signature, publicKey);

            verificationResult.Should().BeTrue();
        }

        [Theory]
        [TestCase("PKnKpnFgs0qvayZfnglSUe8si5ndQdcrjfkSrOpXYC0=")]
        public void Decrypt_Should_BeCompatibleWithExisting(string encriptedBase64)
        {
            var secret = "\"AMLuxuYGWba+VfwJrZjhoLLqsDHIdp5mIe707bACUN4M\"";

            var expectedDescyptedDataAsString = "c29tZSB0ZXN0IGRhdGE=";
            var encryptedData = Convert.FromBase64String(encriptedBase64);
            var decryptedData = Encryption.Decrypt(encryptedData, Encoding.ASCII.GetBytes(secret));

            Convert.ToBase64String(decryptedData).Should().Be(expectedDescyptedDataAsString);
        }

        [Test]
        [TestCase("MIIEvTCCA6WgAwIBAgIEUfqFczANBgkqhkiG9w0BAQsFADCBoTELMAkGA1UEBgwCQVQxSDBGBgNVBAoMP0EtVHJ1c3QgR2VzLiBmLiBTaWNoZXJoZWl0c3N5c3RlbWUgaW0gZWxla3RyLiBEYXRlbnZlcmtlaHIgR21iSDEjMCEGA1UECwwaYS1zaWduLVByZW1pdW0tVGVzdC1TaWctMDIxIzAhBgNVBAMMGmEtc2lnbi1QcmVtaXVtLVRlc3QtU2lnLTAyMB4XDTE3MDQwNDAzMTM1MloXDTIyMDQwNDAzMTM1MlowYzELMAkGA1UEBgwCQVQxGTAXBgNVBAMMEFVJRDogQVRVNjg1NDE1NDQxFDASBgNVBAQMC0FUVTY4NTQxNTQ0MQwwCgYDVQQqDANVSUQxFTATBgNVBAUMDDYyODExMzYzNjQ5MDBZMBMGByqGSM49AgEGCCqGSM49AwEHA0IABEZrkgK0oI50k6gsOrI5juSwabfrr79GEoP91CD0yRHXApOh8WM2l8KLV3nWFL8KbKld2oyhic5rs7ZBc0SQGjmjggIDMIIB/zCBhAYIKwYBBQUHAQEEeDB2MEYGCCsGAQUFBzAChjpodHRwOi8vd3d3LmEtdHJ1c3QuYXQvY2VydHMvYS1zaWduLVByZW1pdW0tVGVzdC1TaWctMDIuY3J0MCwGCCsGAQUFBzABhiBodHRwOi8vb2NzcC10ZXN0LmEtdHJ1c3QuYXQvb2NzcDATBgNVHSMEDDAKgAhGBp+OQY4VvTAnBggrBgEFBQcBAwEB/wQYMBYwCAYGBACORgEBMAoGCCsGAQUFBwsBMBEGA1UdDgQKBAhDC5kL3R5VmTAOBgNVHQ8BAf8EBAMCBsAwCQYDVR0TBAIwADBZBgNVHSAEUjBQMEQGBiooABEBCzA6MDgGCCsGAQUFBwIBFixodHRwOi8vd3d3LmEtdHJ1c3QuYXQvZG9jcy9jcC9hLXNpZ24tUHJlbWl1bTAIBgYEAIswAQEwga4GA1UdHwSBpjCBozCBoKCBnaCBmoaBl2xkYXA6Ly9sZGFwLXRlc3QuYS10cnVzdC5hdC9vdT1hLXNpZ24tUHJlbWl1bS1UZXN0LVNpZy0wMiAoU0hBLTI1Niksbz1BLVRydXN0LGM9QVQ/Y2VydGlmaWNhdGVyZXZvY2F0aW9ubGlzdD9iYXNlP29iamVjdGNsYXNzPWVpZENlcnRpZmljYXRpb25BdXRob3JpdHkwDQYJKoZIhvcNAQELBQADggEBAAyn30RdrZBP9q5W82fu4nVzMHTD6PvwY3fi2G/BKUlaknIRQCqqtEmWuXd9HH48qV+oXWs4hfr0jB69LnENiJv9rOq3aSowXLhwvzOmIBgBcY5Yb/eKjpXobRMgGmPQFejC7cjKSulNTJC9ROesim8BVRHn92pSjduicHBr65dtNpfoSqJfJTXhnp5i+wj3vu+DjzZQShpNon14z0boAkl1nnhagVDipu/V0vDoffAvfjHK03tEJ4oU3C1dwgFPOs9ea4xNR0VrTO1pbOscEnqVPCSOkY1Dqlz/jgurSYhQnFBVCi2lp+kL2Y4RHvFptJkj4Gzh9s3jsSdn6cGKzuc=")]
        public void VerifyWithCertificateBase64_ReceiptSignature_WithECCSquaringIssue(string certBase64)
        {
            var data = Convert.FromBase64String("ZXlKaGJHY2lPaUpGVXpJMU5pSjkuWDFJeExVRlVNVjlUWTJWdVlYUnBielJmWm5Rekl6RmZNakF4Tnkwd05TMHdNMVF4TkRvME5UbzFORjh3TERBd1h6QXNNREJmTUN3d01GOHdMREF3WHpBc01EQmZLMU4xUlZobFZUMWZOVEZtWVRnMU56TmZOVTFOYUZCVmQzbDZabWM5");
            var signature = Convert.FromBase64String("MEYCIQB8J6Yojpai13vHRlYXdM/msx1JxHmZx4kMQ8IavF+/0QIhABv+htGTxPIjRonBTfjrYhLurZxxd4fJPxC80QMzF+hh");

            var verificationResult = Encryption.VerifyWithCertificateBase64(data, signature, certBase64);

            verificationResult.Should().BeTrue();
        }
    }
}
