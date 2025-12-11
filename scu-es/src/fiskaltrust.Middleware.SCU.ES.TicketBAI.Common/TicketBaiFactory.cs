using System;
using System.Globalization;
using System.Linq;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.ifPOS.v2.es.Cases;
using fiskaltrust.Middleware.SCU.ES.Common.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;

#pragma warning disable IDE0052

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;

public class TicketBaiFactory
{
    private readonly TicketBaiSCUConfiguration _configuration;

    public TicketBaiFactory(TicketBaiSCUConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TicketBai ConvertTo(ProcessRequest request)
    {
        var ticketBaiRequest = new TicketBai
        {
            Cabecera = new Cabecera
            {
                IDVersionTBAI = IDVersionTicketBaiType.Item12
            },
            Sujetos = new Sujetos
            {
                Emisor = new Emisor
                {
                    NIF = _configuration.EmisorNif,
                    ApellidosNombreRazonSocial = _configuration.EmisorApellidosNombreRazonSocial
                },
                VariosDestinatarios = SiNoType.N, // this probably needs to be S in cases of multiple cbCustomers, but how can one invoice have multiple recipients?
                VariosDestinatariosSpecified = true,
                EmitidaPorTercerosODestinatario = GetEmissionType(),
                EmitidaPorTercerosODestinatarioSpecified = true
            },
            Factura = CreateFactura(request),
            HuellaTBAI = CreateHuellTBai(request)
        };
        AddCustomerInfoIfNecessary(request, ticketBaiRequest);
        return ticketBaiRequest;
    }

    // Currently we only support N (issuer issues) but we need support for T (third party issues) and D (recipient issues) in the future
    private static EmitidaPorTercerosType GetEmissionType() => EmitidaPorTercerosType.N;

    private static void AddCustomerInfoIfNecessary(ProcessRequest request, TicketBai ticketBaiRequest)
    {
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
            else
            {
                // Customer is not from Spain we need to add more details
                ticketBaiRequest.Sujetos.Destinatarios = 
                [
                    new IDDestinatario
                    {
                        Item = new IDOtro  
                        {
                            CodigoPais = CountryType2.EC,
                            CodigoPaisSpecified = true,
                            IDType = IDTypeType.Item06 // 06 is other, we could use support for other types too
                        },
                        ApellidosNombreRazonSocial = customer.CustomerName,
                        CodigoPostal = customer.CustomerZip,
                        Direccion = customer.CustomerStreet
                    }
                ];
            }
        }
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
                FacturaSimplificada = request.ReceiptRequest.ftReceiptCase.IsCase(ReceiptCase.PointOfSaleReceipt0x0001) | request.ReceiptRequest.ftReceiptCase.IsCase(ReceiptCase.UnknownReceipt0x0000) ? SiNoType.S : SiNoType.N,
                FacturaSimplificadaSpecified = true,
                FacturaEmitidaSustitucionSimplificada = SiNoType.N,
                FacturaEmitidaSustitucionSimplificadaSpecified = true
            },
            DatosFactura = new DatosFacturaType
            {
                DescripcionFactura = GetDescripcionFactura(request),
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

        // For the FechOperacion field we could use Moment of the ChargeItem. Right no we omit it
        // factura.DatosFactura.FechaOperacion = GetLocalTime(request.ReceiptRequest.cbReceiptMoment).ToString("dd-MM-yyyy");
        return factura;
    }

    private static string GetDescripcionFactura(ProcessRequest request) => request.ReceiptRequest.ftReceiptCase.IsCase(ReceiptCase.PointOfSaleReceipt0x0001) ? "Factura Simplificada" : "Factura";

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
            Software = new SoftwareFacturacionType
            {
                LicenciaTBAI = _configuration.SoftwareLicenciaTBAI,
                EntidadDesarrolladora = new EntidadDesarrolladoraType
                {
                    Item = _configuration.SoftwareNif
                },
                Nombre = _configuration.SoftwareName,
                Version = _configuration.SoftwareVersion
            },
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
        var vatRates = request.ReceiptRequest.cbChargeItems.GroupBy(x => x.VATRate);
        return [.. vatRates.Select(x => new DetalleIVAType
        {
            BaseImponible = x.Sum(x => x.Amount - (x.VATAmount ?? 0.0m)).ToString("0.00", CultureInfo.InvariantCulture),
            TipoImpositivo = x.Key.ToString("0.00", CultureInfo.InvariantCulture),
            CuotaImpuesto = x.Sum(x => x.VATAmount ?? 0.0m).ToString("0.00", CultureInfo.InvariantCulture)
        })];
    }

    private static IDDetalleFacturaType[] CreateFacturas(ProcessRequest request)
    {
        // TOdo add discount handling

        return [.. request.ReceiptRequest.cbChargeItems.Select(x => new IDDetalleFacturaType
        {
            DescripcionDetalle = CapText(x.Description, 250),
            Cantidad = x.Quantity.ToString("0.00", CultureInfo.InvariantCulture),
            ImporteUnitario = x.Amount == 0.0m ? "0.00" : ((x.Amount - (x.VATAmount ?? 0.0m)) / x.Quantity).ToString("0.00", CultureInfo.InvariantCulture),
            //Descuento = "0", TODO How should we handle discounts? is this a must have or can e ignore that
            ImporteTotal = x.Amount.ToString("0.00", CultureInfo.InvariantCulture)
        })];
    }

    private static string CapText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Length <= maxLength ? text : text[..maxLength];
    }
}