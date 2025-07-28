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
using fiskaltrust.Middleware.SCU.ES.Models;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactuSoap;

public interface IClient
{
    public Task<(Result<RespuestaRegFactuSistemaFacturacion, Error> result, GovernmentAPI governmentAPI)> SendAsync(Envelope<RequestBody> envelope);
}

public class Client : IClient
{
    private HttpClient _httpClient { get; }

    public Client(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(Result<RespuestaRegFactuSistemaFacturacion, Error> result, GovernmentAPI governmentAPI)> SendAsync(Envelope<RequestBody> envelope)
    {
        var requestString = envelope.XmlSerialize();
        var response = await _httpClient.PostAsync("", new StringContent(requestString, Encoding.UTF8, "application/soap+xml"));

        var governmentAPI = new GovernmentAPI
        {
            Request = requestString,
            Response = await response.Content.ReadAsStringAsync()
        };

        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentType?.MediaType == "text/html")
        {
            return (new Error.Http(response.StatusCode, governmentAPI.Response), governmentAPI);
        }

        Envelope<ResponseBody> content;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var serializer = new XmlSerializer(typeof(Envelope<ResponseBody>));
        try
        {
            using var reader = new StringReader(governmentAPI.Response);
            content = (Envelope<ResponseBody>) serializer.Deserialize(reader)!;
        }
        catch (Exception ex)
        {
            return (new Error.Xml(ex, governmentAPI.Response), governmentAPI);
        }

        if (content.Body.Content is Fault fault)
        {
            return (new Error.Soap($"{fault.FaultCode}{(fault.Detail.ErrorCode.HasValue ? $"({fault.Detail.ErrorCode.Value})" : "")}: {fault.FaultString}"), governmentAPI);
        }

        if (content.Body.Content is RespuestaRegFactuSistemaFacturacion repusta)
        {
            return (repusta, governmentAPI);
        }
        throw new UnreachableException();
    }
}