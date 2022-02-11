using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Communication.Helpers;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Constants;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Exceptions;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Communication
{
    public sealed class FccErsApiProvider : IDisposable
    {
        private readonly Dictionary<string, HttpClient> _clients;
        private readonly Uri _baseAddress;
        private readonly DeutscheFiskalSCUConfiguration _configuration;

        public FccErsApiProvider(DeutscheFiskalSCUConfiguration configuration)
        {
            _baseAddress = FccUriHelper.GetFccUri(configuration);
            _clients = new Dictionary<string, HttpClient>();
            _configuration = configuration;
        }

        public async Task<StartTransactionResponseDto> StartTransactionRequestAsync(StartTransactionRequestDto transactionRequest)
        {
            var jsonPayload = JsonConvert.SerializeObject(transactionRequest, new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await GetClient(transactionRequest.ClientId).PostAsync("transaction", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<StartTransactionResponseDto>(responseContent);
            }

            throw new FiskalCloudException($"Communication error ({response.StatusCode}) while starting transaction (POST /transaction). Request: '{jsonPayload}', Response: '{responseContent}'", 
                (int) response.StatusCode, ErrorHelper.GetErrorType(responseContent), "POST /transaction");
        }

        public async Task<UpdateTransactionResponseDto> UpdateTransactionRequestAsync(ulong transactionNumber, UpdateTransactionRequestDto transactionRequest)
        {
            var jsonPayload = JsonConvert.SerializeObject(transactionRequest, new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await GetClient(transactionRequest.ClientId).PostAsync($"transaction/{transactionNumber}", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<UpdateTransactionResponseDto>(responseContent);
            }

            throw new FiskalCloudException($"Communication error ({response.StatusCode}) while updating transaction (POST transaction/{transactionNumber}). Request: '{jsonPayload}', Response '{responseContent}'", 
                (int) response.StatusCode, ErrorHelper.GetErrorType(responseContent), $"POST /transaction/{transactionNumber}");
        }

        public async Task<FinishTransactionResponseDto> FinishTransactionRequestAsync(ulong transactionNumber, FinishTransactionRequestDto transactionRequest)
        {
            var jsonPayload = JsonConvert.SerializeObject(transactionRequest, new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await GetClient(transactionRequest.ClientId).PutAsync($"transaction/{transactionNumber}", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<FinishTransactionResponseDto>(responseContent);
            }

            throw new FiskalCloudException($"Communication error ({response.StatusCode}) while finishing transaction (PUT transaction/{transactionNumber}). Request: '{jsonPayload}', Response '{responseContent}'",
                (int) response.StatusCode, ErrorHelper.GetErrorType(responseContent), $"PUT /transaction/{transactionNumber}");
        }

        public async Task<IEnumerable<OpenTransactionResponseDto>> GetStartedTransactionsAsync(IEnumerable<string> clientIds)
        {
            var transactions = new List<OpenTransactionResponseDto>();

            foreach (var clientId in clientIds)
            {
                transactions.AddRange(await GetStartedTransactionsAsync(clientId));
            }

            return transactions;
        }

        public async Task<IEnumerable<OpenTransactionResponseDto>> GetStartedTransactionsAsync(string clientId)
        {
            var response = await GetClient(clientId).GetAsync($"transactions?clientId={clientId}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Enumerable.Empty<OpenTransactionResponseDto>();
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<IEnumerable<OpenTransactionResponseDto>>(responseContent);
            }

            throw new FiskalCloudException($"Communication error ({response.StatusCode}) while getting started transactions (GET /transactions?clientId={clientId}). Response '{responseContent}'",
                (int) response.StatusCode, ErrorHelper.GetErrorType(responseContent), $"GET /transactions?clientId={clientId}");
        }

        public void Dispose()
        {
            foreach (var client in _clients.Values ?? Enumerable.Empty<HttpClient>())
            {
                client?.Dispose();
            }
        }

        private HttpClient GetClient(string clientId)
        {
            if (_clients.TryGetValue(clientId, out var client))
            {
                return client;
            }

            var clientConfig = new ClientConfiguration
            {
                BaseAddress = _baseAddress,
                UserName = clientId,
                GrantType = "client_credentials",
                Password = _configuration.ErsCode
            };

            var newClient = new HttpClient(new AuthenticatedHttpClientHandler(clientConfig))
            {
                BaseAddress = _baseAddress
            };
            _clients[clientId] = newClient;
            return newClient;
        }
    }
}
