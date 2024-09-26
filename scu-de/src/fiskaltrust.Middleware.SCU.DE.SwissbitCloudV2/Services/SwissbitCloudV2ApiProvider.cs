using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Helpers;
using Microsoft.Extensions.Logging;


namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Services
{
    public sealed class SwissbitCloudV2ApiProvider : ISwissbitCloudV2ApiProvider, IDisposable
    {
        private const int EXPORT_TIMEOUT_MS = 18000 * 1000;

        private readonly SwissbitCloudV2SCUConfiguration _configuration;
        private readonly HttpClientWrapper _httpClient;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly ILogger<SwissbitCloudV2ApiProvider> _logger;


        public SwissbitCloudV2ApiProvider(SwissbitCloudV2SCUConfiguration configuration, HttpClientWrapper httpClient, ILogger<SwissbitCloudV2ApiProvider> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
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

        public async Task<ExportDto> StartExportAsync()
        {
            var openExports = await GetExportsAsync();
            if (openExports.Count > 0)
            {
                _logger.LogInformation($"There is already an open export with id {openExports[0].Id}. Returning this export, as only one export can be processed by Swissbitcloudv2 at a time." +
                    $"To start a new export the old export data must be deleted and stored with a daily closing. If another daily closing is happening on another queue for this tse, data will be saved and deleted on that queue.");
                return openExports[0];
            }
            var response = await _httpClient.PostAsync($"/api/v1/tse/export", null).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<ExportDto>(await response.Content.ReadAsStringAsync());
        }

        public async Task<List<ExportDto>> GetExportsAsync()
        {
            var response = await _httpClient.GetAsync($"/api/v1/tse/export").ConfigureAwait(false);

            return JsonConvert.DeserializeObject<List<ExportDto>>(await response.Content.ReadAsStringAsync());
        }

        public async Task StoreDownloadResultAsync(ExportDto exportDto)
        {
            if(string.IsNullOrEmpty(exportDto.DownloadUrl))
            {
                exportDto = await WaitUntilExportFinishedAsync(exportDto.Id);
            }
            var contentStream = await GetExportFromResponseUrlAsync(exportDto);

            using var fileStream = File.Create(exportDto.Id.ToString());
            contentStream.Position = 0;
            contentStream.CopyTo(fileStream);
        }

        public async Task<Stream> GetExportFromResponseUrlAsync(ExportDto exportDto)
        {
            using var exportClient = new HttpClient { BaseAddress = new Uri(_configuration.ApiEndpoint), Timeout = TimeSpan.FromSeconds(_configuration.SwissbitCloudV2Timeout) };
            var credentials = Encoding.ASCII.GetBytes($"{_configuration.TseSerialNumber}:{_configuration.TseAccessToken}");
            exportClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

            var response = await exportClient.GetAsync($"{exportDto.DownloadUrl}");
          
            return await response.Content.ReadAsStreamAsync();
        }

        public async Task<ExportDto> GetExportStateResponseByIdAsync(string exportId)
        {
            var response = await _httpClient.GetAsync($"/api/v1/tse/export/{exportId}");
            var responseContent = await response.Content.ReadAsStringAsync();
            
            return JsonConvert.DeserializeObject<ExportDto>(responseContent);
        }

        public async Task<ExportDto> DeleteExportByIdAsync(string exportId)
        {
            var response = await _httpClient.DeleteAsync($"/api/v1/tse/export/{exportId}");
            var responseContent = await response.Content.ReadAsStringAsync();
           
            return JsonConvert.DeserializeObject<ExportDto>(responseContent);
        }

        private async Task<ExportDto> WaitUntilExportFinishedAsync(string exportId)
        {
            var sw = Stopwatch.StartNew();
            do
            {
                try
                {
                    var exportDto = await GetExportStateResponseByIdAsync(exportId);

                    if (exportDto.Status == "failure" || exportDto.Status == "success")
                    {
                        return exportDto;
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
