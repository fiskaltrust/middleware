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
using System.Net.Http.Headers;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Helpers;


namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Services
{
    public sealed class SwissbitCloudV2ApiProvider : ISwissbitCloudV2ApiProvider, IDisposable
    {
        private const int EXPORT_TIMEOUT_MS = 18000 * 1000;

        private readonly SwissbitCloudV2SCUConfiguration _configuration;
        private readonly HttpClientWrapper _httpClient;
        private readonly JsonSerializerSettings _serializerSettings;


        public SwissbitCloudV2ApiProvider(SwissbitCloudV2SCUConfiguration configuration, HttpClientWrapper httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
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
            
            return JsonConvert.DeserializeObject<TransactionResponseDto>(responseContent);
        }

        public async Task<List<string>> GetClientsAsync()
        {
            var response = await _httpClient.GetAsync($"/api/v1/tse/clients");
            var responseContent = await response.Content.ReadAsStringAsync();
            
            return JsonConvert.DeserializeObject<List<string>>(responseContent);
        }
        public async Task<TseDto> GetTseStatusAsync()
        {
            var response = await _httpClient.GetAsync($"/api/v1/tse");
            var responseContent = await response.Content.ReadAsStringAsync();
            
            return JsonConvert.DeserializeObject<TseDto>(responseContent);
        }

        public async Task<TseDto> DisableTseAsync()
        {
            await _httpClient.PostAsync("/api/v1/tse/disableSecureElement", new StringContent("", Encoding.UTF8, "application/json"));
            
            return await GetTseStatusAsync();
        }

        public async Task<List<int>> GetStartedTransactionsAsync()
        {
            var response = await _httpClient.GetAsync($"/api/v1/tse/transactions");
            var responseContent = await response.Content.ReadAsStringAsync();
           
            return JsonConvert.DeserializeObject<List<int>>(responseContent);           
        }

        public async Task CreateClientAsync(ClientDto client)
        {
            var clientDto = new ClientDto { ClientId = client.ClientId };
            var jsonPayload = JsonConvert.SerializeObject(clientDto, _serializerSettings);

            await _httpClient.PostAsync($"/api/v1/tse/registerClient", new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
            
        }

        public async Task DeregisterClientAsync(ClientDto client)
        {
            var jsonPayload = JsonConvert.SerializeObject(client, _serializerSettings);

            await _httpClient.PostAsync($"/api/v1/tse/deregisterClient", new StringContent(jsonPayload, Encoding.UTF8, "application/json"));            
        }

        public async Task<StartExportResponseDto> StartExport()
        {
            var response = await _httpClient.PostAsync($"/api/v1/tse/export", null).ConfigureAwait(false);
           
            return JsonConvert.DeserializeObject<StartExportResponseDto>(await response.Content.ReadAsStringAsync());
        }

        public async Task StoreDownloadResultAsync(string exportId)
        {
            var exportStateResponse = await WaitUntilExportFinishedAsync(exportId);
            var contentStream = await GetExportFromResponseUrlAsync(exportStateResponse);

            using var fileStream = File.Create(exportId.ToString());
            contentStream.Position = 0;
            contentStream.CopyTo(fileStream);
        }

        public async Task<Stream> GetExportFromResponseUrlAsync(ExportStateResponseDto exportStateResponse)
        {
            using var exportClient = new HttpClient { BaseAddress = new Uri(_configuration.ApiEndpoint), Timeout = TimeSpan.FromSeconds(_configuration.SwissbitCloudV2Timeout) };
            var credentials = Encoding.ASCII.GetBytes($"{_configuration.TseSerialNumber}:{_configuration.TseAccessToken}");
            exportClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

            var response = await exportClient.GetAsync($"{exportStateResponse.DownloadUrl}");
          
            return await response.Content.ReadAsStreamAsync();
        }

        public async Task<ExportStateResponseDto> GetExportStateResponseByIdAsync(string exportId)
        {
            var response = await _httpClient.GetAsync($"/api/v1/tse/export/{exportId}");
            var responseContent = await response.Content.ReadAsStringAsync();
            
            return JsonConvert.DeserializeObject<ExportStateResponseDto>(responseContent);
        }

        public async Task<ExportStateResponseDto> DeleteExportByIdAsync(string exportId)
        {
            var response = await _httpClient.DeleteAsync($"/api/v1/tse/export/{exportId}");
            var responseContent = await response.Content.ReadAsStringAsync();
           
            return JsonConvert.DeserializeObject<ExportStateResponseDto>(responseContent);
        }

        private async Task<ExportStateResponseDto> WaitUntilExportFinishedAsync(string exportId)
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

        public void Dispose() => _httpClient?.Dispose();
       
    }
}
