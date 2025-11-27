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

        var ticketBaiRequest = _ticketBaiFactory.ConvertTo(request, middlewareStateData.ES.LastReceipt);
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
            ftSignatureType = SignatureTypeES.Signature.As<ifPOS.v2.Cases.SignatureType>()
        });

        request.ReceiptResponse.AddSignatureItem(new SignatureItem()
        {
            Caption = "Series",
            Data = ticketBaiRequest.Factura.CabeceraFactura.SerieFactura,
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = (ifPOS.v2.Cases.SignatureType) 0x4553_2000_0000_0005
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


    private Uri GetQrCodeUri(ProcessRequest request, TicketBaiRequest ticketBaiRequest, XadesSignedXml signature)
    {
        var crc8 = new CRC8Calculator();
        var url = $"{new Uri(_ticketBaiTerritory.QrCodeSandboxValidationEndpoint)}?{IdentifierUrl(request, ticketBaiRequest, signature)}";
        var cr8 = crc8.ComputeChecksum(url).ToString();
        url += $"&cr={cr8.PadLeft(3, '0')}";
        return new Uri(url);
    }

    private string IdentifierUrl(ProcessRequest request, TicketBaiRequest ticketBaiRequest, XadesSignedXml signature)
    {
        var ticketBaiIdentifier = $"TBAI-{ticketBaiRequest.Sujetos.Emisor.NIF}-{request.ReceiptResponse.ftReceiptMoment:ddMMyy}-{Convert.ToBase64String(signature.SignatureValue!.Take(12).ToArray()).Substring(0, 13)}-";
        var crc8 = new CRC8Calculator();
        ticketBaiIdentifier += crc8.ComputeChecksum(ticketBaiIdentifier).ToString("000");
        return $"id={HttpUtility.UrlEncode(ticketBaiIdentifier)}&s={HttpUtility.UrlEncode(ticketBaiRequest.Factura.CabeceraFactura.SerieFactura)}&nf={HttpUtility.UrlEncode(ticketBaiRequest.Factura.CabeceraFactura.NumFactura)}&i={HttpUtility.UrlEncode(ticketBaiRequest.Factura.DatosFactura.ImporteTotalFactura)}";
    }

    public Task<ESSSCDInfo> GetInfoAsync() => throw new NotImplementedException();

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => Task.FromResult(new EchoResponse { Message = echoRequest.Message });
}
