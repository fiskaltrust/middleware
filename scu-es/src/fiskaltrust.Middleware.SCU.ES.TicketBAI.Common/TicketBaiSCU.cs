using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Territories;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.es.Cases;
using fiskaltrust.ifPOS.v2.Cases;
using Microsoft.Xades;

#pragma warning disable IDE0052

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;

public class TicketBaiSCU : IESSSCD
{
    private readonly TicketBaiSCUConfiguration _configuration;

    private readonly HttpClient _httpClient;
    private readonly TicketBaiFactory _ticketBaiFactory;
    private readonly ILogger<TicketBaiSCU> _logger;
    private readonly ITicketBaiTerritory _ticketBaiTerritory;
    private readonly Uri _baseAddress;

    public TicketBaiSCU(ILogger<TicketBaiSCU> logger, TicketBaiSCUConfiguration configuration, ITicketBaiTerritory ticketBaiTerritory)
    {
        _logger = logger;
        _configuration = configuration;
        _ticketBaiTerritory = ticketBaiTerritory;
        _baseAddress = new Uri(_ticketBaiTerritory.SandboxEndpoint);

        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(_configuration.Certificate);
        handler.AutomaticDecompression = DecompressionMethods.GZip;


        _httpClient = new HttpClient(handler)
        {
            BaseAddress = _baseAddress
        };
        _ticketBaiFactory = new TicketBaiFactory(configuration);
    }

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        if (request.ReceiptResponse.ftStateData is null)
        {
            throw new Exception("ftStateData must be present.");
        }
        var middlewareStateData = MiddlewareStateData.FromReceiptResponse(request.ReceiptResponse);
        if (middlewareStateData is null || middlewareStateData.ES is null)
        {
            throw new Exception("ES state must be present in ftStateData.");
        }

        var endpoint = !request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void)
                ? _ticketBaiTerritory.SubmitInvoices
                : _ticketBaiTerritory.CancelInvoices;

        var ticketBaiRequest = _ticketBaiFactory.ConvertTo(request, middlewareStateData.ES);
        ticketBaiRequest.Sujetos.Emisor.NIF = _configuration.EmisorNif;
        ticketBaiRequest.Sujetos.Emisor.ApellidosNombreRazonSocial = _configuration.EmisorApellidosNombreRazonSocial;

        var xml = XmlHelpers.GetXMLIncludingNamespace(ticketBaiRequest);
        var (requestContent, signature) = XmlHelpers.SignXmlContentWithXades(xml, _ticketBaiTerritory.PolicyIdentifier, _ticketBaiTerritory.PolicyDigest, _configuration.Certificate);

        requestContent = _ticketBaiTerritory.ProcessContent(ticketBaiRequest, requestContent);

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(_ticketBaiTerritory.SandboxEndpoint + endpoint))
        {
            Content = _ticketBaiTerritory.GetHttpContent(requestContent)
        };
        _ticketBaiTerritory.AddHeaders(ticketBaiRequest, httpRequestMessage.Headers);

        var response = await _httpClient.SendAsync(httpRequestMessage);

        var (success, responseMessages, responseContent) = await _ticketBaiTerritory.GetSuccess(response);

        if (!success)
        {
            throw new AggregateException(responseMessages.Select(r => new Exception($"{r.code}: {r.message}")));
        }

        request.ReceiptResponse.AddSignatureItem(new SignatureItem()
        {
            Caption = "",
            Data = GetIdentier(request, ticketBaiRequest, signature).ToString(),
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = ifPOS.v2.Cases.SignatureType.Unknown.WithCountry("ES")
        });

        request.ReceiptResponse.AddSignatureItem(new SignatureItem()
        {
            Caption = "[www.fiskaltrust.es]",
            Data = GetQrCodeUri(request, ticketBaiRequest, signature).ToString(),
            ftSignatureFormat = SignatureFormat.QRCode,
            ftSignatureType = SignatureTypeES.Url.As<ifPOS.v2.Cases.SignatureType>()
        });

        request.ReceiptResponse.AddSignatureItem(new SignatureItem()
        {
            Caption = "Signature",
            Data = Convert.ToBase64String(signature.SignatureValue!),
            ftSignatureFormat = SignatureFormat.Base64,
            ftSignatureType = SignatureTypeES.Signature.As<ifPOS.v2.Cases.SignatureType>().WithFlag(SignatureTypeFlags.DontVisualize)
        });

        foreach (var message in responseMessages)
        {
            request.ReceiptResponse.AddSignatureItem(new SignatureItem()
            {
                Caption = $"Codigo {message.code}",
                Data = message.message,
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = ifPOS.v2.Cases.SignatureType.Unknown.WithCategory(SignatureTypeCategory.Information)
            });
        }


        middlewareStateData.ES.GovernmentAPI = new GovernmentAPI
        {
            Version = GovernmentAPISchemaVersion.V0,
            Request = requestContent,
            Response = responseContent
        };
        request.ReceiptResponse.ftStateData = middlewareStateData;

        return new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse
        };
    }

    private string GetIdentier(ProcessRequest request, TicketBaiRequest ticketBaiRequest, XadesSignedXml signature)
    {
        string datePart = request.ReceiptResponse.ftReceiptMoment.ToString("ddMMyy");
        string first13 = Convert.ToBase64String(signature.SignatureValue!).Substring(0, Math.Min(13, Convert.ToBase64String(signature.SignatureValue!).Length));
        string baseId = $"TBAI-{Uri.EscapeDataString(ticketBaiRequest.Sujetos.Emisor.NIF)}-{datePart}-{first13}";
        string crcInput = baseId + "-";
        var crc8 = new CRC8Calculator();
        var identifier = $"{baseId}-{CRC8Calculator.Calculate(crcInput)}";
        return identifier;
    }

    private Uri GetQrCodeUri(ProcessRequest request, TicketBaiRequest ticketBaiRequest, XadesSignedXml signature)
    {
        var identifier = GetIdentier(request, ticketBaiRequest, signature);
        return new Uri(BuildValidationUrl(identifier, ticketBaiRequest.Factura.CabeceraFactura.SerieFactura, ticketBaiRequest.Factura.CabeceraFactura.NumFactura, ticketBaiRequest.Factura.DatosFactura.ImporteTotalFactura));
    }


    public static string BuildValidationUrl(string identifier, string series, string number, string total, string baseUrl = "https://batuz.eus/QRTBAI/")
    {
        if (!baseUrl.EndsWith("/"))
            baseUrl += "/";
        string urlWithoutCrc = $"{baseUrl}?id={identifier}&s={Uri.EscapeDataString(series)}&nf={number}&i={total}";
        return $"{urlWithoutCrc}&cr={CRC8Calculator.Calculate(urlWithoutCrc)}";
    }

    public Task<ESSSCDInfo> GetInfoAsync() => throw new NotImplementedException();

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => Task.FromResult(new EchoResponse { Message = echoRequest.Message });
}
