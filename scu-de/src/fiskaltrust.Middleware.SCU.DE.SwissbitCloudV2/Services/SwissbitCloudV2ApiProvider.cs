using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Exceptions;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models;
using Newtonsoft.Json;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Helpers;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Tar;
using System.Net.Http.Headers;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Services
{
    public sealed class SwissbitCloudV2ApiProvider : ISwissbitCloudV2ApiProvider, IDisposable
    {
        private const int EXPORT_TIMEOUT_MS = 18000 * 1000;

        private readonly SwissbitCloudV2SCUConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerSettings _serializerSettings;


        public SwissbitCloudV2ApiProvider(SwissbitCloudV2SCUConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = GetClient();
            _serializerSettings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
        }

        public async Task CreateClientAsync(ClientDto client)
        {
            var clientDto = new ClientDto { ClientId = client.ClientId };
            var jsonPayload = JsonConvert.SerializeObject(clientDto, _serializerSettings);

            var response = await _httpClient.PostAsync($"/api/v1/tse/registerClient", new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new SwissbitCloudV2Exception($"Communication error ({response.StatusCode}) while creating a client (POST api/v1/tse/registerClient). Response: {responseContent}",
                    (int) response.StatusCode, $"PUT api/v1/tse/registerClient");
            }

        }
        private HttpClient GetClient()
        {
            var client = new HttpClient { BaseAddress = new Uri(_configuration.ApiEndpoint), Timeout = TimeSpan.FromSeconds(_configuration.SwissbitCloudV2Timeout) };
            var credentials = Encoding.ASCII.GetBytes($"{_configuration.SerialNumber}:{_configuration.AccessToken}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));
            client.DefaultRequestHeaders.Add("REQUEST-ID", Guid.NewGuid().ToString());

            return client;
        }
        public void Dispose() => _httpClient?.Dispose();

       

    }
}
