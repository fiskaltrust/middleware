using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.storage.serialization.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.ME.Test.Launcher.Helpers
{
    public class HelipadHelper
    {

        public static async Task<ftCashBoxConfiguration> GetConfigurationAsync(string cashBoxId, string accessToken)
        {

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri($"https://helipad-sandbox.fiskaltrust.cloud/");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("cashboxid", cashBoxId);
                httpClient.DefaultRequestHeaders.Add("accesstoken", accessToken);
                var result = await httpClient.GetAsync("api/configuration");
                var content = await result.Content.ReadAsStringAsync();
                if (result.IsSuccessStatusCode)
                {
                    if (string.IsNullOrEmpty(content))
                    {
                        throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid. (Statuscode: {result.StatusCode}, Content: '{result.Content}')");
                    }

                    var configuration = JsonConvert.DeserializeObject<ftCashBoxConfiguration>(content);
                    configuration.TimeStamp = DateTime.UtcNow.Ticks;
                    return configuration;
                }
                else
                {
                    throw new Exception($"{content}");
                }
            }
        }

    }
}
