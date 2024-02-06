using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Communication.Helpers;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Exceptions;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Models;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Tar;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Communication
{
    public class FccAdminApiProvider
    {
        private readonly DeutscheFiskalSCUConfiguration _configuration;
        private readonly ILogger<FccAdminApiProvider> _logger;
        private readonly Uri _baseAddress;
        private readonly ConcurrentDictionary<Guid, List<(DateTime, DateTime)>> _splitExports = new ConcurrentDictionary<Guid, List<(DateTime, DateTime)>>();

        public FccAdminApiProvider(DeutscheFiskalSCUConfiguration configuration, ILogger<FccAdminApiProvider> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _baseAddress = FccUriHelper.GetFccUri(configuration);
        }

        public async Task<List<ClientResponseDto>> GetClientsAsync()
        {
            using var client = GetBasicAuthAdminClient();
            var response = await client.GetAsync("clients").ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<List<ClientResponseDto>>(responseContent);
            }

            throw new FiskalCloudException($"Communication error ({response.StatusCode}) while getting registered clients (GET /clients). Response: {responseContent}",
                (int) response.StatusCode, ErrorHelper.GetErrorType(responseContent), "GET /clients");
        }

        public async Task CreateClientAsync(string clientId)
        {
            var request = new CreateClientRequestDto
            {
                ClientId = clientId,
                ErsIdentifier = clientId,
                RegistrationToken = _configuration.ActivationToken,
                BriefDescription = clientId,
                TypeOfSystem = "Default"
            };

            using var client = GetBasicAuthAdminClient();
            var requestContent = JsonConvert.SerializeObject(request);
            var response = await client.PostAsync("registration", new StringContent(requestContent, Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new FiskalCloudException($"Communication error ({response.StatusCode}) while registering client (POST /registration). Request: '{requestContent}', Response: '{responseContent}'",
                    (int) response.StatusCode, ErrorHelper.GetErrorType(responseContent), "POST /registration");
            }
        }

        public async Task DeregisterClientAsync(string clientId)
        {
            var request = new DeregisterClientRequestDto
            {
                ClientId = clientId
            };

            using var client = GetBasicAuthAdminClient();
            var requestContent = JsonConvert.SerializeObject(request);
            var response = await client.PostAsync("deregistration", new StringContent(requestContent, Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new FiskalCloudException($"Communication error ({response.StatusCode}) while deregistering client (POST /deregistration). Request: '{requestContent}', Response: '{responseContent}'",
                    (int) response.StatusCode, ErrorHelper.GetErrorType(responseContent), "POST /deregistration");
            }
        }

        public async Task<FccInfoResponseDto> GetFccInfoAsync()
        {
            using var client = GetBasicAuthAdminClient();

            var response = await client.GetAsync("info");
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<FccInfoResponseDto>(responseContent);
            }

            throw new FiskalCloudException($"Communication error ({response.StatusCode}) while requesting FCC info (GET /info). Response: '{responseContent}'",
                (int) response.StatusCode, ErrorHelper.GetErrorType(responseContent), "GET /info");
        }

        public async Task<SelfCheckResponseDto> GetSelfCheckResultAsync()
        {
            using var client = GetOAuthAdminClient();
            var response = await client.GetAsync("selfcheck");
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<SelfCheckResponseDto>(responseContent);
            }

            throw new FiskalCloudException($"Communication error ({response.StatusCode}) while getting self check result (GET /selfcheck). Response: '{responseContent}'",
                (int) response.StatusCode, ErrorHelper.GetErrorType(responseContent), "GET /selfcheck");
        }

        public async Task<TssDetailsResponseDto> GetTssDetailsAsync()
        {
            using var client = GetOAuthAdminClient();
            var response = await client.GetAsync("tssdetails");
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<TssDetailsResponseDto>(responseContent);
            }

            throw new FiskalCloudException($"Communication error ({response.StatusCode}) while getting TSS details (GET /tssdetails). Response: '{responseContent}'",
                (int) response.StatusCode, ErrorHelper.GetErrorType(responseContent), "GET /tssdetails");
        }

        public async Task<byte[]> ExportSingleTransactionAsync(ulong transactionNumber)
        {
            using var client = GetOAuthAdminClient();
            var response = await client.GetAsync($"export/transactions/{transactionNumber}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Convert.FromBase64String(responseContent);
            }

            throw new FiskalCloudException($"Communication error ({response.StatusCode}) while exporting single transaction (GET /export/transactions/{transactionNumber}). Response: '{responseContent}'",
                (int) response.StatusCode, ErrorHelper.GetErrorType(responseContent), $"GET /export/transactions/{transactionNumber}");
        }

        public async Task RequestExportAsync(Guid exportId, string targetFile, DateTime startDate, DateTime endDate, string clientId = null, bool isSplit = false)
        {
            var url = $"export/transactions/time?startDate={startDate:yyyy-MM-dd'T'HH:mm:ss'Z'}&endDate={endDate:yyyy-MM-dd'T'HH:mm:ss'Z'}";
            if (clientId != null)
            {
                url += $"&clientId={clientId}";
            }

            var response = await GetOAuthAdminClient().GetAsync(url);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                if (!isSplit)
                {
                    File.WriteAllBytes(targetFile, Array.Empty<byte>());
                }
                return;
            }

            if (response.IsSuccessStatusCode)
            {
                // Deserialize base64 response without loading it into memory. Bold move of Microsoft to call this "CryptoStream", but who am I to judge.
                using var contentStream = new CryptoStream(await response.Content.ReadAsStreamAsync(), new FromBase64Transform(), CryptoStreamMode.Read);
                if (isSplit)
                {
                    TarFileHelper.AppendTarStreamToTarFile(targetFile, contentStream);
                    AddSplitAcknowledgment(exportId, startDate, endDate);
                }
                else
                {
                    using var fileStream = File.Create(targetFile);
                    contentStream.CopyTo(fileStream);
                }
            }
            else
            {
                if (response.StatusCode.Equals(HttpStatusCode.RequestEntityTooLarge))
                {
                    _logger.LogDebug("Requested export was too large to be processed by the FCC, splitting into multiple export requests.");
                    await RequestSplitExportAsync(exportId, targetFile, startDate, endDate, clientId).ConfigureAwait(false);
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    throw new FiskalCloudException($"Communication error ({response.StatusCode}) while requesting export (GET {url}). Response: '{responseContent}",
                        (int) response.StatusCode, ErrorHelper.GetErrorType(responseContent), $"GET /{url}");
                }
            }

            // Finalize merged TAR file after last batch was processed
            if (IsSplitExport(exportId) && !isSplit)
            {
                TarFileHelper.FinalizeTarFile(targetFile);
                _logger.LogDebug("Finalized merged TAR file {fileName}.", targetFile);
            }
        }

        public bool IsSplitExport(Guid exportId)
        {
            return _splitExports.ContainsKey(exportId);
        }

        private void AddSplitAcknowledgment(Guid exportId, DateTime startDate, DateTime endDate)
        {
            _splitExports.AddOrUpdate(exportId, new List<(DateTime, DateTime)>() { (startDate, endDate) }, (k, v) => { v.Add((startDate, endDate)); return v; });
        }

        private async Task RequestSplitExportAsync(Guid exportId, string targetFile, DateTime startDate, DateTime endDate, string clientId)
        {
            var difference = endDate - startDate;
            if (difference.TotalSeconds > 0)
            {
                var half = difference.TotalSeconds / 2;
                var newStartDate = startDate.AddSeconds(half);
                var newEndDate = startDate.AddSeconds(half - 1);

                _logger.LogDebug("Split large export into two parts: 1) {firstStartDate}-{firstEndDate}, 2) {firstStartDate}-{secondEndDate}", newStartDate, endDate, startDate, newEndDate);
                await RequestExportAsync(exportId, targetFile, newStartDate, endDate, clientId, true).ConfigureAwait(false);
                await RequestExportAsync(exportId, targetFile, startDate, newEndDate, clientId, true).ConfigureAwait(false);
            }
        }

        public async Task AcknowledgeSplitTransactionsAsync(Guid exportId, string clientId = null)
        {
            if (_splitExports.TryGetValue(exportId, out var _splitExportDatetimes))
            {
                foreach ((var startDateTime, var endDateTime) in _splitExportDatetimes)
                {
                    await AcknowledgeAllTransactionsAsync(startDateTime, endDateTime, clientId).ConfigureAwait(false);
                }
                _splitExports.TryRemove(exportId, out _);
            }
        }

        public void RemoveSplitExportIfExists(string exportId)
        {
            try
            {
                var gExportID = Guid.Parse(exportId);
                if (_splitExports.ContainsKey(gExportID))
                {
                    _splitExports.TryRemove(gExportID, out _);
                }
            }catch{}
        } 

        public async Task AcknowledgeAllTransactionsAsync(DateTime startDate, DateTime endDate, string clientId = null)
        {
            var url = $"export/transactions/time?startDate={startDate:yyyy-MM-dd'T'HH:mm:ss'Z'}&endDate={endDate:yyyy-MM-dd'T'HH:mm:ss'Z'}";
            if (clientId != null)
            {
                url += $"&clientId={clientId}";
            }

            var response = await GetOAuthAdminClient().PostAsync(url, new StringContent("ACK", Encoding.UTF8, "text/plain"));
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
            {
                throw new FiskalCloudException($"Communication error ({response.StatusCode}): {responseContent}",
                    (int) response.StatusCode, ErrorHelper.GetErrorType(responseContent), $"POST /{url}");
            }
        }

        public HttpClient GetBasicAuthAdminClient()
        {
            var client = new HttpClient { BaseAddress = _baseAddress, Timeout = TimeSpan.FromSeconds(_configuration.FCCTimeoutSec) };
            var credentials = Encoding.ASCII.GetBytes($"admin:{_configuration.ErsCode}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

            return client;
        }

        public HttpClient GetBasicAuthActuatorClient()
        {
            var client = new HttpClient { BaseAddress = _baseAddress, Timeout = TimeSpan.FromSeconds(_configuration.FCCTimeoutSec) };
            var credentials = Encoding.ASCII.GetBytes($"actuator:{_configuration.ErsCode}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

            return client;
        }

        public HttpClient GetOAuthAdminClient()
        {
            var clientConfig = new ClientConfiguration
            {
                BaseAddress = _baseAddress,
                UserName = "admin-auth-client-id",
                Password = "admin-auth-client-secret",
                GrantType = "password",
                AdditionalProperties = new Dictionary<string, string>
                {
                    { "username", "admin" },
                    { "password", _configuration.ErsCode },
                },
                Timeout = _configuration.FCCTimeoutSec
            };
            return new HttpClient(new AuthenticatedHttpClientHandler(clientConfig))
            {
                BaseAddress = _baseAddress
            };
        }
    }
}
