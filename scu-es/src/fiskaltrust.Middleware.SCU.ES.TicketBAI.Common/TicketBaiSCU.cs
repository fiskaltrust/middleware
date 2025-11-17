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
    private readonly Uri _qrCodeBaseAddress;

    public TicketBaiSCU(ILogger<TicketBaiSCU> logger, TicketBaiSCUConfiguration configuration, ITicketBaiTerritory ticketBaiTerritory)
    {
        _logger = logger;
        _configuration = configuration;
        _ticketBaiTerritory = ticketBaiTerritory;
        _baseAddress = new Uri(_ticketBaiTerritory.SandboxEndpoint);
        _qrCodeBaseAddress = new Uri(_ticketBaiTerritory.QrCodeSandboxValidationEndpoint);

        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(_configuration.Certificate);
        handler.AutomaticDecompression = DecompressionMethods.GZip;


        _httpClient = new HttpClient(handler)
        {
            BaseAddress = _baseAddress
        };
        _ticketBaiFactory = new TicketBaiFactory(configuration);
    }

    public async Task<SubmitResponse> SendAsync(SubmitInvoiceRequest request, string endpoint)
    {
        var ticketBaiRequest = _ticketBaiFactory.ConvertTo(request);
        ticketBaiRequest.Sujetos.Emisor.NIF = _configuration.EmisorNif;
        ticketBaiRequest.Sujetos.Emisor.ApellidosNombreRazonSocial = _configuration.EmisorApellidosNombreRazonSocial;

        var xml = XmlHelpers.GetXMLIncludingNamespace(ticketBaiRequest);
        var content = XmlHelpers.SignXmlContentWithXades(xml, _ticketBaiTerritory.PolicyIdentifier, _ticketBaiTerritory.PolicyDigest, _configuration.Certificate);

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(_ticketBaiTerritory.SandboxEndpoint + endpoint))
        {
            Content = _ticketBaiTerritory.GetContent(ticketBaiRequest, content)
        };
        _ticketBaiTerritory.AddHeaders(ticketBaiRequest, httpRequestMessage.Headers);

        var response = await _httpClient.SendAsync(httpRequestMessage);
        var responseContent = await _ticketBaiTerritory.GetResponse(response);
        if (responseContent.IsErr)
        {
            var errorResponse = responseContent.ErrValue!.Match(
                ok => GetResponseFromContent(ok, ticketBaiRequest),
                err => throw err
            );
            throw new AggregateException(errorResponse.ResultMessages.Select(x => new Exception($"{x.code}: {x.message}")));
        }
        var result = GetResponseFromContent(responseContent.OkValue!, ticketBaiRequest);
        result.RequestContent = content;
        return result;
    }

    private Uri GetQrCodeUri(TicketBaiRequest ticketBaiRequest, TicketBaiResponse ticketBaiResponse)
    {
        var crc8 = new CRC8Calculator();
        var url = $"{_qrCodeBaseAddress}?{IdentifierUrl(ticketBaiResponse.Salida.IdentificadorTBAI, ticketBaiRequest)}";
        var cr8 = crc8.ComputeChecksum(url).ToString();
        url += $"&cr={cr8.PadLeft(3, '0')}";
        return new Uri(url);
    }

    private string IdentifierUrl(string ticketBaiIdentifier, TicketBaiRequest ticketBaiRequest) => $"id={HttpUtility.UrlEncode(ticketBaiIdentifier)}&s={HttpUtility.UrlEncode(ticketBaiRequest.Factura.CabeceraFactura.SerieFactura)}&nf={HttpUtility.UrlEncode(ticketBaiRequest.Factura.CabeceraFactura.NumFactura)}&i={HttpUtility.UrlEncode(ticketBaiRequest.Factura.DatosFactura.ImporteTotalFactura)}";

    private SubmitResponse GetResponseFromContent(string responseContent, TicketBaiRequest ticketBaiRequest)
    {
        var ticketBaiResponse = XmlHelpers.ParseXML<TicketBaiResponse>(responseContent) ?? throw new Exception("Something horrible has happened");
        if (ticketBaiResponse.Salida.Estado == "00")
        {
            var identifier = ticketBaiResponse.Salida.IdentificadorTBAI.Split('-');
            return new SubmitResponse
            {
                IssuerVatId = identifier[1],
                ExpeditionDate = identifier[2],
                ShortSignatureValue = identifier[3],
                Identifier = ticketBaiResponse.Salida.IdentificadorTBAI,
                ResponseContent = responseContent,
                SignatureValue = ticketBaiRequest.Signature,
                Succeeded = true,
                QrCode = GetQrCodeUri(ticketBaiRequest, ticketBaiResponse),
                ResultMessages = ticketBaiResponse.Salida?.ResultadosValidacion?.Select(x => (x.Codigo, x.Descripcion))?.ToList() ?? new()
            };
        }
        else
        {
            return new SubmitResponse
            {
                ResponseContent = responseContent,
                Succeeded = false,
                ResultMessages = ticketBaiResponse.Salida.ResultadosValidacion.Select(x => (x.Codigo, x.Descripcion)).ToList()
            };
        }
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

        var submitInvoiceRequest = new SubmitInvoiceRequest
        {
            ftCashBoxIdentification = request.ReceiptResponse.ftCashBoxIdentification,
            InvoiceMoment = request.ReceiptResponse.ftReceiptMoment,
            InvoiceNumber = request.ReceiptRequest.cbReceiptReference!, // QUESTION
            LastInvoiceMoment = middlewareStateData.ES.LastReceipt?.Response.ftReceiptMoment,
            LastInvoiceNumber = middlewareStateData.ES.LastReceipt?.Request.cbReceiptReference,
            LastInvoiceSignature = middlewareStateData.ES.LastReceipt?.Response.ftSignatures?.First(x => x.ftSignatureType.IsType(SignatureTypeES.Signature)).Data,
            Series = "",
            InvoiceLine = request.ReceiptRequest.cbChargeItems.Select(c => new InvoiceLine
            {
                Amount = c.Amount,
                Description = c.Description,
                Quantity = c.Quantity,
                VATAmount = c.VATAmount ?? (c.Amount * c.VATRate),
                VATRate = c.VATRate
            }).ToList()
        };

        var submitResponse = await SendAsync(
            submitInvoiceRequest,
            !request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void)
                ? _ticketBaiTerritory.SubmitInvoices
                : _ticketBaiTerritory.CancelInvoices);

        if (!submitResponse.Succeeded)
        {
            throw new AggregateException(submitResponse.ResultMessages.Select(r => new Exception($"{r.code}: {r.message}")));
        }

        request.ReceiptResponse.AddSignatureItem(new SignatureItem()
        {
            Caption = "[www.fiskaltrust.es]",
            Data = submitResponse.QrCode!.ToString(),
            ftSignatureFormat = SignatureFormat.QRCode,
            ftSignatureType = SignatureTypeES.Url.As<ifPOS.v2.Cases.SignatureType>()
        });

        request.ReceiptResponse.AddSignatureItem(new SignatureItem()
        {
            Caption = "Signature",
            Data = submitResponse.ShortSignatureValue!,
            ftSignatureFormat = SignatureFormat.Base64,
            ftSignatureType = SignatureTypeES.Signature.As<ifPOS.v2.Cases.SignatureType>()
        });

        foreach (var message in submitResponse.ResultMessages)
        {
            request.ReceiptResponse.AddSignatureItem(new SignatureItem()
            {
                Caption = $"Codigo {message.code}",
                Data = message.message,
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = ifPOS.v2.Cases.SignatureType.Unknown.WithCategory(SignatureTypeCategory.Information)
            });
        }


        // middlewareStateData.ES.GovernmentAPI = governmentAPI;
        request.ReceiptResponse.ftStateData = middlewareStateData;

        return new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse
        };
    }

    public Task<ESSSCDInfo> GetInfoAsync() => throw new NotImplementedException();

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => Task.FromResult(new EchoResponse { Message = echoRequest.Message });
}
