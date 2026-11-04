using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;

namespace fiskaltrust.Middleware.Journal.DE.Common.Helpers
{
    public static class Conversion
    {
        public static string From2DigitsTo3DigitsCountryCode(string country)
        {
            try
            {
                var ri = new RegionInfo(country);
                return ri.ThreeLetterISORegionName;
            }
            catch (ArgumentException)
            {
                return country;
            }
        }

        public static string FromTsePublicKeyToSerialNumber(string publicKeyBase64)
        {
            if (string.IsNullOrEmpty(publicKeyBase64))
            {
                return null;
            }
            var publicKey = Convert.FromBase64String(publicKeyBase64);
            if (IsFiskalyPublicKey(publicKey))
            {
                // Unlike all other vendors, Fiskaly only uses the last 65 byte to skip the key header (...)
                publicKey = publicKey.Skip(Math.Max(0, publicKey.Length - 65)).ToArray();
            }
            else if (IsAsn1Sequence(publicKey))
            {
                // If the byte array is an ASN.1 sequence, it must be extracted first (e.g. for DF TSEs)
                // https://documentation.fiskal.cloud/md/fiskal_cloud_implementation/checklist_compute_serial/
                publicKey = publicKey.Skip(23).ToArray();
            }

            using (var hasher = SHA256.Create())
            {
                var hash = hasher.ComputeHash(publicKey);
                return BitConverter.ToString(hash).ToLower().Replace("-", "");
            }
        }

        private static bool IsFiskalyPublicKey(byte[] publicKey)
        {
            var fiskalyHeader = new byte[] { 48, 89, 48, 19, 6, 7, 42, 134, 72, 206, 61, 2, 1, 6, 8, 42, 134, 72, 206, 61, 3, 1, 7, 3 };
            return publicKey.Length > 24 && publicKey.Take(24).SequenceEqual(fiskalyHeader);
        }

        private static bool IsAsn1Sequence(byte[] publicKey) => publicKey.Length > 0 && (publicKey[0] == 0x30 || publicKey[0] == 0x48);
    }
}
