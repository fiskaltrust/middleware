using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Models;

#pragma warning disable IDE0052

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public class TicketBaiFactory
{
    private readonly Sujetos _sujetos;
    private readonly Cabecera _cabacera;
    private readonly SoftwareFacturacionType _software;

    public TicketBaiFactory(TicketBaiSCUConfiguration configuration)
    {
        _sujetos = new Sujetos
        {
            Emisor = new Emisor
            {
                NIF = configuration.EmisorNif,
                ApellidosNombreRazonSocial = configuration.EmisorApellidosNombreRazonSocial
            },
            VariosDestinatarios = SiNoType.N,
            EmitidaPorTercerosODestinatario = EmitidaPorTercerosType.N
        };
        _cabacera = new Cabecera
        {
            IDVersionTBAI = IDVersionTicketBaiType.Item1Period2
        };
        _software = new SoftwareFacturacionType
        {
            LicenciaTBAI = "TBAIARbKKFCFdCC00879",
            EntidadDesarrolladora = new EntidadDesarrolladoraType
            {
                NIF = "B10646545"
            },
            Nombre = "Incodebiz",
            Version = "1.0"
        };
    }

    public TicketBaiRequest ConvertTo(SubmitInvoiceRequest request)
    {
        var huellTbai = new HuellaTBAI
        {
            Software = _software,
            NumSerieDispositivo = request.ftCashBoxIdentification
        };

        if (request.LastInvoiceNumber != null && request.LastInvoiceSignature != null && request.LastInvoiceMoment != null)
        {
            huellTbai.EncadenamientoFacturaAnterior = new EncadenamientoFacturaAnteriorType
            {
                SerieFacturaAnterior = request.Series,
                NumFacturaAnterior = request.LastInvoiceNumber,
                FechaExpedicionFacturaAnterior = request.LastInvoiceMoment.Value.ToString("dd-MM-yyyy"),
                SignatureValueFirmaFacturaAnterior = request.LastInvoiceSignature
            };
        }

        var ticketBaiRequest = new TicketBaiRequest
        {
            Cabecera = _cabacera,
            Sujetos = _sujetos,
            HuellaTBAI = huellTbai,
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
                    FechaOperacion = request.InvoiceMoment.ToString("dd-MM-yyyy"), //TODO: needs to be set if issuing the invoice was different from the actual date
                    DescripcionFactura = "Invoice", //TODO: Can we hardcode this value?
                    DetallesFactura = CreateFacturas(request),
                    ImporteTotalFactura = request.InvoiceLine.Sum(x => x.Amount).ToString("#.##"),
                    Claves = new List<IDClaveType>
                        {
                            new IDClaveType
                            {
                                ClaveRegimenIvaOpTrascendencia = IdOperacionesTrascendenciaTributariaType.Item51
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
                                         DesgloseIVA = CreateDetalleIVAType(request)
                                     }
                                 }
                        }
                    }
                }
            }
        };

        return ticketBaiRequest;
    }

    private static List<DetalleIVAType> CreateDetalleIVAType(SubmitInvoiceRequest request)
    {
        var facturas = new List<DetalleIVAType>();
        var vatRates = request.InvoiceLine.GroupBy(x => x.VATRate);
        foreach (var line in vatRates)
        {
            facturas.Add(new DetalleIVAType
            {
                BaseImponible = line.Sum(x => x.Amount - x.VATAmount).ToString("#.##"),
                TipoImpositivo = line.Key.ToString("#.##"),
                CuotaImpuesto = line.Sum(x => x.VATAmount).ToString("#.##"),
                OperacionEnRecargoDeEquivalenciaORegimenSimplificado = SiNoType.S
            });
        }

        return facturas;
    }

    private static List<IDDetalleFacturaType> CreateFacturas(SubmitInvoiceRequest request)
    {
        var facturas = new List<IDDetalleFacturaType>();
        foreach (var line in request.InvoiceLine)
        {
            facturas.Add(new IDDetalleFacturaType
            {
                DescripcionDetalle = line.Description,
                Cantidad = line.Quantity.ToString("#.##"),
                ImporteUnitario = (line.Amount - line.VATAmount).ToString("#.##"),
                //Descuento = "0", TODO How should we handle discounts? is this a must have or can e ignore that
                ImporteTotal = line.Amount.ToString("#.##")
            });
        }

        return facturas;
    }
}