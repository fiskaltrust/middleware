using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Territories;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.es.Cases;
using fiskaltrust.ifPOS.v2.Cases;
using Microsoft.Xades;
using System.Collections.Generic;
using fiskaltrust.Middleware.SCU.ES.Common;
using fiskaltrust.Middleware.SCU.ES.Common.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Factories;
using System.Text.Json;

#pragma warning disable IDE0052

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;

public class TicketBaiSCU : IESSSCD
{
    private readonly List<ReceiptCase> UnprocessedCases = new List<ReceiptCase>
    {
        ReceiptCase.ZeroReceipt0x2000,
        ReceiptCase.OneReceipt0x2001,
        ReceiptCase.DailyClosing0x2011,
        ReceiptCase.MonthlyClosing0x2012,
        ReceiptCase.YearlyClosing0x2013
    };

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
        _baseAddress = configuration.Sandbox ? new Uri(_ticketBaiTerritory.SandboxEndpoint) : new Uri(_ticketBaiTerritory.ProdEndpoint);

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
        try
        {
            if (UnprocessedCases.Contains(request.ReceiptRequest.ftReceiptCase.Case()))
            {
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }

            if (request.ReceiptRequest?.cbChargeItems is { } chargeItems && !chargeItems.All(item => item.ftChargeItemCase != 0))
            {
                request.ReceiptResponse.SetReceiptResponseError("All charge items must specify ftChargeItemCase before sending to TicketBAI.");
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }

            var endpoint = !request.ReceiptRequest!.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void)
                    ? _ticketBaiTerritory.SubmitInvoices
                    : _ticketBaiTerritory.CancelInvoices;

            var ticketBaiRequest = _ticketBaiFactory.ConvertTo(request);
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
                var errors = JsonSerializer.Serialize(responseMessages.Select(r => new Exception($"{r.code}: {r.message}")));
                _logger.LogError("TicketBAI submission failed with errors: {Errors}", errors);
                request.ReceiptResponse.SetReceiptResponseError(errors);
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }

            request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateTBAIIdentifierSignature(GetIdentier(request, ticketBaiRequest, signature).ToString()));
            request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateQRCodeSignature(GetQrCodeUri(request, ticketBaiRequest, signature).ToString()));
            request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateSignatureSignature(signature));

            foreach (var message in responseMessages)
            {
                request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateResponseMessageSignature(message));
            }

            var governmentApi = new GovernmentAPI
            {
                Version = GovernmentAPISchemaVersion.V0,
                Request = requestContent,
                Response = responseContent
            };
            var middlewareStateData = MiddlewareStateData.FromReceiptResponse(request.ReceiptResponse);
            middlewareStateData ??= new MiddlewareStateData();
            middlewareStateData.ES ??= new MiddlewareStateDataES();
            middlewareStateData.ES.GovernmentAPI = governmentApi;
            request.ReceiptResponse.ftStateData = middlewareStateData;
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing TicketBAI receipt.");
            request.ReceiptResponse.AddSignatureItem(new SignatureItem()
            {
                Caption = "scu-ticketbai-unhandled-error",
                Data = ex.Message,
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = ifPOS.v2.Cases.SignatureType.Unknown.WithCategory(SignatureTypeCategory.Failure)
            });
            request.ReceiptResponse.ftState = request.ReceiptResponse.ftState.WithState(State.Error);
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }
    }


    private string GetIdentier(ProcessRequest request, TicketBai ticketBaiRequest, XadesSignedXml signature)
    {
        var datePart = request.ReceiptResponse.ftReceiptMoment.ToString("ddMMyy");
        var first13 = Convert.ToBase64String(signature.SignatureValue!).Substring(0, Math.Min(13, Convert.ToBase64String(signature.SignatureValue!).Length));
        var baseId = $"TBAI-{Uri.EscapeDataString(ticketBaiRequest.Sujetos.Emisor.NIF)}-{datePart}-{first13}";
        var crcInput = baseId + "-";
        var crc8 = new CRC8Calculator();
        var identifier = $"{baseId}-{CRC8Calculator.Calculate(crcInput)}";
        return identifier;
    }

    private Uri GetQrCodeUri(ProcessRequest request, TicketBai ticketBaiRequest, XadesSignedXml signature)
    {
        var identifier = GetIdentier(request, ticketBaiRequest, signature);
        return new Uri(BuildValidationUrl(identifier, ticketBaiRequest.Factura.CabeceraFactura.SerieFactura, ticketBaiRequest.Factura.CabeceraFactura.NumFactura, ticketBaiRequest.Factura.DatosFactura.ImporteTotalFactura));
    }


    public static string BuildValidationUrl(string identifier, string series, string number, string total, string baseUrl = "https://batuz.eus/QRTBAI/")
    {
        if (!baseUrl.EndsWith("/"))
            baseUrl += "/";
        var urlWithoutCrc = $"{baseUrl}?id={identifier}&s={Uri.EscapeDataString(series)}&nf={number}&i={total}";
        return $"{urlWithoutCrc}&cr={CRC8Calculator.Calculate(urlWithoutCrc)}";
    }

    public Task<ESSSCDInfo> GetInfoAsync() => throw new NotImplementedException();

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => Task.FromResult(new EchoResponse { Message = echoRequest.Message });
}
