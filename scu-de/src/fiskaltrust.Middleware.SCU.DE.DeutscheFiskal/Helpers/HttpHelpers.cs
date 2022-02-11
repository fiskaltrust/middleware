using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Helpers
{
    public static class HttpHelpers
    {
        public static async Task<bool> IsAddressAvailable(string address)
        {
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) })
            {
                try
                {
                    var result = await client.GetAsync(address);
                    return result.IsSuccessStatusCode;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}
