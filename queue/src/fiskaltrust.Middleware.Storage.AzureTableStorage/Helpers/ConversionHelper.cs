using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Helpers
{
    internal static class ConversionHelper
    {
        public static string ToBase64UrlString(byte[] bytes)
        {
            var base64 = Convert.ToBase64String(bytes);
            return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static byte[] FromBase64UrlString(string base64urlString)
        {
            var base64 = base64urlString.Replace('_', '/').Replace('-', '+');
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }
            return Convert.FromBase64String(base64);
        }

        public static string ReplaceNonAllowedKeyCharacters(string partitionOrRowKey)
        {
            const char replacementChar = '_';

            if (string.IsNullOrEmpty(partitionOrRowKey))
            {
                return partitionOrRowKey;
            }
            return partitionOrRowKey
                .Replace('/', replacementChar)
                .Replace('\\', replacementChar)
                .Replace('#', replacementChar)
                .Replace('?', replacementChar);
        }
    }
}

