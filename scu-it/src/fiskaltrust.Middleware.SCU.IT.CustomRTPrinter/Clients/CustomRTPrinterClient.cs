using System;
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
        private readonly HttpClient _httpClient;
        private readonly string _commandUrl;
        private readonly ILogger<CustomRTPrinterClient> _logger;

        public CustomRTPrinterClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public CustomRTPrinterClient(CustomRTPrinterConfiguration configuration, ILogger<CustomRTPrinterClient> logger)
        {
            if (string.IsNullOrEmpty(configuration.DeviceUrl))
            {
                throw new NullReferenceException("ServerUrl is not set.");
            }

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(configuration.DeviceUrl),
                Timeout = TimeSpan.FromMilliseconds(configuration.ClientTimeoutMs),
            };

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
            _logger?.LogDebug("CustomRT → printer:\n{Xml}", xml);
            var response = await _httpClient.PostAsync(_commandUrl, new StringContent(xml, System.Text.Encoding.UTF8, MediaTypeNames.Text.Plain), CancellationToken.None);
            if(response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new CustomRTPrinterException("Unauthorized access to the printer. Please check the printer configuration.");
            }
            var responseXml = await response.Content.ReadAsStringAsync();
            _logger?.LogDebug("CustomRT ← printer:\n{Xml}", responseXml);
            return Deserialize<TRes>(responseXml);
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