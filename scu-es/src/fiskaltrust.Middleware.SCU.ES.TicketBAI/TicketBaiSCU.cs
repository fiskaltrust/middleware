using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Models;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public sealed class TicketBaiSCU //: IESSSCD 
{
    private readonly TicketBaiSCUConfiguration _configuration;
    private readonly TicketBaiRequestFactory _ticketBaiRequestFactory;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TicketBaiSCU> _logger;

    public TicketBaiSCU(ILogger<TicketBaiSCU> logger, TicketBaiSCUConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _ticketBaiRequestFactory = new TicketBaiRequestFactory(configuration);
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(_configuration.Certificate);
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(Digests.Gipuzkoa.SANDBOX_ENDPOINT)
        };
    }

    public async Task<TicketBaiResult> SubmitInvoiceAsync(TicketBaiRequest ticketBaiRequest)
    {
        var content = _ticketBaiRequestFactory.CreateXadesSignedXmlContent(ticketBaiRequest, Digests.Gipuzkoa.POLICY_IDENTIFIER, Digests.Gipuzkoa.POLICY_DIGEST, Digests.Gipuzkoa.POLICY_IDENTIFIER);
        var response = await _httpClient.PostAsync(Digests.Gipuzkoa.SUBMIT_INVOICES, new StringContent(content, Encoding.UTF8, "application/xml"));
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successful");
        }
        var responseContent = await response.Content.ReadAsStringAsync();
        var ticketBaiResponse = ParseHelpers.ParseXML<TicketBaiResponse>(responseContent) ?? throw new Exception("Something horrible has happened");
        if (ticketBaiResponse.Salida.Estado == "00")
        {
            var identifier = ticketBaiResponse.Salida.IdentificadorTBAI.Split('-');
            var result =  new TicketBaiResult
            {
                IssuerVatId = identifier[1],
                ExpeditionDate = identifier[2],
                ShortSignatureValue = identifier[3],
                Identifier = ticketBaiResponse.Salida.IdentificadorTBAI,
                Content = responseContent,
                Succeeded = true,
            };
            var crc8 = new CRC8Calculator();
            var url = $"{GetTerritoryUrl()}?{IdentifierUrl(ticketBaiResponse.Salida.IdentificadorTBAI, ticketBaiRequest)}";
            var cr8 = crc8.ComputeChecksum(url).ToString();
            url += $"&cr={cr8.PadLeft(3, '0')}";
            result.QrCode = new Uri(url);
            return result;
        }
        else
        {
            return new TicketBaiResult
            {
                Content = responseContent,
                Succeeded = false
            };
        }
    }

    private string IdentifierUrl(string ticketBaiIdentifier, TicketBaiRequest ticketBaiRequest)
    {
        return string.Format("id={0}&s={1}&nf={2}&i={3}",
            HttpUtility.UrlEncode(ticketBaiIdentifier),
            HttpUtility.UrlEncode(ticketBaiRequest.Factura.CabeceraFactura.SerieFactura),
            HttpUtility.UrlEncode(ticketBaiRequest.Factura.CabeceraFactura.NumFactura),
            HttpUtility.UrlEncode(ticketBaiRequest.Factura.DatosFactura.ImporteTotalFactura)
        ); 
    }

public async Task<string> CancelInvoiceAsync(TicketBaiRequest ticketBaiRequest)
    {
        var content = _ticketBaiRequestFactory.CreateXadesSignedXmlContent(ticketBaiRequest, Digests.Gipuzkoa.POLICY_IDENTIFIER, Digests.Gipuzkoa.POLICY_DIGEST, Digests.Gipuzkoa.POLICY_IDENTIFIER);
        var response = await _httpClient.PostAsync(Digests.Gipuzkoa.CANCEL_INVOICES, new StringContent(content, Encoding.UTF8, "application/xml"));
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successful");
        }
        return await response.Content.ReadAsStringAsync();
    }


    public Uri GetTerritoryUrl()
    {
        return new Uri("https://tbai.prep.gipuzkoa.eus/qr/");
    }
}

public class CRC8Calculator
{
    private readonly byte[] table = new byte[256];

    public CRC8Calculator()
    {
        GenerateTable();
    }

    public byte ComputeChecksum(string input)
    {
        var data = Encoding.UTF8.GetBytes(input);
        byte crc = 0;
        foreach (var b in data)
        {
            crc = table[crc ^ b];
        }
        return crc;
    }

    private void GenerateTable()
    {
        byte polynomial = 0x07;
        for (var i = 0; i < 256; i++)
        {
            var temp = (byte) i;
            for (byte j = 0; j < 8; j++)
            {
                if ((temp & 0x80) != 0)
                {
                    temp = (byte) ((temp << 1) ^ polynomial);
                }
                else
                {
                    temp <<= 1;
                }
            }
            table[i] = temp;
        }
    }
}

public class TicketBaiResult
{
    public string? Content { get; set; }
    public bool Succeeded { get; set; }
    public Uri? QrCode { get; set; }
    public string? ShortSignatureValue { get; set; }
    public string? ExpeditionDate { get; set; }
    public string? IssuerVatId { get; set; }
    public string? Identifier { get; set; }
}
