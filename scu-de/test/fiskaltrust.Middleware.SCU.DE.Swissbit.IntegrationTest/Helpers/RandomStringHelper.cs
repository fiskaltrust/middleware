using System;
using System.Linq;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.IntegrationTest.Helpers
{
    public static class RandomStringHelper
    {
        private static readonly Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
