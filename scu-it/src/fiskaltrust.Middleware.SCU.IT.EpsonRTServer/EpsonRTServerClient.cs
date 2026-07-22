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

        public Task<RtServerResponse> CreateTillsAsync(string userId, string password, IEnumerable<string> tillMap, IEnumerable<string> tillsToAdd)
        {
            var body = new StringBuilder();
            body.Append("<createTills>");
            body.Append($"<user userId=\"{userId}\" password=\"{password}\" />");
            foreach (var tillId in tillsToAdd)
            {
                body.Append($"<change add=\"{tillId}\" />");
            }
            body.Append("<tills>");
            foreach (var tillId in tillMap)
            {
                // zRepNumber is intentionally omitted: it is only meant for SD-card substitution and a value
                // lower than the till's current Z number makes the RT Server reject the whole map.
                body.Append($"<till tillId=\"{tillId}\" />");
            }
            body.Append("</tills>");
            body.Append("</createTills>");
            return SendToFpServerAsync(body.ToString());
        }

        public Task<RtServerResponse> GetTillMapAsync()
            => SendToFpServerAsync("<createReport><tillMap /></createReport>");

        public Task<RtServerResponse> CreateReceiptAsync(string createReceiptXml)
            => PerformAsync(FpServerEndpoint, createReceiptXml, acceptReceiptWarnings: true);

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

        private async Task<RtServerResponse> PerformAsync(string endpoint, string bodyXml, bool acceptReceiptWarnings = false)
        {
            var response = await PerformOnceAsync(endpoint, bodyXml).ConfigureAwait(false);
            // -8 "Server busy" is transient (e.g. while a daily closure or Z report is being processed):
            // retry with a delay instead of failing the operation.
            for (var attempt = 0; response.CodeAsInt == -8 && attempt < _configuration.ServerBusyRetries; attempt++)
            {
                _logger.LogWarning("RT Server is busy ({status}); retrying '{endpoint}' in {delay} ms (attempt {attempt}/{max}).",
                    response.Status, endpoint, _configuration.ServerBusyRetryDelayInMs, attempt + 1, _configuration.ServerBusyRetries);
                await Task.Delay(_configuration.ServerBusyRetryDelayInMs).ConfigureAwait(false);
                response = await PerformOnceAsync(endpoint, bodyXml).ConfigureAwait(false);
            }

            if (!response.Success && response.CodeAsInt < 0)
            {
                var description = EpsonRTServerErrorCodes.Describe(response.CodeAsInt);
                if (acceptReceiptWarnings && EpsonRTServerErrorCodes.IsReceiptAcceptedWithWarning(response.CodeAsInt))
                {
                    _logger.LogWarning("Calling '{endpoint}' was accepted with warning code {code} ({status}): {description}. Raw: {raw}", endpoint, response.Code, response.Status, description, response.RawResponse);
                }
                else
                {
                    _logger.LogError("Calling '{endpoint}' failed with code {code} ({status}): {description}. Raw: {raw}", endpoint, response.Code, response.Status, description, response.RawResponse);
                    if (!_configuration.IgnoreRTServerErrors)
                    {
                        throw new EpsonRTServerCommunicationException($"Calling '{endpoint}' failed with code {response.Code} ({response.Status}): {description}", response.CodeAsInt);
                    }
                }
            }
            return response;
        }

        private async Task<RtServerResponse> PerformOnceAsync(string endpoint, string bodyXml)
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

            return RtServerResponse.Parse(content);
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
