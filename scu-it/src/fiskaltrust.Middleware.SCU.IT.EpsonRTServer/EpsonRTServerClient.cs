using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.IT.EpsonRTServer.Models;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer
{
    public class EpsonRTServerClient : IEpsonRTServerClient
    {
        private const string FpServerEndpoint = "cgi-bin/fpserver.cgi";
        private const string FpMateEndpoint = "cgi-bin/fpmate.cgi";

        private readonly HttpClient _httpClient;
        private readonly EpsonRTServerConfiguration _configuration;
        private readonly ILogger<EpsonRTServerClient> _logger;
        private readonly string _authHeader;

        public EpsonRTServerClient(EpsonRTServerConfiguration configuration, ILogger<EpsonRTServerClient> logger)
        {
            if (string.IsNullOrEmpty(configuration.ServerUrl))
            {
                throw new NullReferenceException("EpsonRTServerConfiguration ServerUrl is not set.");
            }

            _configuration = configuration;
            _logger = logger;

            if (configuration.DisableSSLValidation)
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                _httpClient = new HttpClient(handler);
            }
            else
            {
                _httpClient = new HttpClient();
            }

            _httpClient.BaseAddress = new Uri(configuration.ServerUrl!.EndsWith("/") ? configuration.ServerUrl : configuration.ServerUrl + "/");
            _httpClient.Timeout = TimeSpan.FromMilliseconds(configuration.RTServerHttpTimeoutInMs);

            _authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{configuration.Username}:{configuration.Password}"));
        }

        public Task<RtServerResponse> CreateTokenAsync(string tillId)
            => SendToFpServerAsync($"<createToken><till tillId=\"{tillId}\" /></createToken>");

        public Task<RtServerResponse> CreateTillsAsync(string userId, string password, IEnumerable<string> tillIds)
        {
            var tills = tillIds.ToList();
            var body = new StringBuilder();
            body.Append("<createTills>");
            body.Append($"<user userId=\"{userId}\" password=\"{password}\" />");
            foreach (var tillId in tills)
            {
                body.Append($"<change add=\"{tillId}\" />");
            }
            body.Append("<tills>");
            foreach (var tillId in tills)
            {
                body.Append($"<till tillId=\"{tillId}\" zRepNumber=\"0001\" />");
            }
            body.Append("</tills>");
            body.Append("</createTills>");
            return SendToFpServerAsync(body.ToString());
        }

        public Task<RtServerResponse> CreateReceiptAsync(string createReceiptXml)
            => SendToFpServerAsync(createReceiptXml);

        public Task<RtServerResponse> CreateDailyClosureAsync(string tillId, int closureType)
            => SendToFpServerAsync($"<createDailyClosure><till tillId=\"{tillId}\" closureType=\"{closureType}\" /></createDailyClosure>");

        public Task<RtServerResponse> GetServerInfoAsync()
            => SendToFpServerAsync("<createReport><serverInfo /></createReport>");

        public Task<RtServerResponse> GetServerTimeAsync()
            => SendToFpServerAsync("<createReport><serverTime /></createReport>");

        public Task<RtServerResponse> GetFirmwareVersionAsync()
            => SendToFpServerAsync("<createReport><firmwareVersion /></createReport>");

        public Task<RtServerResponse> GetFiscalInformationAsync(string tillId)
            => SendToFpServerAsync($"<createReport><fiscalInformation tillId=\"{tillId}\" /></createReport>");

        public Task<RtServerResponse> GetPublicKeyAsync()
            => SendToFpServerAsync("<createReport><publicKey /></createReport>");

        public Task<RtServerResponse> PrintServerZReportAsync()
            => SendToFpMateAsync("<printerFiscalReport><printZReport /></printerFiscalReport>");

        public Task<RtServerResponse> RebootWebServerAsync()
            => SendToFpMateAsync("<printerCommand><rebootWebServer /></printerCommand>");

        private Task<RtServerResponse> SendToFpServerAsync(string bodyXml) => PerformAsync(FpServerEndpoint, bodyXml);

        private Task<RtServerResponse> SendToFpMateAsync(string bodyXml)
            => PerformAsync($"{FpMateEndpoint}?timeout={_configuration.ServerCommandTimeoutInMs}", bodyXml);

        private async Task<RtServerResponse> PerformAsync(string endpoint, string bodyXml)
        {
            var payload = WrapInSoapEnvelope(bodyXml);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/xml")
            };
            requestMessage.Headers.Add("Authorization", $"Basic {_authHeader}");

            var result = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!result.IsSuccessStatusCode)
            {
                throw new EpsonRTServerCommunicationException($"The RT Server returned HTTP {(int) result.StatusCode} ({result.ReasonPhrase}) for '{endpoint}'. Content: {content}", -1);
            }

            var response = RtServerResponse.Parse(content);
            if (!response.Success && response.CodeAsInt < 0)
            {
                _logger.LogError("Calling '{endpoint}' failed with code {code} ({status}). Raw: {raw}", endpoint, response.Code, response.Status, content);
                if (!_configuration.IgnoreRTServerErrors)
                {
                    throw new EpsonRTServerCommunicationException($"Calling '{endpoint}' failed with code {response.Code} ({response.Status}).", response.CodeAsInt);
                }
            }
            return response;
        }

        private static string WrapInSoapEnvelope(string bodyXml) => $"""
<?xml version="1.0" encoding="utf-8"?>
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
<soapenv:Body>
{bodyXml}
</soapenv:Body>
</soapenv:Envelope>
""";
    }
}
