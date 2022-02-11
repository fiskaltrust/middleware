using System;
using System.IO;
using System.Security.Cryptography;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Helpers
{
    public static class ChecksumHelper
    {
        public static string GetSha256FromFile(string path)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(path);
            
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }
}
