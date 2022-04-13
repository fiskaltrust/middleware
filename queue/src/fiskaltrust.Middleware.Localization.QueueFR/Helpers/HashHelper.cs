using System;
using System.IO;
using System.Security.Cryptography;

namespace fiskaltrust.Middleware.Localization.QueueFR.Helpers
{
    public static class HashHelper
    {
        public static string ComputeSHA256Base64Url(string fileName)
        {
            using var stream = File.OpenRead(fileName);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(stream);

            var base64 = Convert.ToBase64String(hash);
            
            return base64.TrimEnd(new char[] { '=' }).Replace('+', '-').Replace('/', '_');
        }
    }
}
