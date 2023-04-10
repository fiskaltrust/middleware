using Org.BouncyCastle.Math;
using Org.BouncyCastle.X509;

namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Helpers
{
    public static class CertificateHelpers
    {
        public static bool CompareSerialNumbers(byte[] certificate, string serialnumber)
        {
            if (certificate == null || serialnumber == null)
            {
                return false;
            }

            var certSerialNumber = serialnumber.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? new BigInteger(serialnumber.Substring(2), 16)
                : new BigInteger(serialnumber, 10);

            var certificates = new X509CertificateParser().ReadCertificates(certificate);
            foreach (X509Certificate c in certificates)
            {
                if (c.SerialNumber.ToString().Trim() == certSerialNumber.ToString().Trim())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
