using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Interface.Client.Http.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace fiskaltrust.Middleware.Interface.Client.Http
{
    internal sealed class HttpPos : IPOS, IDisposable
    {
        private readonly HttpPosClientOptions _options;
        private readonly HttpClient _httpClient;
        private readonly string _v0VersionUrl;

        private delegate string AsyncEchoCaller(string message);
        private delegate ifPOS.v0.ReceiptResponse AsyncSignCaller(ifPOS.v0.ReceiptRequest request);
        private delegate Stream AsyncJournalCaller(long ftJournalType, long from, long to);


        public HttpPos(HttpPosClientOptions options)
        {
            _httpClient = GetClient(options);
            _options = options;
            _v0VersionUrl = _options.UseUnversionedLegacyUrls ? "" : "v0/";

        }

        private HttpClient GetClient(HttpPosClientOptions options)
        {
            var url = options.Url.ToString().EndsWith("/") ? options.Url : new Uri($"{options.Url}/");

            HttpClient client;

            if (options.DisableSslValidation.HasValue && options.DisableSslValidation.Value)
            {
                var handler = new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true
                };

                client = new HttpClient(handler) { BaseAddress = url };
            }
            else
            {
                client = new HttpClient { BaseAddress = url };
            }


            if (options.CashboxId.HasValue)
                client.DefaultRequestHeaders.Add("cashboxid", options.CashboxId.Value.ToString());

            if (!string.IsNullOrEmpty(options.AccessToken))
                client.DefaultRequestHeaders.Add("accesstoken", options.AccessToken);

            return client;
        }

        IAsyncResult ifPOS.v0.IPOS.BeginEcho(string message, AsyncCallback callback, object state)
        {
            var d = new AsyncEchoCaller((this as ifPOS.v0.IPOS).Echo);
            return d.BeginInvoke(message, callback, d);
        }

        string ifPOS.v0.IPOS.EndEcho(IAsyncResult result)
        {
            var d = (AsyncEchoCaller)result.AsyncState;
            return d.EndInvoke(result);
        }

        string ifPOS.v0.IPOS.Echo(string message)
        {
            if (_options.CommunicationType == HttpCommunicationType.Json)
            {
                return JsonEcho(message);
            }
            else
            {
                return XmlEcho(message);
            }
        }

        string JsonEcho(string message)
        {
            var jsonstring = JsonConvert.SerializeObject(message);
            var jsonContent = new StringContent(jsonstring, Encoding.UTF8, "application/json");

            using (var response = _httpClient.PostAsync($"{_v0VersionUrl}json/echo", jsonContent).Result)
            {
                response.EnsureSuccessStatusCode();
                var content = response.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<string>(content);
            }
        }

        string XmlEcho(string message)
        {
            var xmlString = XmlSerializationHelpers.Serialize(message);
            var xmlContent = new StringContent(xmlString, Encoding.UTF8, "application/xml");

            using (var response = _httpClient.PostAsync($"xml/{_v0VersionUrl}echo", xmlContent).Result)
            {
                response.EnsureSuccessStatusCode();
                var content = response.Content.ReadAsStringAsync().Result;

                return content;
            }
        }

        async Task<EchoResponse> IPOS.EchoAsync(EchoRequest message)
        {
            if (_options.CommunicationType == HttpCommunicationType.Json)
            {
                return await JsonEchoAsync(message);
            }
            else
            {
                return await XmlEchoAsync(message);
            }
        }

        private async Task<EchoResponse> XmlEchoAsync(EchoRequest message)
        {
            var xmlString = XmlSerializationHelpers.Serialize(message);
            var xmlContent = new StringContent(xmlString, Encoding.UTF8, "application/xml");

            using (var response = await _httpClient.PostAsync("v1/Echo", xmlContent))
            {
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                var xml = XElement.Parse(content);
                string jsonText = JsonConvert.SerializeXNode(xml);
                return JsonConvert.DeserializeObject<EchoResponse>(jsonText);
            }
        }

        private async Task<EchoResponse> JsonEchoAsync(EchoRequest message)
        {
            var jsonstring = JsonConvert.SerializeObject(message);
            var jsonContent = new StringContent(jsonstring, Encoding.UTF8, "application/json");

            using (var response = await _httpClient.PostAsync("v1/Echo", jsonContent))
            {
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<EchoResponse>(content.ToString());
            }
        }

        IAsyncResult ifPOS.v0.IPOS.BeginSign(ifPOS.v0.ReceiptRequest data, AsyncCallback callback, object state)
        {
            var d = new AsyncSignCaller((this as ifPOS.v0.IPOS).Sign);
            return d.BeginInvoke(data, callback, d);
        }

        ifPOS.v0.ReceiptResponse ifPOS.v0.IPOS.EndSign(IAsyncResult result)
        {
            var d = (AsyncSignCaller)result.AsyncState;
            return d.EndInvoke(result);
        }

        ifPOS.v0.ReceiptResponse ifPOS.v0.IPOS.Sign(ifPOS.v0.ReceiptRequest data)
        {
            if (_options.CommunicationType == HttpCommunicationType.Json)
            {
                return JsonSignAsync<ifPOS.v0.ReceiptRequest, ifPOS.v0.ReceiptResponse>(data, $"json/{_v0VersionUrl}sign").Result;
            }
            else
            {
                return XmlSignAsync<ifPOS.v0.ReceiptRequest, ifPOS.v0.ReceiptResponse>(data, $"xml/{_v0VersionUrl}sign").Result;
            }
        }

        async Task<ReceiptResponse> IPOS.SignAsync(ReceiptRequest request)
        {
            if (_options.CommunicationType == HttpCommunicationType.Json)
            {
                return await JsonSignAsync<ReceiptRequest, ReceiptResponse>(request, "v1/sign");
            }
            else
            {
                return await XmlSignAsync<ReceiptRequest, ReceiptResponse>(request, "v1/sign");
            }
        }

        private async Task<TResponse> JsonSignAsync<TRequest, TResponse>(TRequest request, string endpoint)
        {
            var jsonstring = JsonConvert.SerializeObject(request);
            var jsonContent = new StringContent(jsonstring, Encoding.UTF8, "application/json");

            using (var response = await _httpClient.PostAsync(endpoint, jsonContent))
            {
                var content = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();

                return JsonConvert.DeserializeObject<TResponse>(content);
            }
        }

        private async Task<TResponse> XmlSignAsync<TRequest, TResponse>(TRequest request, string endpoint)
        {
            var xmlString = XmlSerializationHelpers.Serialize(request);
            var xmlContent = new StringContent(xmlString, Encoding.UTF8, "application/xml");

            using (var response = await _httpClient.PostAsync(endpoint, xmlContent))
            {
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var xml = XElement.Parse(content);
                string jsonText = JsonConvert.SerializeXNode(xml);

                return JsonConvert.DeserializeObject<TResponse>(jsonText);
            }
        }


        IAsyncResult ifPOS.v0.IPOS.BeginJournal(long ftJournalType, long from, long to, AsyncCallback callback, object state)
        {
            var d = new AsyncJournalCaller((this as ifPOS.v0.IPOS).Journal);
            return d.BeginInvoke(ftJournalType, from, to, callback, d);
        }

        Stream ifPOS.v0.IPOS.EndJournal(IAsyncResult result)
        {
            var d = (AsyncJournalCaller)result.AsyncState;
            return d.EndInvoke(result);
        }

        Stream ifPOS.v0.IPOS.Journal(long ftJournalType, long from, long to)
        {
            using (var client = GetClient(_options))
            {
                string format;
                if (_options.CommunicationType == HttpCommunicationType.Json)
                {
                    format = "json";
                }
                else
                {
                    format = "xml";
                }

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue($"application/{format}"));

                var response = client.PostAsync($"{format}/{_v0VersionUrl}journal?type={ftJournalType}&from={from}&to={to}", new StringContent("", Encoding.UTF8, $"application/{format}")).Result;
                response.EnsureSuccessStatusCode();
                var stream = response.Content.ReadAsStreamAsync().Result;
                return stream;
            }
        }

        IAsyncEnumerable<JournalResponse> IPOS.JournalAsync(JournalRequest request)
        {
            throw new NotSupportedException("Async streaming is not supported in HTTP. Please call the non-async Journal method.");
        }

        public void Dispose() => _httpClient?.Dispose();
    }
}
