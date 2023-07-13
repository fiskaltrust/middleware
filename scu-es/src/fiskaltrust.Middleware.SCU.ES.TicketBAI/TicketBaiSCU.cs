using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Territories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

#pragma warning disable IDE0052

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public class TicketBaiSCU : IESSSCD
{
    private readonly TicketBaiSCUConfiguration _configuration;
    private readonly TicketBaiRequestFactory _ticketBaiRequestFactory;
    private readonly Sujetos _sujetos;
    private readonly Cabecera _cabacera;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TicketBaiSCU> _logger;
    private readonly ITicketBaiTerritory _ticketBaiTerritory;

    public TicketBaiSCU(ILogger<TicketBaiSCU> logger, TicketBaiSCUConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _ticketBaiRequestFactory = new TicketBaiRequestFactory(configuration);
        _ticketBaiTerritory = configuration.TicketBaiTerritory switch
        {
            TicketBaiTerritory.Araba => new Araba(),
            TicketBaiTerritory.Bizkaia => new Bizkaia(),
            TicketBaiTerritory.Gipuzkoa => new Gipuzkoa(),
            _ => throw new Exception("Not supported"),
        };

        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(_configuration.Certificate);
        _sujetos = new Sujetos
        {
            Emisor = new Emisor
            {
                NIF = _configuration.EmisorNif,
                ApellidosNombreRazonSocial = _configuration.EmisorApellidosNombreRazonSocial
            },
            VariosDestinatarios = SiNoType.N,
            EmitidaPorTercerosODestinatario = EmitidaPorTercerosType.N
        };
        _cabacera = new Cabecera
        {
            IDVersionTBAI = IDVersionTicketBaiType.Item1Period2
        };
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_ticketBaiTerritory.SandboxEndpoint)
        };
    }

    public async Task<SubmitResponse> SubmitInvoiceAsync(SubmitInvoiceRequest request)
    {
        var ticketBaiRequest = ConvertTo(request);
   

        var content = _ticketBaiRequestFactory.CreateXadesSignedXmlContent(ticketBaiRequest);
        var httpRequestHeaders = new HttpRequestMessage(HttpMethod.Post, new Uri(_ticketBaiTerritory.SandboxEndpoint + _ticketBaiTerritory.SubmitInvoices))
        {
            Content = new StringContent(content, Encoding.UTF8, "application/xml")
        };
        if (_configuration.TicketBaiTerritory == TicketBaiTerritory.Bizkaia)
        {
            httpRequestHeaders.Headers.Add("eus-bizkaia-n3-version", "1.0");
            httpRequestHeaders.Headers.Add("eus-bizkaia-n3-content-type", "application/xml");
            // TODO which year needs to be transmitted?
            httpRequestHeaders.Headers.Add("eus-bizkaia-n3-data", JsonConvert.SerializeObject(Bizkaia.GenerateHeader(ticketBaiRequest.Sujetos.Emisor.NIF, ticketBaiRequest.Sujetos.Emisor.ApellidosNombreRazonSocial, "240", DateTime.UtcNow.Year.ToString())));
        }

        var response = await _httpClient.SendAsync(httpRequestHeaders);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = _ticketBaiRequestFactory.GetResponseFromContent(responseContent, ticketBaiRequest);
        result.RequestContent = content;
        return result;
    }

    public async Task<SubmitResponse> CancelInvoiceAsync(SubmitInvoiceRequest request)
    {
        var ticketBaiRequest = ConvertTo(request);
        var content = _ticketBaiRequestFactory.CreateXadesSignedXmlContent(ticketBaiRequest);
        var response = await _httpClient.PostAsync(_ticketBaiTerritory.CancelInvoices, new StringContent(content, Encoding.UTF8, "application/xml"));
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = _ticketBaiRequestFactory.GetResponseFromContent(responseContent, ticketBaiRequest);
        result.RequestContent = content;
        return result;
    }

    public string GetRawXml(SubmitInvoiceRequest ticketBaiRequest)
    {
        return _ticketBaiRequestFactory.CreateXadesSignedXmlContent(ConvertTo(ticketBaiRequest));
    }

    public TicketBaiRequest ConvertTo(SubmitInvoiceRequest request)
    {
        var ticketBaiRequest = new TicketBaiRequest
        {
            Cabecera = _cabacera,
            Sujetos = _sujetos,
            HuellaTBAI = new HuellaTBAI
            {
                EncadenamientoFacturaAnterior = new EncadenamientoFacturaAnteriorType
                {
                    SerieFacturaAnterior = "CTS2-2023",
                    NumFacturaAnterior = "0002",
                    FechaExpedicionFacturaAnterior = "26-06-2023",
                    SignatureValueFirmaFacturaAnterior = "VJzuyDtdogaJ7RgGvSSqpw17xj8QUVUp/9wOlWn8W+iCMRQ1u6HC+XuRkftbec/oD0ryoyp1iB1feZuR2hzEPTZIS49rv2atWlON"
                },
                Software = new SoftwareFacturacionType
                {
                    LicenciaTBAI = "TBAIGIPRE00000001035",
                    EntidadDesarrolladora = new EntidadDesarrolladoraType
                    {
                        NIF = "B10646545"
                    },
                    Nombre = "Incodebiz",
                    Version = "1.0"
                },
                NumSerieDispositivo = "GP4FC5J"
            },
            Factura = new Factura
            {
                CabeceraFactura = new CabeceraFacturaType
                {
                    SerieFactura = request.Series,
                    NumFactura = request.InvoiceNumber,
                    FechaExpedicionFactura = request.InvoiceMoment.ToString("dd-MM-yyyy"),
                    HoraExpedicionFactura = request.InvoiceMoment.ToString("HH:mm:ss"),
                    FacturaSimplificada = SiNoType.S,
                    FacturaEmitidaSustitucionSimplificada = SiNoType.N,
                },
                DatosFactura = new DatosFacturaType
                {
                    FechaOperacion = "15-10-2021",
                    DescripcionFactura = "Servicios Prueba",
                    DetallesFactura = new List<IDDetalleFacturaType> {
                          new IDDetalleFacturaType
                                    {
                                        DescripcionDetalle = "test object",
                                        Cantidad = "1",
                                        ImporteUnitario = "100.00",
                                        Descuento = "0",
                                        ImporteTotal = "121.00"
                                    }
                        },
                    ImporteTotalFactura = "121.00",
                    Claves = new List<IDClaveType>
                        {
                            new IDClaveType
                            {
                                ClaveRegimenIvaOpTrascendencia = IdOperacionesTrascendenciaTributariaType.Item01
                            }
                        }
                },
                TipoDesglose = new TipoDesgloseType
                {
                    DesgloseFactura = new DesgloseFacturaType
                    {
                        Sujeta = new SujetaType
                        {
                            NoExenta = new List<DetalleNoExentaType>
                                 {
                                     new DetalleNoExentaType
                                     {
                                         TipoNoExenta = TipoOperacionSujetaNoExentaType.S1,
                                         DesgloseIVA = new List<DetalleIVAType>
                                         {
                                             new DetalleIVAType
                                             {
                                                 BaseImponible = "100.0",
                                                 TipoImpositivo = "21.0",
                                                 CuotaImpuesto = "21.0",
                                                 OperacionEnRecargoDeEquivalenciaORegimenSimplificado = SiNoType.N
                                             }
                                         }
                                     }
                                 }
                        }
                    }
                }
            }
        };

        return ticketBaiRequest;
    }
}
