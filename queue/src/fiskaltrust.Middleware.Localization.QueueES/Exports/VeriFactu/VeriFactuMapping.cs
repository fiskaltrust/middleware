﻿using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueES.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueES.Helpers;
using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.ES.Helpers;
using fiskaltrust.Middleware.SCU.ES.Models;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.VisualBasic;
using Version = fiskaltrust.Middleware.SCU.ES.Models.Version;

namespace fiskaltrust.Middleware.Localization.QueueES.Exports;

public class VeriFactuMapping
{
    private readonly MasterDataConfiguration _masterData;
    private readonly IMiddlewareQueueItemRepository _queueItemRepository;
    private readonly X509Certificate2? _certificate;

    public VeriFactuMapping(MasterDataConfiguration masterData, IMiddlewareQueueItemRepository queueItemRepository, X509Certificate2? certificate = null)
    {
        _masterData = masterData;
        _queueItemRepository = queueItemRepository;
        _certificate = certificate;
    }

    public RegFactuSistemaFacturacion CreateRegFactuSistemaFacturacion(RegistroFacturacionAnulacion registroFacturacionAnulacion) => CreateRegFactuSistemaFacturacion(new RegistroFactura { Item = registroFacturacionAnulacion });
    public RegFactuSistemaFacturacion CreateRegFactuSistemaFacturacion(RegistroFacturacionAlta registroFacturacionAlta) => CreateRegFactuSistemaFacturacion(new RegistroFactura { Item = registroFacturacionAlta });
    public RegFactuSistemaFacturacion CreateRegFactuSistemaFacturacion(RegistroFactura registroFactura) => CreateRegFactuSistemaFacturacion([registroFactura]);
    public RegFactuSistemaFacturacion CreateRegFactuSistemaFacturacion(IEnumerable<RegistroFactura> registroFactura)
    {
        var cabecera = new Cabecera
        {
            ObligadoEmision = new PersonaFisicaJuridicaES
            {
                // "Name and company name of the person responsible for issuing the invoices."
                // Not sure how this needs to be formated. Maybe we'll need some extra fields in the master data?
                // Should this be the AccountName, or OutletName or sth from the Agencies?
                NombreRazon = _masterData.Account.AccountName,
                NIF = _masterData.Account.VatId
            },

        };

        return new RegFactuSistemaFacturacion
        {
            Cabecera = cabecera,
            RegistroFactura = registroFactura.ToArray()
        };
    }

    public async Task<RegFactuSistemaFacturacion> CreateRegFactuSistemaFacturacionAsync(IAsyncEnumerable<ftQueueItem> queueItems)
    {
        var registroFactura = new List<RegistroFactura>();
        ReceiptRequest? previousReceiptRequest = null;
        ReceiptResponse? previousReceiptResponse = null;

        await foreach (var queueItem in queueItems)
        {
            var receiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(queueItem.request)!;
            var receiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(queueItem.response)!;
            if (!(receiptResponse.ftSignatures.Any(x => x.ftSignatureType.IsType(SignatureTypeES.Huella)) && receiptResponse.ftSignatures.Any(x => x.ftSignatureType.IsType(SignatureTypeES.Url))))
            {
                continue;
            }
            if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
            {
                if (previousReceiptRequest is null || previousReceiptResponse is null)
                {
                    throw new Exception("There needs to be a previous receipt in the chain to perform a void");
                }
                registroFactura.Add(
                    new RegistroFactura
                    {
                        Item = await CreateRegistroFacturacionAnulacionAsync(receiptRequest, receiptResponse, previousReceiptRequest, previousReceiptResponse)
                    });
            }
            else
            {
                registroFactura.Add(
                    new RegistroFactura
                    {
                        Item = await CreateRegistroFacturacionAltaAsync(receiptRequest, receiptResponse, previousReceiptRequest, previousReceiptResponse)
                    });
            }

            previousReceiptRequest = receiptRequest;
            previousReceiptResponse = receiptResponse;
        }

        return CreateRegFactuSistemaFacturacion(registroFactura);
    }

