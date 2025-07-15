using System.Diagnostics;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using fiskaltrust.Middleware.SCU.ES.VeriFactuModels;
using fiskaltrust.Middleware.SCU.ES.VeriFactu;
using fiskaltrust.Middleware.SCU.ES.VeriFactu.Helpers;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactuSoap;

public interface IClient
{
    public Task<Result<RespuestaRegFactuSistemaFacturacion, Error>> SendAsync(Envelope<RequestBody> envelope);
}

public class Client : IClient
{
    private HttpClient _httpClient { get; }

    public Client(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<RespuestaRegFactuSistemaFacturacion, Error>> SendAsync(Envelope<RequestBody> envelope)
    {
        var requestString = envelope.XmlSerialize();
        var response = await _httpClient.PostAsync("", new StringContent(requestString, Encoding.UTF8, "application/soap+xml"));


        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentType?.MediaType == "text/html")
        {
            return new Error.Http(response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        var contentSteam = await response.Content.ReadAsStreamAsync();
        Envelope<ResponseBody> content;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var serializer = new XmlSerializer(typeof(Envelope<ResponseBody>));
        try
        {
            content = (Envelope<ResponseBody>)serializer.Deserialize(contentSteam)!;
        }
        catch (Exception ex)
        {
            if (contentSteam.CanSeek)
            {
                contentSteam.Seek(0, SeekOrigin.Begin);
            }

            using var reader = new StreamReader(contentSteam);
            return new Error.Xml(ex, await reader.ReadToEndAsync());
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