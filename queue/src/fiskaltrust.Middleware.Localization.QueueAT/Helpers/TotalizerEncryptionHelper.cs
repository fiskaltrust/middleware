using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace fiskaltrust.Middleware.Localization.QueueAT.Helpers
{
    public static class TotalizerEncryptionHelper
    {
        private const int TOTALIZER_LENGTH_AT = 5;

        public static byte[] EncryptTotalizer(string cashBoxIdentification, string receiptIdentification, string keyBase64, decimal totalizerValue)
        {
            var iv = ComputeInitializationVector(cashBoxIdentification, receiptIdentification);
            var key = Convert.FromBase64String(keyBase64);

            var totalizerInt = Convert.ToInt64(totalizerValue * 100);
            var totalizerBytes = BitConverter.GetBytes(totalizerInt);

            if (BitConverter.IsLittleEndian)
            {
                totalizerBytes = totalizerBytes.Take(TOTALIZER_LENGTH_AT).ToArray();
                totalizerBytes = totalizerBytes.Reverse().ToArray();
            }
            else
            {
                totalizerBytes = totalizerBytes.Take(TOTALIZER_LENGTH_AT).ToArray();
            }

            var cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
            cipher.Init(true, new ParametersWithIV(ParameterUtilities.CreateKeyParameter("AES", key), iv));
            var encrypted = cipher.DoFinal(totalizerBytes);

            return encrypted.Take(TOTALIZER_LENGTH_AT).ToArray();
        }

        public static decimal DecryptTotalizer(string cashBoxIdentification, string receiptIdentification, string keyBase64, byte[] encryptedTotalizer)
        {
            var iv = ComputeInitializationVector(cashBoxIdentification, receiptIdentification);
            var key = Convert.FromBase64String(keyBase64);

            var cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
            cipher.Init(false, new ParametersWithIV(ParameterUtilities.CreateKeyParameter("AES", key), iv));

            var totalizerBytesDecrypted = cipher.DoFinal(encryptedTotalizer);

            var totalizerBytesShort = totalizerBytesDecrypted.Take(TOTALIZER_LENGTH_AT).ToArray();

            if (BitConverter.IsLittleEndian)
            {
                totalizerBytesShort = totalizerBytesShort.Reverse().ToArray();
            }
            Array.Resize(ref totalizerBytesShort, 8);

            var totalizerInt = BitConverter.ToInt64(totalizerBytesShort, 0);
            return Convert.ToDecimal(totalizerInt) / 100;
        }

        private static byte[] ComputeInitializationVector(string cashBoxIdentification, string receiptIdentification)
        {
            var rawContentBytes = Encoding.UTF8.GetBytes(cashBoxIdentification + receiptIdentification);
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(rawContentBytes).Take(16).ToArray();
        }
    }
}
