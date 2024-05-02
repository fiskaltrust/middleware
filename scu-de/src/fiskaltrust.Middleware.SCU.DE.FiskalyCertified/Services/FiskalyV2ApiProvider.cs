using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Exceptions;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models;
using Newtonsoft.Json;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Tar;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Services
{
    public sealed class FiskalyV2ApiProvider : IFiskalyApiProvider, IDisposable
    {
        public ConcurrentDictionary<Guid, List<SplitExportStateData>> SplitExports { get; set; } = new ConcurrentDictionary<Guid, List<SplitExportStateData>>();

        private const int EXPORT_TIMEOUT_MS = 18000 * 1000;

        private readonly FiskalySCUConfiguration _configuration;
        private readonly HttpClientWrapper _httpClient;
        private readonly JsonSerializerSettings _serializerSettings;


        public FiskalyV2ApiProvider(FiskalySCUConfiguration configuration, HttpClientWrapper httpClientWrapper)
        {
            _configuration = configuration;
            _httpClient = httpClientWrapper;
            _serializerSettings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
        }

        public async Task<TransactionDto> PutTransactionRequestAsync(Guid tssId, Guid transactionId, TransactionRequestDto transactionRequest)
        {
            var jsonPayload = JsonConvert.SerializeObject(transactionRequest, _serializerSettings);

            var response = await _httpClient.PutAsync($"tss/{tssId}/tx/{transactionId}?tx_revision=1", new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<TransactionDto>(responseContent);
            }

            throw new FiskalyException($"Communication error ({response.StatusCode}) while processing a transaction (PUT tss/{tssId}/tx/{transactionId}?tx_revision=1). Response: {responseContent}",
                (int) response.StatusCode, $"PUT tss/{tssId}/tx/{transactionId}?tx_revision=1");
        }

        public async Task<TransactionDto> PutTransactionRequestWithStateAsync(Guid tssId, ulong transactionNumber, long lastRevision, TransactionRequestDto transactionRequest)
        {
            var jsonPayload = JsonConvert.SerializeObject(transactionRequest, _serializerSettings);

            var response = await _httpClient.PutAsync($"tss/{tssId}/tx/{transactionNumber}?tx_revision={lastRevision}", new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<TransactionDto>(responseContent);
            }

            throw new FiskalyException($"Communication error ({response.StatusCode}) while processing a transaction (PUT tss/{tssId}/tx/{transactionNumber}?tx_revision={lastRevision}). Response: {responseContent}",
                (int) response.StatusCode, $"PUT tss/{tssId}/tx/{transactionNumber}?tx_revision={lastRevision}");
        }

        public async Task<TransactionDto> GetTransactionDtoAsync(Guid tssId, ulong transactionNumber)
        {
            var response = await _httpClient.GetAsync($"tss/{tssId}/tx/{transactionNumber}");
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<TransactionDto>(responseContent);
            }

            throw new FiskalyException($"Communication error ({response.StatusCode}) while getting a transaction (GET tss/{tssId}/tx/{transactionNumber}). Response: {responseContent}",
                (int) response.StatusCode, $"GET tss/{tssId}/tx/{transactionNumber}");
        }

        public async Task<IEnumerable<TransactionDto>> GetStartedTransactionsAsync(Guid tssId)
        {
            var response = await _httpClient.GetAsync($"tss/{tssId}/tx?states[]=ACTIVE");
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<CollectionResponse<TransactionDto>>(responseContent).Data;
            }

            throw new FiskalyException($"Communication error ({response.StatusCode}) while getting active transactions (GET tss/{tssId}/tx?states[]=ACTIVE). Response: {responseContent}",
                (int) response.StatusCode, $"GET tss/{tssId}/tx?states[]=ACTIVE");
        }

        public async Task<ExportStateInformationDto> GetExportStateInformationByIdAsync(Guid tssId, Guid exportId)
        {
            if (SplitExports.ContainsKey(exportId))
            {
                return await GetSplitExportStateInformationAsync(tssId, exportId);
            }
            else
            {
                var response = await _httpClient.GetAsync($"tss/{tssId}/export/{exportId}");
                var responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<ExportStateInformationDto>(responseContent);
                }
                throw new FiskalyException($"Communication error ({response.StatusCode}) while getting export state information (GET tss/{tssId}/export/{exportId}). Response: {responseContent}",
                (int) response.StatusCode, $"GET tss/{tssId}/export/{exportId}");
            }
        }

        private async Task<ExportStateInformationDto> GetSplitExportStateInformationAsync(Guid tssId, Guid exportId)
        {
            HttpResponseMessage response = null;
            foreach (var splitExport in SplitExports[exportId])
            {
                response = await _httpClient.GetAsync($"tss/{tssId}/export/{splitExport.ExportId}");
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    throw new FiskalyException($"Communication error ({response.StatusCode}) while getting export state information (GET tss/{tssId}/export/{splitExport.ExportId}). Response: {content}",
                                               (int) response.StatusCode, $"GET tss/{tssId}/export/{splitExport.ExportId}");
                }
            }
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ExportStateInformationDto>(responseContent);
        }

        public async Task<Dictionary<string, object>> GetExportMetadataAsync(Guid tssId, Guid exportId)
        {
            var response = await _httpClient.GetAsync($"tss/{tssId}/export/{exportId}/metadata");
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
            }

            throw new FiskalyException($"Communication error ({response.StatusCode}) while getting export metadata (GET tss/{tssId}/export/{exportId}/metadata). Response: {responseContent}",
                (int) response.StatusCode, $"GET tss/{tssId}/export/{exportId}/metadata");
        }

        public async Task RequestExportAsync(Guid tssId, ExportTransactions exportRequest, Guid exportId, long? fromTransactionNumber, long toTransactionNumber)
        {
            var query = $"?end_transaction_number={toTransactionNumber}";
            if (exportRequest.ClientId != default)
            {
                query += $"&client_id={exportRequest.ClientId}";
            }
            if (fromTransactionNumber != null)
            {
                query += $"&start_transaction_number={fromTransactionNumber}";
            }

            var response = await _httpClient.PutAsync($"tss/{tssId}/export/{exportId}?{query}", new StringContent("{}", Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new FiskalyException($"Communication error ({response.StatusCode}) while triggering export (PUT tss/{tssId}/export/{exportId}?{query}). Response: {responseContent}",
                    (int) response.StatusCode, $"PUT tss/{tssId}/export/{exportId}?{query}");
            }
        }

        public async Task SetExportMetadataAsync(Guid tssId, Guid exportId, long? fromTransactionNumber, long toTransactionNumber)
        {
            var metadata = new Dictionary<string, string>
            {
                {"start_transaction_number", fromTransactionNumber?.ToString() },
                {"end_transaction_number", toTransactionNumber.ToString() }
            };
            var jsonPayload = JsonConvert.SerializeObject(metadata, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });
            var response = await _httpClient.SendAsync(new HttpMethod("PATCH"), $"tss/{tssId}/export/{exportId}/metadata",jsonPayload);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new FiskalyException($"Communication error ({response.StatusCode}) while setting export metadata (PATCH tss/{tssId}/export/{exportId}/metadata). Response: {responseContent}",
                    (int) response.StatusCode, $"PATCH tss/{tssId}/export/{exportId}/metadata");
            }
        }

        public async Task RequestExportAsync(Guid tssId, ExportTransactionsWithTransactionNumberDto exportRequest, Guid exportId)
        {
            var query = $"?start_transaction_number={exportRequest.StartTransactionNumber}&end_transaction_number={exportRequest.EndTransactionNumber}";
            if (exportRequest.ClientId != default)
            {
                query += $"&client_id={exportRequest.ClientId}";
            }

            var response = await _httpClient.PutAsync($"tss/{tssId}/export/{exportId}?{query}", new StringContent("{}", Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new FiskalyException($"Communication error ({response.StatusCode}) while triggering export (PUT tss/{tssId}/export/{exportId}?{query}). Response: {responseContent}",
                    (int) response.StatusCode, $"PUT tss/{tssId}/export/{exportId}?{query}");
            }
        }

        public async Task RequestExportAsync(Guid tssId, ExportTransactionsWithDatesDto exportRequest, Guid exportId)
        {

            var query = $"?start_date={exportRequest.StartDate}&end_date={exportRequest.EndDate}";
            if (exportRequest.ClientId != default)
            {
                query += $"&client_id={exportRequest.ClientId}";
            }

            var response = await _httpClient.PutAsync($"tss/{tssId}/export/{exportId}?{query}", new StringContent("{}", Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new FiskalyException($"Communication error ({response.StatusCode}) while triggering export (PUT tss/{tssId}/export/{exportId}?{query}). Response: {responseContent}",
                    (int) response.StatusCode, $"PUT tss/{tssId}/export/{exportId}?{query}");
            }
        }

        public async Task StoreDownloadResultAsync(Guid tssId, Guid exportId)
        {
            var exportStateInformation = await WaitUntilExportFinished(tssId, exportId);
            var contentStream = await GetExportByExportStateAsync(exportStateInformation);

            using var fileStream = File.Create(exportId.ToString());
            contentStream.CopyTo(fileStream);
        }

        public async Task StoreDownloadSplitResultAsync(Guid tssId, SplitExportStateData splitExportStateData)
        {
            var exportStateInformation = await WaitUntilExportFinished(tssId, splitExportStateData.ExportId);
            var result = await GetExportByExportStateAsync(exportStateInformation);

            if (exportStateInformation.State == "COMPLETED")
            {
                TarFileHelper.AppendTarStreamToTarFile(splitExportStateData.ParentExportId.ToString(), result);
                splitExportStateData.ExportStateData.State = ExportState.Succeeded;
            }
            else
            {
                splitExportStateData.ExportStateData.State = ExportState.Failed ;
            }
        }

        private async Task<ExportStateInformationDto> WaitUntilExportFinished(Guid tssId, Guid exportId)
        {
            var sw = Stopwatch.StartNew();
            do
            {
                try
                {
                    var exportStateInformation = await GetExportStateInformationByIdAsync(tssId, exportId);
                    if (exportStateInformation.State == "ERROR" || exportStateInformation.State == "COMPLETED")
                    {
                        return exportStateInformation;
                    }
                    await Task.Delay(1000);
                }
                catch (Exception)
                {

                    throw;
                }
            } while (sw.ElapsedMilliseconds < EXPORT_TIMEOUT_MS);

            throw new TimeoutException($"Timeout of {EXPORT_TIMEOUT_MS}ms was reached while exporting the backup {exportId}.");
        }

        public async Task<TssDto> GetTseByIdAsync(Guid tssId)
        {
            var response = await _httpClient.GetAsync($"tss/{tssId}");
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<TssDto>(responseContent);
            }

            throw new FiskalyException($"Communication error ({response.StatusCode}) while getting TSS (GET tss/{tssId}). Response: {responseContent}",
                (int) response.StatusCode, $"GET tss/{tssId}");
        }

        public async Task<Stream> GetExportByExportStateAsync(ExportStateInformationDto exportStateInformation)
        {
            var response = await _httpClient.GetAsync($"tss/{exportStateInformation.TssId}/export/{exportStateInformation.Id}/file");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync();
            }

            throw new FiskalyException($"Communication error ({response.StatusCode}) while downloading TAR export (GET tss/{exportStateInformation.TssId}/export/{exportStateInformation.Id}/file).",
                (int) response.StatusCode, $"GET tss/{exportStateInformation.TssId}/export/{exportStateInformation.Id}/file");
        }

        public async Task<TssDto> PatchTseStateAsync(Guid tssId, TseStateRequestDto tseState)
        {
            return await RunAsAdminAsync(tssId, async () =>
            {
                var jsonPayload = JsonConvert.SerializeObject(tseState, _serializerSettings);
                var response = await _httpClient.SendAsync(new HttpMethod("PATCH"), $"tss/{tssId}", jsonPayload);
                var responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<TssDto>(responseContent);
                }

                throw new FiskalyException($"Communication error ({response.StatusCode}) while setting TSS state (PATCH tss/{tssId}) Response: {responseContent}",
                    (int) response.StatusCode, $"PATCH tss/{tssId}");
            });
        }

        public async Task<List<ClientDto>> GetClientsAsync(Guid tssId)
        {
            var response = await _httpClient.GetAsync($"tss/{tssId}/client");
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<CollectionResponse<ClientDto>>(responseContent).Data;
            }

            throw new FiskalyException($"Communication error ({response.StatusCode}) while getting TSS clients (GET tss/{tssId}/client). Response: {responseContent}",
                (int) response.StatusCode, $"GET tss/{tssId}/client");
        }

        public async Task CreateClientAsync(Guid tssId, string serialNumber, Guid clientId)
        {
            await RunAsAdminAsync(tssId, async () =>
            {
                var clientRequest = new ClientRequestDto { SerialNumber = serialNumber };
                var jsonPayload = JsonConvert.SerializeObject(clientRequest, _serializerSettings);

                var response = await _httpClient.PutAsync($"tss/{tssId}/client/{clientId}", new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    throw new FiskalyException($"Communication error ({response.StatusCode}) while creating a client (PUT tss/{tssId}/client/{clientId}). Response: {responseContent}",
                        (int) response.StatusCode, $"PUT tss/{tssId}/client/{clientId}");
                }
            });
        }

        public async Task DisableClientAsync(Guid tssId, string serialNumber, Guid clientId)
        {
            await RunAsAdminAsync(tssId, async () =>
            {
                var clientRequest = new { state = "DEREGISTERED" };
                var jsonPayload = JsonConvert.SerializeObject(clientRequest, _serializerSettings);
                var response = await _httpClient.SendAsync(new HttpMethod("PATCH"), $"tss/{tssId}/client/{clientId}", jsonPayload);

                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    throw new FiskalyException($"Communication error ({response.StatusCode}) while creating a client (PUT tss/{tssId}/client/{clientId}). Response: {responseContent}",
                        (int) response.StatusCode, $"PUT tss/{tssId}/client/{clientId}");
                }
            });
        }

        public async Task<Dictionary<string, object>> GetTseMetadataAsync(Guid tssId)
        {
            var response = await _httpClient.GetAsync($"tss/{tssId}/metadata");
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
            }

            throw new FiskalyException($"Communication error ({response.StatusCode}) while getting TSS metadata (GET tss/{tssId}/metadata). Response: {responseContent}",
                (int) response.StatusCode, $"GET tss/{tssId}/metadata");
        }

        public async Task PatchTseMetadataAsync(Guid tssId, Dictionary<string, object> metadata)
        {
            var jsonPayload = JsonConvert.SerializeObject(metadata, _serializerSettings);
            var response = await _httpClient.SendAsync(new HttpMethod("PATCH"), $"tss/{tssId}/metadata", jsonPayload);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new FiskalyException($"Communication error ({response.StatusCode}) while setting TSS metadata (PUT tss/{tssId}/metadata). Response: {responseContent}",
                    (int) response.StatusCode, $"PUT tss/{tssId}/metadata");
            }
        }

        private async Task LoginAdminAsync(Guid tssId)
        {
            var loginRequest = new LoginRequestDto { AdminPin = _configuration.AdminPin };
            var jsonPayload = JsonConvert.SerializeObject(loginRequest, _serializerSettings);

            var response = await _httpClient.PostAsync($"tss/{tssId}/admin/auth", new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new FiskalyException($"Communication error ({response.StatusCode}) while performing admin login (POST tss/{tssId}/admin/auth). Response: {responseContent}",
                    (int) response.StatusCode, $"POST tss/{tssId}/admin/auth");
            }
        }

        private async Task LogoutAdminAsync(Guid tssId)
        {
            var response = await _httpClient.PostAsync($"tss/{tssId}/admin/logout", new StringContent("{}", Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new FiskalyException($"Communication error ({response.StatusCode}) while performing admin logout (POST tss/{tssId}/admin/logout). Response: {responseContent}",
                    (int) response.StatusCode, $"POST tss/{tssId}/admin/logout");
            }
        }

        private async Task<T> RunAsAdminAsync<T>(Guid tssId, Func<Task<T>> method)
        {
            try
            {
                await LoginAdminAsync(tssId);
                return await method();
            }
            finally
            {
                try
                {
                    await LogoutAdminAsync(tssId);
                }
                catch { }
            }
        }

        private async Task RunAsAdminAsync(Guid tssId, Func<Task> method)
        {
            try
            {
                await LoginAdminAsync(tssId);
                await method();
            }
            finally
            {
                try
                {
                    await LogoutAdminAsync(tssId);
                }
                catch { }
            }
        }

        public void Dispose() => _httpClient?.Dispose();
    }
}
