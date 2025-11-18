using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Models;
using System.Globalization;

#pragma warning disable IDE0052

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;

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
            LicenciaTBAI = configuration.SoftwareLicenciaTBAI,
            EntidadDesarrolladora = new EntidadDesarrolladoraType
            {
                NIF = configuration.SoftwareNif
            },
            Nombre = configuration.SoftwareName,
            Version = configuration.SoftwareVersion
        };
    }

    public TicketBaiRequest ConvertTo(SubmitInvoiceRequest request)
    {
        var ticketBaiRequest = new TicketBaiRequest
        {
            Cabecera = _cabacera,
            Sujetos = _sujetos,
            HuellaTBAI = CreateHuellTBai(request),
            Factura = CreateFactura(request)
        };
        return ticketBaiRequest;
    }

    private static Factura CreateFactura(SubmitInvoiceRequest request)
    {
        var facturoa = new Factura
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
                ImporteTotalFactura = request.InvoiceLine.Sum(x => x.Amount).ToString("0.00", CultureInfo.InvariantCulture),
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
                                         DesgloseIVA = CreateDetalleIVAType(request)
                                     }
                                 }
                    }
                }
            }
        };
        return facturoa;
    }

    private HuellaTBAI CreateHuellTBai(SubmitInvoiceRequest request)
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

        return huellTbai;
    }

    private static List<DetalleIVAType> CreateDetalleIVAType(SubmitInvoiceRequest request)
    {
        var vatRates = request.InvoiceLine.GroupBy(x => x.VATRate);
        return vatRates.Select(x => new DetalleIVAType
        {
            BaseImponible = x.Sum(x => x.Amount - x.VATAmount).ToString("0.00", CultureInfo.InvariantCulture),
            TipoImpositivo = x.Key.ToString("0.00", CultureInfo.InvariantCulture),
            CuotaImpuesto = x.Sum(x => x.VATAmount).ToString("0.00", CultureInfo.InvariantCulture),
            OperacionEnRecargoDeEquivalenciaORegimenSimplificado = SiNoType.N
        }).ToList();
    }

    private static List<IDDetalleFacturaType> CreateFacturas(SubmitInvoiceRequest request)
    {
        return request.InvoiceLine.Select(x => new IDDetalleFacturaType
        {
            DescripcionDetalle = x.Description,
            Cantidad = x.Quantity.ToString("0.00", CultureInfo.InvariantCulture),
            ImporteUnitario = (x.Amount - x.VATAmount).ToString("0.00", CultureInfo.InvariantCulture),
            //Descuento = "0", TODO How should we handle discounts? is this a must have or can e ignore that
            ImporteTotal = x.Amount.ToString("0.00", CultureInfo.InvariantCulture)
        }).ToList();
    }
}