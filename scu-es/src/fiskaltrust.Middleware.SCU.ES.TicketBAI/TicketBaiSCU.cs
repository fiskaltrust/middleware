using System;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Text.Json;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Helpers;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Territories;
using Microsoft.Extensions.Logging;

#pragma warning disable IDE0052

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public class TicketBaiSCU : IESSSCD
{
    private readonly TicketBaiSCUConfiguration _configuration;

    private readonly HttpClient _httpClient;
    private readonly TicketBaiFactory _ticketBaiFactory;
    private readonly ILogger<TicketBaiSCU> _logger;
    private readonly ITicketBaiTerritory _ticketBaiTerritory;
    private readonly Uri _baseAddress;
    private readonly Uri _qrCodeBaseAddress;

    public TicketBaiSCU(ILogger<TicketBaiSCU> logger, TicketBaiSCUConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _ticketBaiTerritory = configuration.TicketBaiTerritory switch
        {
            TicketBaiTerritory.Araba => new Araba(),
            TicketBaiTerritory.Bizkaia => new Bizkaia(),
            TicketBaiTerritory.Gipuzkoa => new Gipuzkoa(),
            _ => throw new Exception("Not supported"),
        };
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

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        var submitInvoiceRequest = new SubmitInvoiceRequest
        {
            ftCashBoxIdentification = request.ReceiptResponse.ftCashBoxIdentification,
            InvoiceMoment = request.ReceiptResponse.ftReceiptMoment,
            InvoiceNumber = request.ReceiptRequest.cbReceiptReference!, // QUESTION
            LastInvoiceMoment = request.PreviousReceiptResponse?.ftReceiptMoment,
            LastInvoiceNumber = request.PreviousReceiptRequest?.cbReceiptReference,
            LastInvoiceSignature = request.PreviousReceiptResponse?.ftSignatures?.First(x => x.ftSignatureType.IsType(SignatureTypeES.Signature)).Data,
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

        var submitResponse = request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void)
            ? await CancelInvoiceAsync(submitInvoiceRequest)
            : await SubmitInvoiceAsync(submitInvoiceRequest);

        if (!submitResponse.Succeeded)
        {
            throw new AggregateException(submitResponse.ResultMessages.Select(r => new Exception($"{r.code}: {r.message}")));
        }

        request.ReceiptResponse.AddSignatureItem(new SignatureItem()
        {
            Caption = "[www.fiskaltrust.es]",
            Data = submitResponse.QrCode!.ToString(),
            ftSignatureFormat = SignatureFormat.QRCode,
            ftSignatureType = SignatureTypeES.Url.As<SignatureType>()
        });

        request.ReceiptResponse.AddSignatureItem(new SignatureItem()
        {
            Caption = "Signature",
            Data = submitResponse.ShortSignatureValue!,
            ftSignatureFormat = SignatureFormat.Base64,
            ftSignatureType = SignatureTypeES.Signature.As<SignatureType>()
        });

        foreach (var message in submitResponse.ResultMessages)
        {
            request.ReceiptResponse.AddSignatureItem(new SignatureItem()
            {
                Caption = $"Codigo {message.code}",
                Data = message.message,
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureType.Unknown.WithCategory(SignatureTypeCategory.Information)
            });
        }

        return new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse
        };
    }

    public Task<ESSSCDInfo> GetInfoAsync() => throw new NotImplementedException();


    private async Task<SubmitResponse> SubmitInvoiceAsync(SubmitInvoiceRequest request)
    {
        var ticketBaiRequest = _ticketBaiFactory.ConvertTo(request);
        if (_configuration.TicketBaiTerritory == TicketBaiTerritory.Bizkaia)
        {
            ticketBaiRequest.Sujetos.Emisor.NIF = "A99807000";
            ticketBaiRequest.Sujetos.Emisor.ApellidosNombreRazonSocial = "yDhFHBTxpf6J3eD79i9eawNbaLRb22";
        }
        var xml = XmlHelpers.GetXMLIncludingNamespace(ticketBaiRequest);
        var content = XmlHelpers.SignXmlContentWithXades(xml, _ticketBaiTerritory.PolicyIdentifier, _ticketBaiTerritory.PolicyDigest, _configuration.Certificate);
        if (_configuration.TicketBaiTerritory == TicketBaiTerritory.Bizkaia)
        {
            var rawContent = $"""
<?xml version="1.0" encoding="UTF-8" standalone="no"?>
<lrpjfecsgap:LROEPJ240FacturasEmitidasConSGAltaPeticion
	xmlns:lrpjfecsgap="https://www.batuz.eus/fitxategiak/batuz/LROE/esquemas/LROE_PJ_240_1_1_FacturasEmitidas_ConSG_AltaPeticion_V1_0_2.xsd">
	<Cabecera>
		<Modelo>240</Modelo>
		<Capitulo>1</Capitulo>
		<Subcapitulo>1.1</Subcapitulo>
		<Operacion>A00</Operacion>
		<Version>1.0</Version>
		<Ejercicio>{DateTime.UtcNow.Year.ToString()}</Ejercicio>
		<ObligadoTributario>
	        <NIF>{ticketBaiRequest.Sujetos.Emisor.NIF}</NIF>
            <ApellidosNombreRazonSocial>{ticketBaiRequest.Sujetos.Emisor.ApellidosNombreRazonSocial}</ApellidosNombreRazonSocial>
		</ObligadoTributario>
	</Cabecera>
	<FacturasEmitidas>
		<FacturaEmitida>
			<TicketBai>{Convert.ToBase64String(Encoding.UTF8.GetBytes(content))}</TicketBai>
		</FacturaEmitida>
	</FacturasEmitidas>
</lrpjfecsgap:LROEPJ240FacturasEmitidasConSGAltaPeticion>
""";
            var requestContent = new ByteArrayContent(Compress(rawContent));
            requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            requestContent.Headers.Add("Content-Encoding", "gzip");

            var httpRequestHeaders = new HttpRequestMessage(HttpMethod.Post, new Uri(_ticketBaiTerritory.SandboxEndpoint + _ticketBaiTerritory.SubmitInvoices))
            {
                Content = requestContent
            };
            httpRequestHeaders.Headers.Add("eus-bizkaia-n3-version", "1.0");
            httpRequestHeaders.Headers.Add("eus-bizkaia-n3-content-type", "application/xml");
            // TODO which year needs to be transmitted?
            httpRequestHeaders.Headers.Add("eus-bizkaia-n3-data",
                    JsonSerializer.Serialize(Bizkaia.GenerateHeader(ticketBaiRequest.Sujetos.Emisor.NIF, ticketBaiRequest.Sujetos.Emisor.ApellidosNombreRazonSocial, "240", DateTime.UtcNow.Year.ToString())));
            var response = await _httpClient.SendAsync(httpRequestHeaders);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = GetResponseFromContent(responseContent, ticketBaiRequest);
            result.RequestContent = content;
            return result;
        }
        else
        {
            var httpRequestHeaders = new HttpRequestMessage(HttpMethod.Post, new Uri(_ticketBaiTerritory.SandboxEndpoint + _ticketBaiTerritory.SubmitInvoices))
            {
                Content = new StringContent(content, Encoding.UTF8, "application/xml")
            };
            var response = await _httpClient.SendAsync(httpRequestHeaders);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = GetResponseFromContent(responseContent, ticketBaiRequest);
            result.RequestContent = content;
            return result;
        }
    }

    private byte[] Compress(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        using (var compressedStream = new MemoryStream())
        {
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(bytes, 0, bytes.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }
    }

    private async Task<SubmitResponse> CancelInvoiceAsync(SubmitInvoiceRequest request)
    {
        var ticketBaiRequest = _ticketBaiFactory.ConvertTo(request);
        var xml = XmlHelpers.GetXMLIncludingNamespace(ticketBaiRequest);
        var content = XmlHelpers.SignXmlContentWithXades(xml, _ticketBaiTerritory.PolicyIdentifier, _ticketBaiTerritory.PolicyDigest, _configuration.Certificate);
        var httpRequestHeaders = new HttpRequestMessage(HttpMethod.Post, new Uri(_ticketBaiTerritory.SandboxEndpoint + _ticketBaiTerritory.CancelInvoices))
        {
            Content = new StringContent(content, Encoding.UTF8, "application/xml")
        };
        var response = await _httpClient.SendAsync(httpRequestHeaders);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = GetResponseFromContent(responseContent, ticketBaiRequest);
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
}
