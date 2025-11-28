using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Models;
using System.Globalization;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.es.Cases;

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

    public TicketBaiRequest ConvertTo(ProcessRequest request, Receipt? lastReceipt)
    {
        var ticketBaiRequest = new TicketBaiRequest
        {
            Cabecera = _cabacera,
            Sujetos = _sujetos,
            HuellaTBAI = CreateHuellTBai(request, lastReceipt),
            Factura = CreateFactura(request, lastReceipt)
        };
        return ticketBaiRequest;
    }

    private static Factura CreateFactura(ProcessRequest request, Receipt? lastReceipt)
    {
        var facturoa = new Factura
        {
            CabeceraFactura = new CabeceraFacturaType
            {
                SerieFactura = "T", // QUESTION
                NumFactura = request.ReceiptRequest.cbReceiptReference,
                FechaExpedicionFactura = request.ReceiptResponse.ftReceiptMoment.ToString("dd-MM-yyyy"),
                HoraExpedicionFactura = request.ReceiptResponse.ftReceiptMoment.ToString("HH:mm:ss"),
                FacturaSimplificada = SiNoType.S,
                FacturaEmitidaSustitucionSimplificada = SiNoType.N,
            },
            DatosFactura = new DatosFacturaType
            {
                FechaOperacion = request.ReceiptResponse.ftReceiptMoment.ToString("dd-MM-yyyy"), //TODO: needs to be set if issuing the invoice was different from the actual date
                DescripcionFactura = "Invoice", //TODO: Can we hardcode this value?
                DetallesFactura = CreateFacturas(request),
                ImporteTotalFactura = request.ReceiptRequest.cbChargeItems.Sum(x => x.Amount).ToString("0.00", CultureInfo.InvariantCulture),
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

    private HuellaTBAI CreateHuellTBai(ProcessRequest request, Receipt? lastReceipt)
    {
        var huellTbai = new HuellaTBAI
        {
            Software = _software,
            NumSerieDispositivo = request.ReceiptResponse.ftCashBoxIdentification
        };

        var numFacturaAnterior = lastReceipt?.Request.cbReceiptReference;
        var signatureValueFirmaFacturaAnterior = lastReceipt?.Response.ftSignatures?.First(x => x.ftSignatureType.IsType(SignatureTypeES.Signature)).Data;
        var serieFacturaAnterior = lastReceipt?.Response.ftSignatures?.First(x => x.ftSignatureType.IsType((SignatureTypeES) 0x4553_2000_0000_0005)).Data;
        var fechaExpedicionFacturaAnterior = lastReceipt?.Response.ftReceiptMoment;
        if (numFacturaAnterior != null && signatureValueFirmaFacturaAnterior != null && fechaExpedicionFacturaAnterior != null)
        {
            huellTbai.EncadenamientoFacturaAnterior = new EncadenamientoFacturaAnteriorType
            {
                SerieFacturaAnterior = serieFacturaAnterior,
                NumFacturaAnterior = numFacturaAnterior,
                FechaExpedicionFacturaAnterior = fechaExpedicionFacturaAnterior.Value.ToString("dd-MM-yyyy"),
                SignatureValueFirmaFacturaAnterior = signatureValueFirmaFacturaAnterior
            };
        }

        return huellTbai;
    }

    private static List<DetalleIVAType> CreateDetalleIVAType(ProcessRequest request)
    {
        var chargeItems = request.ReceiptRequest.cbChargeItems.Select(c =>
        {
            c.VATAmount = c.VATAmount ?? (c.Amount * c.VATRate / 100.0m);
            return c;
        });

        var vatRates = chargeItems.GroupBy(x => x.VATRate);
        return vatRates.Select(x => new DetalleIVAType
        {
            BaseImponible = x.Sum(x => x.Amount - x.VATAmount!.Value).ToString("0.00", CultureInfo.InvariantCulture),
            TipoImpositivo = x.Key.ToString("0.00", CultureInfo.InvariantCulture),
            CuotaImpuesto = x.Sum(x => x.VATAmount!.Value).ToString("0.00", CultureInfo.InvariantCulture),
            OperacionEnRecargoDeEquivalenciaORegimenSimplificado = SiNoType.N
        }).ToList();
    }

    private static List<IDDetalleFacturaType> CreateFacturas(ProcessRequest request)
    {
        var chargeItems = request.ReceiptRequest.cbChargeItems.Select(c =>
        {
            c.VATAmount = c.VATAmount ?? (c.Amount * c.VATRate / 100.0m);
            return c;
        });
        return chargeItems.Select(x => new IDDetalleFacturaType
        {
            DescripcionDetalle = x.Description,
            Cantidad = x.Quantity.ToString("0.00", CultureInfo.InvariantCulture),
            ImporteUnitario = (x.Amount - x.VATAmount!.Value).ToString("0.00", CultureInfo.InvariantCulture),
            //Descuento = "0", TODO How should we handle discounts? is this a must have or can e ignore that
            ImporteTotal = x.Amount.ToString("0.00", CultureInfo.InvariantCulture)
        }).ToList();
    }
}