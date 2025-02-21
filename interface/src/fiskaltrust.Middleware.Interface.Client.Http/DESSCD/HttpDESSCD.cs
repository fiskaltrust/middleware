using fiskaltrust.ifPOS.v1.de;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Http
{
    internal class HttpDESSCD : IDESSCD
    {
        private readonly HttpClient _httpClient;

        public HttpDESSCD(HttpDESSCDClientOptions options)
        {
            _httpClient = GetClient(options);
        }

        public async Task<ExportDataResponse> ExportDataAsync() => await ExecuteHttpGetAsync<ExportDataResponse>("v1", "exportdata").ConfigureAwait(false);

        public async Task<FinishTransactionResponse> FinishTransactionAsync(FinishTransactionRequest request) => await ExecuteHttpPostAsync<FinishTransactionResponse>("v1", "finishtransaction", request).ConfigureAwait(false);

        public async Task<TseInfo> GetTseInfoAsync() => await ExecuteHttpGetAsync<TseInfo>("v1", "tseinfo").ConfigureAwait(false);

        public async Task<TseState> SetTseStateAsync(TseState state) => await ExecuteHttpPostAsync<TseState>("v1", "tsestate", state).ConfigureAwait(false);

        public async Task<StartTransactionResponse> StartTransactionAsync(StartTransactionRequest request) => await ExecuteHttpPostAsync<StartTransactionResponse>("v1", "starttransaction", request).ConfigureAwait(false);

        public async Task<UpdateTransactionResponse> UpdateTransactionAsync(UpdateTransactionRequest request) => await ExecuteHttpPostAsync<UpdateTransactionResponse>("v1", "updatetransaction", request).ConfigureAwait(false);

        public async Task<RegisterClientIdResponse> RegisterClientIdAsync(RegisterClientIdRequest request) => await ExecuteHttpPostAsync<RegisterClientIdResponse>("v1", "registerclientid", request).ConfigureAwait(false);

        public async Task<UnregisterClientIdResponse> UnregisterClientIdAsync(UnregisterClientIdRequest request) => await ExecuteHttpPostAsync<UnregisterClientIdResponse>("v1", "unregisterclientid", request).ConfigureAwait(false);

        public async Task ExecuteSetTseTimeAsync() => await ExecuteHttpPostAsync("v1", "executesettsetime").ConfigureAwait(false);

        public async Task ExecuteSelfTestAsync() => await ExecuteHttpPostAsync("v1", "executeselftest").ConfigureAwait(false);

        public async Task<StartExportSessionResponse> StartExportSessionAsync(StartExportSessionRequest request) => await ExecuteHttpPostAsync<StartExportSessionResponse>("v1", "startexportsession", request).ConfigureAwait(false);

        public async Task<StartExportSessionResponse> StartExportSessionByTimeStampAsync(StartExportSessionByTimeStampRequest request) => await ExecuteHttpPostAsync<StartExportSessionResponse>("v1", "startexportsessionbytimestamp", request).ConfigureAwait(false);

        public async Task<StartExportSessionResponse> StartExportSessionByTransactionAsync(StartExportSessionByTransactionRequest request) => await ExecuteHttpPostAsync<StartExportSessionResponse>("v1", "startexportsessionbytransaction", request).ConfigureAwait(false);

        public async Task<ExportDataResponse> ExportDataAsync(ExportDataRequest request) => await ExecuteHttpPostAsync<ExportDataResponse>("v1", "exportdata", request).ConfigureAwait(false);

        public async Task<EndExportSessionResponse> EndExportSessionAsync(EndExportSessionRequest request) => await ExecuteHttpPostAsync<EndExportSessionResponse>("v1", "endexportsession", request).ConfigureAwait(false);

        public async Task<ScuDeEchoResponse> EchoAsync(ScuDeEchoRequest request) => await ExecuteHttpPostAsync<ScuDeEchoResponse>("v1", "echo", request).ConfigureAwait(false);

        private async Task<T> ExecuteHttpPostAsync<T>(string urlVersion, string urlMethod, object parameter = null)
        {
            var url = Path.Combine(urlVersion, urlMethod);
            StringContent stringContent = null;

            if (parameter != null)
            {
                var json = JsonConvert.SerializeObject(parameter);
                stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            }
            var response = await _httpClient.PostAsync(url, stringContent).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(result);
        }

        private async Task ExecuteHttpPostAsync(string urlVersion, string urlMethod, object parameter = null)
        {
            var url = Path.Combine(urlVersion, urlMethod);
            StringContent stringContent = null;

            if (parameter != null)
            {
                var json = JsonConvert.SerializeObject(parameter);
                stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            }
            var response = await _httpClient.PostAsync(url, stringContent).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        private async Task<T> ExecuteHttpGetAsync<T>(string urlVersion, string urlMethod)
        {
            var url = Path.Combine(urlVersion, urlMethod);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(result);
        }

        private HttpClient GetClient(HttpDESSCDClientOptions options)
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
    }
}