using fiskaltrust.ifPOS.v1.at;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Http
{
    internal class HttpATSSCD : IATSSCD
    {
        private readonly HttpClient _httpClient;

        private delegate string AsyncEchoCaller(string message);
        private delegate byte[] AsyncCertificateCaller();
        private delegate string AsyncZdaCaller();
        private delegate byte[] AsyncSignCaller(byte[] data);

        public HttpATSSCD(HttpATSSCDClientOptions options)
        {
            _httpClient = GetClient(options);
        }

        public async Task<CertificateResponse> CertificateAsync() => await ExecuteHttpGetAsync<CertificateResponse>("v1", "Certificate").ConfigureAwait(false);

        public async Task<ZdaResponse> ZdaAsync() => await ExecuteHttpGetAsync<ZdaResponse>("v1", "Zda").ConfigureAwait(false);

        public async Task<SignResponse> SignAsync(SignRequest signRequest) => await ExecuteHttpPostAsync<SignResponse>("v1", "Sign", signRequest).ConfigureAwait(false);

        public async Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => await ExecuteHttpPostAsync<EchoResponse>("v1", "Echo", echoRequest).ConfigureAwait(false);

        public byte[] Certificate() => Task.Run(async () => await ExecuteHttpGetAsync<byte[]>("v0", "Certificate").ConfigureAwait(false)).Result;

        public IAsyncResult BeginCertificate(AsyncCallback callback, object state)
        {
            var d = new AsyncCertificateCaller((this as ifPOS.v0.IATSSCD).Certificate);
            return d.BeginInvoke(callback, d);
        }

        public byte[] EndCertificate(IAsyncResult result)
        {
            var d = (AsyncCertificateCaller)result.AsyncState;
            return d.EndInvoke(result);
        }

        public string ZDA() => Task.Run(async () => await ExecuteHttpGetAsync<string>("v0", "ZDA").ConfigureAwait(false)).Result;

        public IAsyncResult BeginZDA(AsyncCallback callback, object state)
        {
            var d = new AsyncZdaCaller((this as ifPOS.v0.IATSSCD).ZDA);
            return d.BeginInvoke(callback, d);
        }

        public string EndZDA(IAsyncResult result)
        {
            var d = (AsyncZdaCaller)result.AsyncState;
            return d.EndInvoke(result);
        }

        public byte[] Sign(byte[] data) => Task.Run(async () => await ExecuteHttpPostAsync<byte[]>("v0", "Sign", data).ConfigureAwait(false)).Result;

        public IAsyncResult BeginSign(byte[] data, AsyncCallback callback, object state)
        {
            var d = new AsyncSignCaller((this as ifPOS.v0.IATSSCD).Sign);
            return d.BeginInvoke(data, callback, d);
        }

        public byte[] EndSign(IAsyncResult result)
        {
            var d = (AsyncSignCaller)result.AsyncState;
            return d.EndInvoke(result);
        }

        public string Echo(string message) => Task.Run(async () => await ExecuteHttpPostAsync<string>("v0", "Echo", message).ConfigureAwait(false)).Result;

        public IAsyncResult BeginEcho(string message, AsyncCallback callback, object state)
        {
            var d = new AsyncEchoCaller((this as ifPOS.v0.IATSSCD).Echo);
            return d.BeginInvoke(message, callback, d);
        }

        public string EndEcho(IAsyncResult result)
        {
            var d = (AsyncEchoCaller)result.AsyncState;
            return d.EndInvoke(result);
        }

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

        private HttpClient GetClient(HttpATSSCDClientOptions options)
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