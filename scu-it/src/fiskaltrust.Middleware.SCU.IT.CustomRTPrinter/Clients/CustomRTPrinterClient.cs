using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Responses;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Clients
{
    public class CustomRTPrinterClient
    {
        // Shared HttpClient per DeviceUrl + serialized access per printer (single-threaded device).
        private static readonly ConcurrentDictionary<string, HttpClient> _sharedClients = new();
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new();

        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _gate;
        private readonly string _commandUrl;
        private readonly ILogger<CustomRTPrinterClient> _logger;

        public CustomRTPrinterClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _gate = new SemaphoreSlim(1, 1);
        }

        public CustomRTPrinterClient(CustomRTPrinterConfiguration configuration, ILogger<CustomRTPrinterClient> logger)
        {
            if (string.IsNullOrEmpty(configuration.DeviceUrl))
            {
                throw new NullReferenceException("ServerUrl is not set.");
            }

            var key = configuration.DeviceUrl.TrimEnd('/');
            _httpClient = _sharedClients.GetOrAdd(key, _ => new HttpClient
            {
                BaseAddress = new Uri(configuration.DeviceUrl),
                Timeout = TimeSpan.FromMilliseconds(configuration.ClientTimeoutMs),
            });
            _gate = _gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            _commandUrl = $"xml/printer.htm";
            _logger = logger;

            if (!string.IsNullOrEmpty(configuration.Username) || !string.IsNullOrEmpty(configuration.Password))
                SetBasicAuth(configuration.Username, configuration.Password);
        }

        public void SetBasicAuth(string username, string password)
        {
            var authenticationString = $"{username}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(authenticationString));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
        }

        internal static string Serialize<T>(T obj)
        {
            var serializer = new XmlSerializer(typeof(T));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            using var stringWriter = new StringWriter();
            using var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings
            {
                OmitXmlDeclaration = true
            });

            serializer.Serialize(writer, obj, namespaces);

            var tmp = stringWriter.ToString();
            tmp = tmp.Insert(0, "<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            return tmp;
        }

        public static T Deserialize<T>(string xml)
        {
            using (var reader = new StringReader(xml))
            {
                return (T) (new XmlSerializer(typeof(T)).Deserialize(reader) ?? throw new NullReferenceException("Deserialization failed."));
            }
        }

        public async Task<TRes> SendAsync<TReq, TRes>(TReq request)
            where TRes : IResponse
        {
            var xml = Serialize(request);

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                const int maxAttempts = 3;
                Exception lastError = null;
                for (var attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        _logger?.LogDebug("CustomRT → printer (attempt {Attempt}/{Max}):\n{Xml}", attempt, maxAttempts, xml);
                        var response = await _httpClient.PostAsync(_commandUrl, new StringContent(xml, System.Text.Encoding.UTF8, MediaTypeNames.Text.Plain), CancellationToken.None).ConfigureAwait(false);
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                            throw new CustomRTPrinterException("Unauthorized access to the printer. Please check the printer configuration.");
                        var responseXml = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        _logger?.LogDebug("CustomRT ← printer:\n{Xml}", responseXml);
                        return Deserialize<TRes>(responseXml);
                    }
                    catch (HttpRequestException ex)
                    {
                        lastError = ex;
                        _logger?.LogWarning("CustomRT transport error on attempt {Attempt}: {Msg}. Retrying...", attempt, ex.Message);
                        if (attempt < maxAttempts)
                            await Task.Delay(1000 * attempt).ConfigureAwait(false);
                    }
                    catch (IOException ex)
                    {
                        lastError = ex;
                        _logger?.LogWarning("CustomRT IO error on attempt {Attempt}: {Msg}. Retrying...", attempt, ex.Message);
                        if (attempt < maxAttempts)
                            await Task.Delay(1000 * attempt).ConfigureAwait(false);
                    }
                }
                throw new CustomRTPrinterException($"CustomRT printer unreachable after {maxAttempts} attempts: {lastError?.Message}", lastError);
            }
            finally
            {
                _gate.Release();
            }
        }

        public Task<TRes> SendFiscalReceipt<TRes>(IFiscalRecord[] records)
            where TRes : IResponse
            => SendAsync<PrinterFiscalReceipt, TRes>(new PrinterFiscalReceipt(records));

        public Task<TRes> SendFiscalReport<TRes>(IReport report)
            where TRes : IResponse
            => SendAsync<PrinterFiscalReport, TRes>(new PrinterFiscalReport(report));

        public Task<TRes> SendNonFiscal<TRes>(INonFiscalRecord[] records)
            where TRes : IResponse
            => SendAsync<PrinterNonFiscal, TRes>(new PrinterNonFiscal(records));

        public Task<TRes> SendCommand<TRes>(ICommand command)
            where TRes : IResponse
            => SendAsync<PrinterCommand, TRes>(new PrinterCommand(command));

        public Task<TRes> CancelFiscalReceipt<TRes>()
            where TRes : IResponse
            => SendAsync<PrinterFiscalReceiptCancel, TRes>(new PrinterFiscalReceiptCancel());

        public async Task<string> SendRawAsync(string xmlBody)
        {
            var response = await _httpClient.PostAsync(_commandUrl, new StringContent(xmlBody, System.Text.Encoding.UTF8, MediaTypeNames.Text.Plain), CancellationToken.None);
            return await response.Content.ReadAsStringAsync();
        }
    }
}