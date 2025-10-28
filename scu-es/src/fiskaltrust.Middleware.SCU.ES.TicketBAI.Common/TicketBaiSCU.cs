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
using System.Text.Json;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Territories;
using Microsoft.Extensions.Logging;
using System.Xml;

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

    public async Task<SubmitResponse> SubmitInvoiceAsync(SubmitInvoiceRequest request)
    {
        var ticketBaiRequest = _ticketBaiFactory.ConvertTo(request);
        ticketBaiRequest.Sujetos.Emisor.NIF = _configuration.EmisorNif;
        ticketBaiRequest.Sujetos.Emisor.ApellidosNombreRazonSocial = _configuration.EmisorApellidosNombreRazonSocial;

        var xml = XmlHelpers.GetXMLIncludingNamespace(ticketBaiRequest);
        var content = XmlHelpers.SignXmlContentWithXades(xml, _ticketBaiTerritory.PolicyIdentifier, _ticketBaiTerritory.PolicyDigest, _configuration.Certificate);

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(_ticketBaiTerritory.SandboxEndpoint + _ticketBaiTerritory.SubmitInvoices))
        {
            Content = _ticketBaiTerritory.GetContent(ticketBaiRequest, content)
        };
        _ticketBaiTerritory.AddHeaders(ticketBaiRequest, httpRequestMessage.Headers);

        var response = await _httpClient.SendAsync(httpRequestMessage);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = GetResponseFromContent(responseContent, ticketBaiRequest);
        result.RequestContent = content;
        return result;
    }

    public async Task<SubmitResponse> CancelInvoiceAsync(SubmitInvoiceRequest request)
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