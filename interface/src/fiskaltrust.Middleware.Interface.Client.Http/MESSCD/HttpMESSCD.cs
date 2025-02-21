using fiskaltrust.ifPOS.v1.me;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Http
{
    internal class HttpMESSCD : IMESSCD
    {
        private readonly HttpClient _httpClient;

        public HttpMESSCD(HttpMESSCDClientOptions options)
        {
            _httpClient = GetClient(options);
        }

        public async Task<ComputeIICResponse> ComputeIICAsync(ComputeIICRequest computeIICRequest) => await ExecuteHttpGetAsync<ComputeIICResponse>("v1", "ComputeIIC").ConfigureAwait(false);

        public async Task<RegisterInvoiceResponse> RegisterInvoiceAsync(RegisterInvoiceRequest registerInvoiceRequest) => await ExecuteHttpGetAsync<RegisterInvoiceResponse>("v1", "Invoice").ConfigureAwait(false);

        public async Task<RegisterTcrResponse> RegisterTcrAsync(RegisterTcrRequest registerTcrRequest) => await ExecuteHttpGetAsync<RegisterTcrResponse>("v1", "Register").ConfigureAwait(false);

        public async Task<RegisterCashDepositResponse> RegisterCashDepositAsync(RegisterCashDepositRequest registerCashDepositRequest) => await ExecuteHttpGetAsync<RegisterCashDepositResponse>("v1", "Deposit").ConfigureAwait(false);

        private async Task<T> ExecuteHttpGetAsync<T>(string urlVersion, string urlMethod)
        {
            var url = Path.Combine(urlVersion, urlMethod);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(result);
        }

        private async Task ExecuteHttpGetAsync(string urlVersion, string urlMethod)
        {
            var url = Path.Combine(urlVersion, urlMethod);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
        }

        private HttpClient GetClient(HttpMESSCDClientOptions options)
        {
            var url = options.Url.ToString().EndsWith("/") ? options.Url : new Uri($"{options.Url}/");
            if (options.DisableSslValidation.HasValue && options.DisableSslValidation.Value)
            {
                var handler = new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true
                };

                return new HttpClient(handler) { BaseAddress = url };
            }
            else
            {
                return new HttpClient { BaseAddress = url };
            }
        }

        public async Task UnregisterTcrAsync(UnregisterTcrRequest registerTCRRequest) => await ExecuteHttpGetAsync<bool>("v1", "Unregister").ConfigureAwait(false);

        public async Task RegisterCashWithdrawalAsync(RegisterCashWithdrawalRequest registerCashDepositRequest) => await ExecuteHttpGetAsync("v1", "Withdrawl").ConfigureAwait(false);

        public async Task<ScuMeEchoResponse> EchoAsync(ScuMeEchoRequest request) => await ExecuteHttpGetAsync<ScuMeEchoResponse>("v1", "Echo").ConfigureAwait(false);
    }
}