using System;
using System.Text;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueFR.UnitTest.Helper
{
    public static class JwtTestHelper
    {
        public static string GenerateJwt(CopyPayload payload)
        {
            var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"));
            var body = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));
            var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"));

            return $"{header}.{body}.{signature}";
        }
    }
}