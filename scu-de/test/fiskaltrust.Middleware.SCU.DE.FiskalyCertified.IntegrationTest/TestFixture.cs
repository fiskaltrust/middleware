using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.IntegrationTest
{
    public sealed class TestFixture : IDisposable
    {
        public FiskalySCUConfiguration Configuration { get; }
        public Guid ClientId { get; } = Guid.NewGuid();

        public TestFixture()
        {
#if NET461
            System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
#endif
            Configuration = new FiskalySCUConfiguration
            {
                ApiSecret = "",
                ApiKey = "",
                TssId = Guid.NewGuid(),
                AdminPin = ""
            };
            try
            {
                CreateAndPersonalizeTssAsync().Wait();
                RegisterClientAsync().Wait();
            }
            catch
            {
                DisableTss().Wait();
            }
        }

        private async Task RegisterClientAsync()
        {
            var apiProvider = new FiskalyV2ApiProvider(Configuration, new HttpClientWrapper(Configuration, Mock.Of<ILogger<HttpClientWrapper>>()));
            var scu = new FiskalySCU(Mock.Of<ILogger<FiskalySCU>>(), apiProvider, new ClientCache(apiProvider), Configuration);

            await scu.RegisterClientIdAsync(new RegisterClientIdRequest { ClientId = ClientId.ToString() });
        }

        public void Dispose()
        {
            DisableTss().Wait();
        }

        private async Task CreateAndPersonalizeTssAsync()
        {
            using var client = GetOAuthHttpClient(Configuration);
            var createResponse = await client.PutAsync($"tss/{Configuration.TssId}", new StringContent("{}", Encoding.UTF8, "application/json"));
            createResponse.EnsureSuccessStatusCode();
            dynamic tssResponse = JsonConvert.DeserializeObject(await createResponse.Content.ReadAsStringAsync());
            string adminPuk = tssResponse.admin_puk;

            // Change TSE state from CREATED to UNINITIALIZED
            var personalizeRequest = new HttpRequestMessage(new HttpMethod("PATCH"), $"tss/{Configuration.TssId}")
            {
                Content = new StringContent("{\"state\": \"UNINITIALIZED\"}", Encoding.UTF8, "application/json")
            };
            var personalizeResponse = await client.SendAsync(personalizeRequest);
            personalizeResponse.EnsureSuccessStatusCode();

            // Set admin PIN and login
            var pinChangeRequest = new HttpRequestMessage(new HttpMethod("PATCH"), $"tss/{Configuration.TssId}/admin")
            {
                Content = new StringContent($"{{\"admin_puk\": \"{adminPuk}\", \"new_admin_pin\": \"{Configuration.AdminPin}\"}}", Encoding.UTF8, "application/json")
            };
            var pinChangeRResponse = await client.SendAsync(pinChangeRequest);
            pinChangeRResponse.EnsureSuccessStatusCode();

            var authResponse = await client.PostAsync($"tss/{Configuration.TssId}/admin/auth", new StringContent($"{{\"admin_pin\": \"{Configuration.AdminPin}\"}}", Encoding.UTF8, "application/json"));
            authResponse.EnsureSuccessStatusCode();

            // Change TSE state from UNINITIALIZED to INITIALIZED
            var initRequest = new HttpRequestMessage(new HttpMethod("PATCH"), $"tss/{Configuration.TssId}")
            {
                Content = new StringContent("{\"state\": \"INITIALIZED\"}", Encoding.UTF8, "application/json")
            };
            var initResponse = await client.SendAsync(initRequest);
            initResponse.EnsureSuccessStatusCode();
        }

        private async Task DisableTss()
        {
            using var client = GetOAuthHttpClient(Configuration);

            var authResponse = await client.PostAsync($"tss/{Configuration.TssId}/admin/auth", new StringContent($"{{\"admin_pin\": \"{Configuration.AdminPin}\"}}", Encoding.UTF8, "application/json"));
            authResponse.EnsureSuccessStatusCode();

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"tss/{Configuration.TssId}")
            {
                Content = new StringContent("{\"state\": \"DISABLED\"}", Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        private HttpClient GetOAuthHttpClient(FiskalySCUConfiguration configuration)
        {
            var url = configuration.ApiEndpoint.EndsWith("/") ? configuration.ApiEndpoint : $"{configuration.ApiEndpoint}/";
            return new HttpClient(new AuthenticatedHttpClientHandler(configuration, Mock.Of<ILogger<AuthenticatedHttpClientHandler>>()))
            {
                BaseAddress = new Uri(url)
            };
        }
    }
}
