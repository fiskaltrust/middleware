using System.Diagnostics;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using fiskaltrust.Middleware.SCU.ES.Models;
using fiskaltrust.Middleware.SCU.ES.VeriFactu.Helpers;
using fiskaltrust.Middleware.SCU.ES.VeriFactu.Mapping;

namespace fiskaltrust.Middleware.SCU.ES.Soap;



public interface IClient
{
    public Task<Result<RespuestaRegFactuSistemaFacturacion, Error>> SendAsync(Envelope<RequestBody> envelope);
}

public class Client : IClient
{
    private HttpClient _httpClient { get; }

    public Client(Uri uri, X509Certificate2 certificate)
    {
        var requestHandler = new HttpClientHandler();
        requestHandler.ClientCertificates.Add(certificate);
        _httpClient = new HttpClient(requestHandler)
        {
            BaseAddress = uri,
        };
        _httpClient.DefaultRequestHeaders.Add("AcceptCharset", "utf-8");
    }

    public async Task<Result<RespuestaRegFactuSistemaFacturacion, Error>> SendAsync(Envelope<RequestBody> envelope)
    {
        var requestString = envelope.XmlSerialize();
        var response = await _httpClient.PostAsync("/wlpl/TIKE-CONT/ws/SistemaFacturacion/VerifactuSOAP", new StringContent(requestString, Encoding.UTF8, "application/soap+xml"));

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var serializer = new XmlSerializer(typeof(Envelope<ResponseBody>));
        var contentStream = await response.Content.ReadAsStreamAsync();

        Envelope<ResponseBody> content;
        try
        {
            content = (Envelope<ResponseBody>) serializer.Deserialize(contentStream)!;
        }
        catch (Exception ex)
        {
            using var reader = new StreamReader(contentStream);
            return new Error.Xml(ex, await reader.ReadToEndAsync());
        }

        if (!response.IsSuccessStatusCode)
        {
            return new Error.Http(content.XmlSerialize());
        }

        if (content.Body.Content is Fault fault)
        {
            return new Error.Soap($"{fault.FaultCode}{(fault.Detail.ErrorCode.HasValue ? $"({fault.Detail.ErrorCode.Value})" : "")}: {fault.FaultString}");
        }

        if (content.Body.Content is RespuestaRegFactuSistemaFacturacion repusta)
        {
            return repusta;
        }
        throw new UnreachableException();
    }
}