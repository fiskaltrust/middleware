using System;
using System.IO;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.Soap;

public class Client
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

    public async Task<string> SendAsync(Envelope envelope)
    {
        var response = await _httpClient.PostAsync("/wlpl/TIKE-CONT/ws/SistemaFacturacion/VerifactuSOAP", new StringContent(envelope.Serialize(), Encoding.UTF8, "application/soap+xml"));

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(content);
        }

        return content;
    }
}