    public async Task<RegistroFacturacionAnulacion> CreateRegistroFacturacionAnulacionAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, ReceiptRequest previousReceiptRequest, ReceiptResponse previousReceiptResponse)
    {
        if (receiptRequest.cbPreviousReceiptReference is null)
        {
            throw new Exception("cbPreviousReceiptReference is required for voiding a receipt.");
        }
        if (receiptRequest.cbPreviousReceiptReference.IsGroup)
        {
            throw new NotSupportedException("Grouping of receipts is not supported.");
        }
        var previousQueueItems = _queueItemRepository.GetByReceiptReferenceAsync(receiptRequest.cbPreviousReceiptReference.SingleValue);
        if (await previousQueueItems.IsEmptyAsync())
        {
            throw new Exception($"Receipt with cbReceiptReference {receiptRequest.cbPreviousReceiptReference.SingleValue} not found.");
        }

        var voidedQueueItem = await previousQueueItems.SingleOrDefaultAsync() ?? throw new Exception($"Multiple receipts with cbReceiptReference {receiptRequest.cbPreviousReceiptReference} found.");

        var voidedReceiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(voidedQueueItem.request)!;
        var voidedReceiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(voidedQueueItem.response)!;

        var registroFacturacionAnulacion = new RegistroFacturacionAnulacion
        {
            IDVersion = Version.Item10,
            IDFactura = new IDFacturaExpedidaBaja
            {

                IDEmisorFacturaAnulada = voidedReceiptResponse.ftSignatures.First(x => x.ftSignatureType.IsType(SignatureTypeES.NIF)).Data,
                NumSerieFacturaAnulada = voidedReceiptResponse.ftReceiptIdentification.Split('#')[1],
                FechaExpedicionFacturaAnulada = voidedReceiptRequest.cbReceiptMoment.ToString("dd-MM-yyy")
            },
            Encadenamiento = new RegistroFacturacionAnulacionEncadenamiento
            {
                Item = await GetEncadenamientoFacturaAnteriorAsync(previousReceiptRequest, previousReceiptResponse)
            },
            // Which PosSystem from the list should we take? In DE we just take the first one...
            // Is this fiskaltrust or the dealer/creator
            SistemaInformatico = new SistemaInformatico
            {
                NombreRazon = "Thomas Steininger", // add real name here... and maybe get that from the config
                NombreSistemaInformatico = "fiskaltrust.Middleware",
                // Identification code given by the producing person or entity to its computerised invoicing system (RIS) which, once installed, constitutes the RIS used.
                // It should distinguish it from any other possible different RIS produced by the same producing person or entity.
                // The possible restrictions to its values shall be detailed in the corresponding documentation in the AEAT electronic office (validations document...).
                IdSistemaInformatico = "00", // alphanumeric(2)
                // VatId of producing company. We don't have that right now.
                Item = "M0291081Q",
                Version = "1.0.0", // version
                NumeroInstalacion = receiptResponse.ftCashBoxIdentification,
                TipoUsoPosibleSoloVerifactu = Booleano.N,
                TipoUsoPosibleMultiOT = Booleano.N,
                IndicadorMultiplesOT = Booleano.N
            },
            FechaHoraHusoGenRegistro = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(new DateTimeOffset(receiptResponse.ftReceiptMoment, TimeSpan.Zero), "Europe/Madrid"),
            TipoHuella = TipoHuella.Item01,
            Huella = null!
        };

        registroFacturacionAnulacion.Huella = registroFacturacionAnulacion.GetHuella();

        return _certificate is null ? registroFacturacionAnulacion : XmlHelpers.Deserialize<RegistroFacturacionAnulacion>(XmlHelpers.SignXmlContentWithXades(XmlHelpers.GetXMLIncludingNamespace(registroFacturacionAnulacion, "sf", "RegistroFacturacionAlta"), _certificate))!;
    }

    public async Task<RegistroFacturacionAlta> CreateRegistroFacturacionAltaAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, ReceiptRequest? previousReceiptRequest, ReceiptResponse? previousReceiptResponse)
    {
        var registroFacturacionAlta = new RegistroFacturacionAlta
        {
            IDVersion = Version.Item10,
            IDFactura = new IDFactura
            {
                IDEmisorFactura = _masterData.Account.VatId,
                NumSerieFactura = receiptResponse.ftReceiptIdentification.Split('#')[1],
                FechaExpedicionFactura = receiptRequest.cbReceiptMoment.ToString("dd-MM-yyy")
            },

            // "Name and business name of the person required to issue the invoice."
            // Not sure how this needs to be formated. Maybe we'll need some extra fields in the master data?
            // Should this be the AccountName, or OutletName or sth from the Agencies?
            NombreRazonEmisor = _masterData.Account.AccountName,
            TipoFactura = receiptRequest.ftReceiptCase.GetType() switch
            {
                _ => ClaveTipoFactura.F2, // QUESTION: is simplified invoice correct?
                // _ => throw new Exception($"Invalid receipt case {receiptRequest.ftReceiptCase}")
            },
            DescripcionOperacion = "test", // TODO: add descrpiton?,
            // FacturaSinIdentifDestinatarioArt61d = Booleano.S, // TODO: do we need this art. 61d?
            Desglose = receiptRequest.cbChargeItems.Select(chargeItem => new Detalle
            {
                // 01 Value added tax (VAT)
                // 02 Tax on Production, Services and Imports (IPSI) for Ceuta and Melilla
                // 03 Canary Islands Indirect General Tax (IGIC)
                // 05 Other
                Impuesto = Impuesto.Item01,
                // we'll have to check these in detail
                ClaveRegimen = IdOperacionesTrascendenciaTributaria.Item01,
                BaseImponibleOimporteNoSujeto = (chargeItem.Amount - chargeItem.GetVATAmount()).ToVeriFactuNumber(),
                Item = chargeItem.ftChargeItemCase.NatureOfVat() switch
                {
                    ChargeItemCaseNatureOfVatES.UsualVatApplies => CalificacionOperacion.S1, // If CalificacionOperacion is S1 and BaseImponibleACoste is not filled in, TipoImpositivo and CuotaRepercutida are mandatory.

                    ChargeItemCaseNatureOfVatES.NotSubjectArticle7and14 => CalificacionOperacion.N1, // TODO: Document
                    ChargeItemCaseNatureOfVatES.NotSubjectLocationRules => CalificacionOperacion.N2, // TODO: Document

                    ChargeItemCaseNatureOfVatES.ExteptArticle20 => OperacionExenta.E1, // TODO: Document
                    ChargeItemCaseNatureOfVatES.ExteptArticle21 => OperacionExenta.E2, // TODO: Document
                    ChargeItemCaseNatureOfVatES.ExteptArticle22 => OperacionExenta.E3, // TODO: Document
                    ChargeItemCaseNatureOfVatES.ExteptArticle23And24 => OperacionExenta.E4, // TODO: Document
                    ChargeItemCaseNatureOfVatES.ExteptArticle25 => OperacionExenta.E5, // TODO: Document
                    ChargeItemCaseNatureOfVatES.ExteptOthers => OperacionExenta.E6, // TODO: Document

                    ChargeItemCaseNatureOfVatES.ReverseCharge => CalificacionOperacion.S2,
                    _ => throw new Exception($"Invalid charge item case {chargeItem.ftChargeItemCase}")
                },
                TipoImpositivo = chargeItem.VATRate.ToVeriFactuNumber(),
                CuotaRepercutida = (chargeItem.VATAmount ?? chargeItem.Amount * chargeItem.VATRate).ToVeriFactuNumber()
            }).ToArray(),
            CuotaTotal = receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.GetVATAmount()).ToVeriFactuNumber(),
            ImporteTotal = (receiptRequest.cbReceiptAmount ?? receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.Amount)).ToVeriFactuNumber(),
            Encadenamiento = new RegistroFacturacionAltaEncadenamiento
            {
                Item = await GetEncadenamientoFacturaAnteriorAsync(previousReceiptRequest, previousReceiptResponse)
            },
            // Which PosSystem from the list should we take? In DE we just take the first one...
            // Is this fiskaltrust or the dealer/creator
            SistemaInformatico = new SistemaInformatico
            {
                NombreRazon = "Thomas Steininger", // add real name here... and maybe get that from the config
                NombreSistemaInformatico = "fiskaltrust.Middleware",
                // Identification code given by the producing person or entity to its computerised invoicing system (RIS) which, once installed, constitutes the RIS used.
                // It should distinguish it from any other possible different RIS produced by the same producing person or entity.
                // The possible restrictions to its values shall be detailed in the corresponding documentation in the AEAT electronic office (validations document...).
                IdSistemaInformatico = "00", // alphanumeric(2)
                // VatId of producing company. We don't have that right now.
                Item = "M0291081Q",
                Version = "1.0.0", // version
                NumeroInstalacion = receiptResponse.ftCashBoxIdentification,
                TipoUsoPosibleSoloVerifactu = Booleano.N,
                TipoUsoPosibleMultiOT = Booleano.N,
                IndicadorMultiplesOT = Booleano.N
            },
            FechaHoraHusoGenRegistro = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(new DateTimeOffset(receiptResponse.ftReceiptMoment, TimeSpan.Zero), "Europe/Madrid"),
            TipoHuella = TipoHuella.Item01,
            Huella = null!
        };

        registroFacturacionAlta.Huella = registroFacturacionAlta.GetHuella();

        return _certificate is null ? registroFacturacionAlta : XmlHelpers.Deserialize<RegistroFacturacionAlta>(XmlHelpers.SignXmlContentWithXades(XmlHelpers.GetXMLIncludingNamespace(registroFacturacionAlta, "sf", "RegistroFacturacionAlta"), _certificate))!;
    }

    private async Task<object> GetEncadenamientoFacturaAnteriorAsync(ReceiptRequest? previousReceiptRequest, ReceiptResponse? previousReceiptResponse)
    {
        if (previousReceiptRequest is null || previousReceiptResponse is null)
        {
            return PrimerRegistroCadena.S;
        }



        var previousHash = previousReceiptResponse.ftSignatures.First(x => x.ftSignatureType.IsType(SignatureTypeES.Huella)).Data;

        if (previousReceiptRequest is not null && previousReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            if (previousReceiptRequest.cbPreviousReceiptReference is null)
            {
                throw new Exception("cbPreviousReceiptReference is required for voiding a receipt.");
            }
            if (previousReceiptRequest.cbPreviousReceiptReference.IsGroup)
            {
                throw new NotSupportedException("Grouping of receipts is not supported.");
            }
            var previousQueueItems = _queueItemRepository.GetByReceiptReferenceAsync(previousReceiptRequest.cbPreviousReceiptReference.SingleValue);
            if (await previousQueueItems.IsEmptyAsync())
            {
                throw new Exception($"Receipt with cbReceiptReference {previousReceiptRequest.cbPreviousReceiptReference.SingleValue} not found.");
            }

            var voidedQueueItem = await previousQueueItems.SingleOrDefaultAsync() ?? throw new Exception($"Multiple receipts with cbReceiptReference {previousReceiptRequest.cbPreviousReceiptReference} found.");

            var voidedReceiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(voidedQueueItem.request)!;
            var voidedReceiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(voidedQueueItem.response)!;

            return new EncadenamientoFacturaAnterior
            {
                IDEmisorFactura = voidedReceiptResponse.ftSignatures.First(x => x.ftSignatureType.IsType(SignatureTypeES.NIF)).Data,
                NumSerieFactura = voidedReceiptResponse.ftReceiptIdentification.Split('#')[1],
                FechaExpedicionFactura = voidedReceiptRequest!.cbReceiptMoment.ToString("dd-MM-yyy"),
                Huella = previousReceiptResponse.ftSignatures.First(x => x.ftSignatureType.IsType(SignatureTypeES.Huella)).Data
            };
        }
        else
        {
            return new EncadenamientoFacturaAnterior
            {
                IDEmisorFactura = previousReceiptResponse.ftSignatures.First(x => x.ftSignatureType.IsType(SignatureTypeES.NIF)).Data,
                NumSerieFactura = previousReceiptResponse.ftReceiptIdentification.Split('#')[1],
                FechaExpedicionFactura = previousReceiptRequest!.cbReceiptMoment.ToString("dd-MM-yyy"),
                Huella = previousHash
            };
        }
    }
}

