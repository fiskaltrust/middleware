using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.ifPOS.v2.es.Cases;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Models;

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
            VariosDestinatariosSpecified = true,
            EmitidaPorTercerosODestinatario = EmitidaPorTercerosType.N,
            EmitidaPorTercerosODestinatarioSpecified = true
        };

        _cabacera = new Cabecera
        {
            IDVersionTBAI = IDVersionTicketBaiType.Item12
        };
        _software = new SoftwareFacturacionType
        {
            LicenciaTBAI = configuration.SoftwareLicenciaTBAI,
            EntidadDesarrolladora = new EntidadDesarrolladoraType
            {
                Item = configuration.SoftwareNif
            },
            Nombre = configuration.SoftwareName,
            Version = configuration.SoftwareVersion
        };
    }

    public TicketBai ConvertTo(ProcessRequest request)
    {
        var ticketBaiRequest = new TicketBai
        {
            Cabecera = _cabacera,
            Sujetos = _sujetos,
            HuellaTBAI = CreateHuellTBai(request),
            Factura = CreateFactura(request)
        };

        if (request.ReceiptRequest.ContainsCustomerInfo())
        {
            var customer = request.ReceiptRequest.GetCustomerOrNull()!;
            if (!string.IsNullOrEmpty(customer.CustomerVATId))
            {
                // Currently we only set if we get a NIF
                ticketBaiRequest.Sujetos.Destinatarios = [
                    new IDDestinatario
                    {
                        Item = customer.CustomerVATId,
                        ApellidosNombreRazonSocial = customer.CustomerName,
                        CodigoPostal = customer.CustomerZip,
                        Direccion = customer.CustomerStreet
                    }
                ];
            }

            if (!string.IsNullOrEmpty(customer.CustomerCountry) || customer.CustomerCountry != "ES")
            {

                // Customer is not from Spain we need to add more details
                ticketBaiRequest.Sujetos.Destinatarios = [
                    new IDDestinatario
                    {
                        Item = customer.CustomerVATId,
                        ApellidosNombreRazonSocial = customer.CustomerName,
                        CodigoPostal = customer.CustomerZip,
                        Direccion = customer.CustomerStreet
                    }
                ];
            }
        }
        return ticketBaiRequest;
    }


    private static Factura CreateFactura(ProcessRequest request)
    {
        var (serieFactura, numFactura) = request.ReceiptResponse.GetNumSerieFacturaParts();
        var factura = new Factura
        {
            CabeceraFactura = new CabeceraFacturaType
            {
                SerieFactura = serieFactura, // QUESTION
                NumFactura = numFactura.ToString(),
                FechaExpedicionFactura = GetLocalTime(request.ReceiptRequest.cbReceiptMoment).ToString("dd-MM-yyyy"),
                HoraExpedicionFactura = GetLocalTime(request.ReceiptRequest.cbReceiptMoment).ToString("HH:mm:ss"),
                FacturaSimplificada = request.ReceiptRequest.ftReceiptCase.IsCase(ReceiptCase.PointOfSaleReceipt0x0001) ? SiNoType.S : SiNoType.N,
                FacturaSimplificadaSpecified = true,
                FacturaEmitidaSustitucionSimplificada = SiNoType.N,
                FacturaEmitidaSustitucionSimplificadaSpecified = true
            },
            DatosFactura = new DatosFacturaType
            {
                DetallesFactura = CreateFacturas(request),
                ImporteTotalFactura = request.ReceiptRequest.cbChargeItems.Sum(x => x.Amount).ToString("0.00", CultureInfo.InvariantCulture),
                Claves =
                [
                    new IDClaveType
                    {
                        ClaveRegimenIvaOpTrascendencia = IdOperacionesTrascendenciaTributariaType.Item01
                    }
                ]
            },
            TipoDesglose = new TipoDesgloseType
            {
                Item = new DesgloseFacturaType
                {
                    Sujeta = new SujetaType
                    {
                        NoExenta =
                            [
                                new DetalleNoExentaType
                                {
                                    TipoNoExenta = TipoOperacionSujetaNoExentaType.S1,
                                    DesgloseIVA = CreateDetalleIVAType(request)
                                }
                            ]
                    }
                }
            }
        };

        // The DescripcionFactura depends on the type of receipt. Probably we will need to consider the used ChargeItems and the type of payment too
        if (request.ReceiptRequest.ftReceiptCase.IsCase(ReceiptCase.PointOfSaleReceipt0x0001))
        {
            factura.DatosFactura.DescripcionFactura = "Factura Simplificada";
        }
        else
        {
            factura.DatosFactura.DescripcionFactura = "Factura";
        }

        // For the FechOperacion field we could use Moment of the ChargeItem. Right no we omit it
        // factura.DatosFactura.FechaOperacion = GetLocalTime(request.ReceiptRequest.cbReceiptMoment).ToString("dd-MM-yyyy");
        return factura;
    }

    private static DateTime GetLocalTime(DateTime utcTime)
    {
        TimeZoneInfo spainZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
        DateTime spainLocal = TimeZoneInfo.ConvertTimeFromUtc(utcTime, spainZone);
        return spainLocal;
    }

    private HuellaTBAI CreateHuellTBai(ProcessRequest request)
    {
        var huellTbai = new HuellaTBAI
        {
            Software = _software,
            NumSerieDispositivo = request.ReceiptResponse.ftCashBoxIdentification
        };


        var middlewareStateData = MiddlewareStateData.FromReceiptResponse(request.ReceiptResponse);
        if (middlewareStateData is not null && middlewareStateData.ES is not null && middlewareStateData.ES.LastReceipt is not null)
        {
            var lastReceipt = middlewareStateData.ES.LastReceipt;
            var anterior = lastReceipt?.Response?.GetNumSerieFacturaParts();
            var signatureValueFirmaFacturaAnterior = lastReceipt?.Response.ftSignatures?.FirstOrDefault(x => x.ftSignatureType.Country() == "ES" && x.ftSignatureType.IsType(SignatureTypeES.Signature))?.Data;
            if (signatureValueFirmaFacturaAnterior != null)
            {
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
            }
        }

        return huellTbai;
    }

    private static DetalleIVAType[] CreateDetalleIVAType(ProcessRequest request)
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
            OperacionEnRecargoDeEquivalenciaORegimenSimplificado = SiNoType.N,
            OperacionEnRecargoDeEquivalenciaORegimenSimplificadoSpecified = true
        }).ToArray();
    }

    private static IDDetalleFacturaType[] CreateFacturas(ProcessRequest request)
    {
        var chargeItems = request.ReceiptRequest.cbChargeItems.Select(c =>
        {
            c.VATAmount = c.VATAmount ?? (c.Amount * c.VATRate / 100.0m);
            return c;
        });
        // TOdo add discount handling


        return chargeItems.Select(x => new IDDetalleFacturaType
        {
            DescripcionDetalle = CapText(x.Description, 250),
            Cantidad = x.Quantity.ToString("0.00", CultureInfo.InvariantCulture),
            ImporteUnitario = (x.Amount - x.VATAmount!.Value).ToString("0.00", CultureInfo.InvariantCulture),
            //Descuento = "0", TODO How should we handle discounts? is this a must have or can e ignore that
            ImporteTotal = x.Amount.ToString("0.00", CultureInfo.InvariantCulture)
        }).ToArray();
    }

    private static string CapText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (text.Length <= maxLength)
            return text;

        return text[..maxLength];
    }
}