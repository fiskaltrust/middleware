using System;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

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

            return stringWriter.ToString();
        }

        internal static T Deserialize<T>(Stream xml)
        {
            return (T) (new XmlSerializer(typeof(T)).Deserialize(xml) ?? throw new NullReferenceException("Deserialization failed."));
        }

        public async Task<TRes> PostAsync<TReq, TRes>()
            where TReq : Models.Requests.IRequest, new()
            where TRes : Models.Responses.IResponse
        {
            return await PostAsync<TReq, TRes>(new TReq());
        }

        public async Task<TRes> PostAsync<TReq, TRes>(TReq request)
            where TReq : Models.Requests.IRequest
            where TRes : Models.Responses.IResponse
        {
            var response = await _httpClient.PostAsync("/", new StringContent(Serialize(request), System.Text.Encoding.UTF8, MediaTypeNames.Text.Plain), CancellationToken.None);
            return Deserialize<TRes>(await response.Content.ReadAsStreamAsync());
        }
    }
}