public static class HuellaExt
{
    public static string GetHuella(this RegistroFacturacionAlta registroFacturacionAlta)
        => registroFacturacionAlta.GetHuella(new List<(string key, Func<RegistroFacturacionAlta, string> value)> {
            ("IDEmisorFactura", x => x.IDFactura.IDEmisorFactura),
            ("NumSerieFactura", x => x.IDFactura.NumSerieFactura),
            ("FechaExpedicionFactura", x => x.IDFactura.FechaExpedicionFactura),
            ("TipoFactura", x => Enum.GetName(x.TipoFactura)!),
            ("CuotaTotal", x => x.CuotaTotal),
            ("ImporteTotal", x => x.ImporteTotal),
            ("Huella", x => x.Encadenamiento.Item is EncadenamientoFacturaAnterior encadenamiento ? encadenamiento.Huella : ""),
            ("FechaHoraHusoGenRegistro", x => x.FechaHoraHusoGenRegistro.ToString("yyyy-MM-ddTHH:mm:sszzz")),
        });

    public static string GetHuella(this RegistroFacturacionAnulacion registroFacturacionAnulacion)
        => registroFacturacionAnulacion.GetHuella(new List<(string key, Func<RegistroFacturacionAnulacion, string> value)> {
            ("IDEmisorFacturaAnulada", x => x.IDFactura.IDEmisorFacturaAnulada),
            ("NumSerieFacturaAnulada", x => x.IDFactura.NumSerieFacturaAnulada),
            ("FechaExpedicionFacturaAnulada", x => x.IDFactura.FechaExpedicionFacturaAnulada),
            ("Huella", x => x.Encadenamiento.Item is EncadenamientoFacturaAnterior encadenamiento ? encadenamiento.Huella : ""),
            ("FechaHoraHusoGenRegistro", x => x.FechaHoraHusoGenRegistro.ToString("yyyy-MM-ddTHH:mm:sszzz")),
        });


    private static string GetHuella<T>(this T self, List<(string key, Func<T, string> value)> selectors)
    {
        var data = new StringBuilder();
        foreach (var (n, (key, value)) in selectors.Select((x, i) => (i + 1, x)))
        {
            data.AppendFormat(GetValue(key, value(self), false, selectors.Count != n));
        }
        var stringData = data.ToString();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(stringData));

        return Convert.ToHexString(hash);
    }

    private static string GetValue(string key, string value, bool encoded = false, bool separator = true)
        => key + "=" + (encoded ? HttpUtility.UrlEncode(value.Trim()) : value.Trim()) + (separator ? "&" : "");
}

public static class XmlExt
{
    public static string XmlSerialize<T>(this T registroFacturacionAlta)
    {
        var serializer = new XmlSerializer(typeof(T));

        using var writer = new Utf8StringWriter();

        serializer.Serialize(writer, registroFacturacionAlta);

        return writer.ToString();
    }
}
