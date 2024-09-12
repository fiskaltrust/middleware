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
using System.Security.Cryptography.X509Certificates;

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

        public async Task<TransactionResponseDto> TransactionAsync(string transactionType, TransactionRequestDto transactionRequest)
        {
            var jsonPayload = JsonConvert.SerializeObject(transactionRequest, _serializerSettings);

            var response = await _httpClient.PostAsync($"/api/v1/tse/{transactionType}", new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<TransactionResponseDto>(responseContent);
            }

            throw new SwissbitCloudV2Exception($"Communication error ({response.StatusCode}) while creating a client (POST api/v1/tse/{transactionType}). Response: {responseContent}",
                    (int) response.StatusCode, $"POST api/v1/tse/{transactionType}");
        }

        public async Task<List<string>> GetClientsAsync()
        {
             var response = await _httpClient.GetAsync($"/api/v1/tse/clients");
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<List<string>>(responseContent);
            }

            throw new SwissbitCloudV2Exception($"Communication error ({response.StatusCode}) while creating a client (GET /api/v1/tse/clients). Response: {responseContent}",
                    (int) response.StatusCode, $"GET /api/v1/tse/clients");

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

        public async Task<StartExportResponse> StartExport()
        {
            var response = await _httpClient.PostAsync($"/api/v1/tse/export", null).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new SwissbitCloudV2Exception($"Communication error ({response.StatusCode}) while starting the export (POST api/v1/tse/export). Response: {responseContent}",
                    (int) response.StatusCode, $"PUT api/v1/tse/export");
            }
            return JsonConvert.DeserializeObject<StartExportResponse>(await response.Content.ReadAsStringAsync());
        }

        public async Task StoreDownloadResultAsync(string exportId)
        {
            var exportStateResponse = await WaitUntilExportFinishedAsync(exportId);
            var contentStream = await GetExportByExportStateAsync(exportStateResponse);

            using var fileStream = File.Create(exportId.ToString());
            contentStream.Position = 0;
            contentStream.CopyTo(fileStream);
        }

        public async Task<Stream> GetExportByExportStateAsync(ExportStateResponse exportStateResponse)
        {
            using var exportClient = new HttpClient { BaseAddress = new Uri(_configuration.ApiEndpoint), Timeout = TimeSpan.FromSeconds(_configuration.SwissbitCloudV2Timeout) };
            var credentials = Encoding.ASCII.GetBytes($"{_configuration.TseSerialNumber}:{_configuration.TseAccessToken}");
            exportClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

            var response = await exportClient.GetAsync($"{exportStateResponse.DownloadUrl}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync();
            }

            throw new SwissbitCloudV2Exception($"Communication error ({response.StatusCode}) while downloading TAR export (GET {exportStateResponse.DownloadUrl}/{exportStateResponse.Filename}).",
                (int) response.StatusCode, $"GET {exportStateResponse.DownloadUrl}/{exportStateResponse.Filename}");
        }

        public async Task<ExportStateResponse> GetExportStateResponseByIdAsync(string exportId)
        {
            var response = await _httpClient.GetAsync($"/api/v1/tse/export/{exportId}");
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ExportStateResponse>(responseContent);
            }
            throw new SwissbitCloudV2Exception($"Communication error ({response.StatusCode}) while getting export state information (GET /api/v1/tse/export/{exportId}). Response: {responseContent}",
            (int) response.StatusCode, $"GET /api/v1/tse/export/{exportId}");
        }

        private async Task<ExportStateResponse> WaitUntilExportFinishedAsync(string exportId)
        {
            var sw = Stopwatch.StartNew();
            do
            {
                try
                {
                    var exportStateResponse = await GetExportStateResponseByIdAsync(exportId);

                    if (exportStateResponse.State == "failure" || exportStateResponse.State == "success")
                    {
                        return exportStateResponse;
                    }
                    await Task.Delay(5000);
                }
                catch (Exception)
                {
                    throw;
                }
            } while (sw.ElapsedMilliseconds < EXPORT_TIMEOUT_MS);

            throw new TimeoutException($"Timeout of {EXPORT_TIMEOUT_MS}ms was reached while exporting the backup {exportId}.");
        }

        private HttpClient GetClient()
        {
            var client = new HttpClient { BaseAddress = new Uri(_configuration.ApiEndpoint), Timeout = TimeSpan.FromSeconds(_configuration.SwissbitCloudV2Timeout) };
            var credentials = Encoding.ASCII.GetBytes($"{_configuration.TseSerialNumber}:{_configuration.TseAccessToken}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));
           
            return client;
        }
        public void Dispose() => _httpClient?.Dispose();

       

    }
}
