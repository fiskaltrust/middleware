using System.Net.Http;
using System.Security.Cryptography;
using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.Middleware.SCU.AT.PrimeSignHSM.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.X509;

namespace fiskaltrust.Middleware.SCU.AT.PrimeSignHSM
{
    public class PrimeSignSCU : IATSSCD, IDisposable
    {
        private const string ZDA_NAME = "AT3";

        private readonly HttpClient _httpClient;
        private readonly PrimeSignSCUConfiguration _configuration;
        private readonly ILogger<PrimeSignSCU> _logger;

        private byte[]? _certificate;
        private string? _key;

        private delegate byte[] Sign_Delegate(byte[] data);
        private delegate byte[] Certificate_Delegate();
        private delegate string Echo_Delegate(string message);
        private delegate string Zda_Delegate();

        public PrimeSignSCU(PrimeSignSCUConfiguration configuration, ILogger<PrimeSignSCU> logger)
        {
            var uri = new Uri(configuration.Url.EndsWith("/") ? configuration.Url : configuration.Url + "/");

            if (!configuration.SslValidation)
            {
                var handler = new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true
                };
                _httpClient = new HttpClient(handler) { BaseAddress = uri };
            }
            else
            {
                _httpClient = new HttpClient { BaseAddress = uri };
            }
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<CertificateResponse> CertificateAsync()
        {
            if (string.IsNullOrWhiteSpace(_key))
            {
                _key = await GetDefaultKeyAsync().ConfigureAwait(false);
            }

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"rs/keys/{_key}/certificate.cer");
            requestMessage.Headers.Add("X-AUTH-TOKEN", _configuration.SharedSecret);
            var httpResponse = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            if (httpResponse.IsSuccessStatusCode)
            {
                using var responseContentStream = await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                var parser = new X509CertificateParser();
                var x509 = parser.ReadCertificate(responseContentStream);
                _certificate = x509.GetEncoded();
                return new CertificateResponse { Certificate = _certificate };
            }
            else
            {
                var resultContent = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                _certificate = null;
                throw new Exception($"An error occured when trying to read the default key from the service: Status code {httpResponse.StatusCode}, Response content: {resultContent}");
            }
        }

        public byte[] Certificate() => Task.Run(async () => await CertificateAsync()).Result.Certificate;

        public IAsyncResult BeginCertificate(AsyncCallback callback, object state)
        {
            var d = new Certificate_Delegate(Certificate);
            var r = d.BeginInvoke(callback, d);
            return r;
        }

        public byte[] EndCertificate(IAsyncResult result)
        {
            var d = (Certificate_Delegate) result.AsyncState;
            return d.EndInvoke(result);
        }

        public async Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => await Task.FromResult(new EchoResponse { Message = echoRequest.Message }).ConfigureAwait(false);

        public string Echo(string message) => message;

        public IAsyncResult BeginEcho(string message, AsyncCallback callback, object state)
        {
            var d = new Echo_Delegate(Echo);
            var r = d.BeginInvoke(message, callback, d);
            return r;
        }

        public string EndEcho(IAsyncResult result)
        {
            var d = (Echo_Delegate) result.AsyncState;
            return d.EndInvoke(result);
        }

        public async Task<SignResponse> SignAsync(SignRequest signRequest)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(signRequest.Data);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "rs/rk/signatures/r1raw");
            requestMessage.Headers.Add("X-AUTH-TOKEN", _configuration.SharedSecret);
            requestMessage.Headers.TryAddWithoutValidation("Content-Type", "application/json;charset=utf-8");
            requestMessage.Content = new ByteArrayContent(hash);
            var httpResponse = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            if (httpResponse.IsSuccessStatusCode)
            {
                var response = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var base64 = response.Replace('_', '/').Replace('-', '+');
                switch (base64.Length % 4)
                {
                    case 2:
                        base64 += "==";
                        break;
                    case 3:
                        base64 += "=";
                        break;
                }
                return new SignResponse { SignedData = Convert.FromBase64String(base64) };
            }
            else
            {
                var resultContent = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Exception($"An error occured when trying to sign data via the HSM: Status code {httpResponse.StatusCode}, Response content: {resultContent}");
            }
        }

        public byte[] Sign(byte[] data) => Task.Run(async () => await SignAsync(new SignRequest { Data = data })).Result.SignedData;

        public IAsyncResult BeginSign(byte[] data, AsyncCallback callback, object state)
        {
            var d = new Sign_Delegate(Sign);
            var r = d.BeginInvoke(data, callback, d);
            return r;
        }

        public byte[] EndSign(IAsyncResult result)
        {
            var d = (Sign_Delegate) result.AsyncState;
            return d.EndInvoke(result);
        }

        public async Task<ZdaResponse> ZdaAsync() => await Task.FromResult(new ZdaResponse { ZDA = ZDA_NAME }).ConfigureAwait(false);

        public string ZDA() => ZDA_NAME;

        public IAsyncResult BeginZDA(AsyncCallback callback, object state)
        {
            var d = new Zda_Delegate(ZDA);
            var r = d.BeginInvoke(callback, d);
            return r;
        }

        public string EndZDA(IAsyncResult result)
        {
            var d = (Zda_Delegate) result.AsyncState;
            return d.EndInvoke(result);
        }

        private async Task<string?> GetDefaultKeyAsync()
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "rs/rk/config");
            requestMessage.Headers.Add("X-AUTH-TOKEN", _configuration.SharedSecret);
            var httpResponse = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            var resultContent = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (httpResponse.IsSuccessStatusCode)
            {
                var config = JsonConvert.DeserializeObject<Config>(resultContent);
                return config.User.DefaultKey;
            }
            else
            {
                _logger.LogError("An error occured when trying to read the default key from the service: Status code {StatusCode}, Response content: {ResponseContent}", httpResponse.StatusCode, resultContent);
                return null;
            }
        }

        public void Dispose() => _httpClient?.Dispose();
    }
}