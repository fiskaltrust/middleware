using System;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Responses;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Clients
{
    public class CustomRTPrinterClient
    {
        private readonly HttpClient _httpClient;

        public CustomRTPrinterClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public CustomRTPrinterClient(CustomRTPrinterConfiguration configuration)
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

            return tmp;
        }

        internal static T Deserialize<T>(Stream xml)
        {
            return (T) (new XmlSerializer(typeof(T)).Deserialize(xml) ?? throw new NullReferenceException("Deserialization failed."));
        }

        public async Task<TRes> SendAsync<TReq, TRes>(TReq request)
            where TRes : IResponse
        {
            var response = await _httpClient.PostAsync("/", new StringContent(Serialize(request), System.Text.Encoding.UTF8, MediaTypeNames.Text.Plain), CancellationToken.None);
            return Deserialize<TRes>(await response.Content.ReadAsStreamAsync());
        }

        public Task<TRes> SendFiscalReceipt<TRes>(IFiscalRecord[] records)
            where TRes : IResponse
            => SendAsync<PrinterFiscalReceipt, TRes>(new PrinterFiscalReceipt(records));

        public Task<TRes> SendFiscalReport<TRes>(IReport report)
            where TRes : IResponse
            => SendAsync<PrinterFiscalReport, TRes>(new PrinterFiscalReport(report));

        public Task<TRes> SendCommand<TRes>(ICommand command)
            where TRes : IResponse
            => SendAsync<PrinterCommand, TRes>(new PrinterCommand(command));
    }
}