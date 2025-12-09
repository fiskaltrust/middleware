using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Models;
using System.Globalization;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.ifPOS.v2.es.Cases;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;
using System;

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
            VariosDestinatarios = SiNoType.N, // this probably needs to be S in cases of multiple cbCustomers, but how can one invoice have multiple recipients?
            EmitidaPorTercerosODestinatario = EmitidaPorTercerosType.N,
            // TODDO: We should map the cbCustomer to recepients
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

    public TicketBaiRequest ConvertTo(ProcessRequest request, MiddlewareStateDataES middlewareStateDataES)
    {
        var ticketBaiRequest = new TicketBaiRequest
        {
            Cabecera = _cabacera,
            Sujetos = _sujetos,
            HuellaTBAI = CreateHuellTBai(request, middlewareStateDataES.LastReceipt),
            Factura = CreateFactura(request)
        };
        return ticketBaiRequest;
    }

    private static Factura CreateFactura(ProcessRequest request)
    {
        var (serieFactura, numFactura) = request.ReceiptResponse.GetNumSerieFacturaParts();
        var facturoa = new Factura
        {
            CabeceraFactura = new CabeceraFacturaType
            {
                SerieFactura = serieFactura, // QUESTION
                NumFactura = numFactura.ToString(),
                FechaExpedicionFactura = GetLocalTime(request.ReceiptResponse.ftReceiptMoment).ToString("dd-MM-yyyy"),
                HoraExpedicionFactura = GetLocalTime(request.ReceiptResponse.ftReceiptMoment).ToString("HH:mm:ss"),
                FacturaSimplificada = request.ReceiptRequest.ftReceiptCase.IsCase(ReceiptCase.PointOfSaleReceipt0x0001) ? SiNoType.S : SiNoType.N,
                FacturaEmitidaSustitucionSimplificada = SiNoType.N,
            },
            DatosFactura = new DatosFacturaType
            {
                //FechaOperacion = GetLocalTime(request.ReceiptResponse.ftReceiptMoment).ToString("dd-MM-yyyy"), //TODO: needs to be set if issuing the invoice was different from the actual date
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

    private static DateTime GetLocalTime(DateTime utcTime)
    {
        TimeZoneInfo spainZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
        DateTime spainLocal = TimeZoneInfo.ConvertTimeFromUtc(utcTime, spainZone);
        return spainLocal;
    }

    private HuellaTBAI CreateHuellTBai(ProcessRequest request, Receipt? lastReceipt)
    {
        var huellTbai = new HuellaTBAI
        {
            Software = _software,
            NumSerieDispositivo = request.ReceiptResponse.ftCashBoxIdentification
        };

        var anterior = lastReceipt?.Response?.GetNumSerieFacturaParts();
        var signatureValueFirmaFacturaAnterior = lastReceipt?.Response.ftSignatures?.First(x => x.ftSignatureType.Country() == "ES" && x.ftSignatureType.IsType(SignatureTypeES.Signature)).Data;
        var fechaExpedicionFacturaAnterior = lastReceipt?.Response.ftReceiptMoment;
        if (anterior != null && signatureValueFirmaFacturaAnterior != null && fechaExpedicionFacturaAnterior != null)
        {
            huellTbai.EncadenamientoFacturaAnterior = new EncadenamientoFacturaAnteriorType
            {
                SerieFacturaAnterior = anterior.Value.serieFactura,
                NumFacturaAnterior = anterior.Value.numFactura.ToString(),
                FechaExpedicionFacturaAnterior = fechaExpedicionFacturaAnterior.Value.ToString("dd-MM-yyyy"),
                SignatureValueFirmaFacturaAnterior = signatureValueFirmaFacturaAnterior.Substring(0, 100)
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
        // TOdo add discount handling


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