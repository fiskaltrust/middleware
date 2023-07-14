using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Helpers;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Territories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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

    public async Task<SubmitResponse> SubmitInvoiceAsync(SubmitInvoiceRequest request)
    {
        var ticketBaiRequest = _ticketBaiFactory.ConvertTo(request);
        var xml = XmlHelpers.GetXMLIncludingNamespace(ticketBaiRequest);
        var content = XmlHelpers.SignXmlContentWithXades(xml, _ticketBaiTerritory.PolicyIdentifier, _ticketBaiTerritory.PolicyDigest, _configuration.Certificate);
        if (_configuration.TicketBaiTerritory == TicketBaiTerritory.Bizkaia)
        {
            var rawContent = $"""
<?xml version="1.0" encoding="UTF-8" standalone="no"?>
<lrpficfcsgap:LROEPF140IngresosConFacturaConSGAltaPeticion
	xmlns:lrpficfcsgap="https://www.batuz.eus/fitxategiak/batuz/LROE/esquemas/LROE_PF_140_1_1_Ingresos_ConfacturaConSG_AltaPeticion_V1_0_2.xsd">
	<Cabecera>
		<Modelo>140<Modelo>
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
	<Ingresos>
		<Ingreso>
			<TicketBai>{Convert.ToBase64String(Encoding.UTF8.GetBytes(xml))}</TicektBai>
		</Ingreso>
	</Ingresos>
</lrpficfcsgap:LROEPF140IngresosConFacturaConSGAltaPeticion>
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
                    JsonConvert.SerializeObject(Bizkaia.GenerateHeader("99980645J", "wfJnAHYUqt bPKctDve9Y YjkezB8PV9", "140", DateTime.UtcNow.Year.ToString())));
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

    public byte[] Compress(string data)
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



    public async Task<SubmitResponse> CancelInvoiceAsync(SubmitInvoiceRequest request)
    {
        var ticketBaiRequest = _ticketBaiFactory.ConvertTo(request);
        var xml = XmlHelpers.GetXMLIncludingNamespace(ticketBaiRequest);
        var content = XmlHelpers.SignXmlContentWithXades(xml, _ticketBaiTerritory.PolicyIdentifier, _ticketBaiTerritory.PolicyDigest, _configuration.Certificate);
        var response = await _httpClient.PostAsync(_ticketBaiTerritory.CancelInvoices, new StringContent(content, Encoding.UTF8, "application/xml"));
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = GetResponseFromContent(responseContent, ticketBaiRequest);
        result.RequestContent = content;
        return result;
    }

    public Uri GetQrCodeUri(TicketBaiRequest ticketBaiRequest, TicketBaiResponse ticketBaiResponse)
    {
        var crc8 = new CRC8Calculator();
        var url = $"{_qrCodeBaseAddress}?{IdentifierUrl(ticketBaiResponse.Salida.IdentificadorTBAI, ticketBaiRequest)}";
        var cr8 = crc8.ComputeChecksum(url).ToString();
        url += $"&cr={cr8.PadLeft(3, '0')}";
        return new Uri(url);
    }

    private string IdentifierUrl(string ticketBaiIdentifier, TicketBaiRequest ticketBaiRequest) => $"id={HttpUtility.UrlEncode(ticketBaiIdentifier)}&s={HttpUtility.UrlEncode(ticketBaiRequest.Factura.CabeceraFactura.SerieFactura)}&nf={HttpUtility.UrlEncode(ticketBaiRequest.Factura.CabeceraFactura.NumFactura)}&i={HttpUtility.UrlEncode(ticketBaiRequest.Factura.DatosFactura.ImporteTotalFactura)}";

    public SubmitResponse GetResponseFromContent(string responseContent, TicketBaiRequest ticketBaiRequest)
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
                Succeeded = true,
                QrCode = GetQrCodeUri(ticketBaiRequest, ticketBaiResponse),
                ResultMessages = ticketBaiResponse.Salida.ResultadosValidacion.Select(x => (x.Codigo, x.Descripcion)).ToList()
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