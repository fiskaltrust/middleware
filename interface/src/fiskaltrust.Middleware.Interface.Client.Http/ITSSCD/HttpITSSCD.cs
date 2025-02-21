using fiskaltrust.ifPOS.v1.it;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Http
{
    internal class HttpITSSCD : IITSSCD
    {
        private readonly HttpClient _httpClient;

        public HttpITSSCD(HttpITSSCDClientOptions options)
        {
            _httpClient = GetClient(options);
        }

        public async Task<DeviceInfo> GetDeviceInfoAsync() => await ExecuteHttpGetAsync<DeviceInfo>("v1", "GetDeviceInfo").ConfigureAwait(false);

        public async Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => await ExecuteHttpPostAsync<ScuItEchoResponse>("v1", "Echo", request).ConfigureAwait(false);

        public async Task<FiscalReceiptResponse> FiscalReceiptInvoiceAsync(FiscalReceiptInvoice request) => await ExecuteHttpPostAsync<FiscalReceiptResponse>("v1", "FiscalReceiptInvoice", request).ConfigureAwait(false);

        public async Task<FiscalReceiptResponse> FiscalReceiptRefundAsync(FiscalReceiptRefund request) => await ExecuteHttpPostAsync<FiscalReceiptResponse>("v1", "FiscalReceiptRefund", request).ConfigureAwait(false);

        public async Task<DailyClosingResponse> ExecuteDailyClosingAsync(DailyClosingRequest request) => await ExecuteHttpPostAsync<DailyClosingResponse>("v1", "ExecuteDailyClosing", request).ConfigureAwait(false);

        public async Task<Response> NonFiscalReceiptAsync(NonFiscalRequest request) => await ExecuteHttpPostAsync<Response>("v1", "NonFiscalReceipt", request).ConfigureAwait(false);

        public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)=> await ExecuteHttpPostAsync<ProcessResponse>("v1", "ProcessReceipt", request).ConfigureAwait(false);
       
        public async Task<RTInfo> GetRTInfoAsync()=> await ExecuteHttpGetAsync<RTInfo>("v1", "GetRTInfo").ConfigureAwait(false);

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

        private async Task<T> ExecuteHttpGetAsync<T>(string urlVersion, string urlMethod)
        {
            var url = Path.Combine(urlVersion, urlMethod);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(result);
        }

        private HttpClient GetClient(HttpITSSCDClientOptions options)
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