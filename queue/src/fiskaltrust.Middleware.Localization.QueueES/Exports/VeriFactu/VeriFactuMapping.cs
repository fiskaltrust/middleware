using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueES.Helpers;
using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.SCU.ES.Helpers;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.VisualBasic;

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

    public RegFactuSistemaFacturacion CreateRegFactuSistemaFacturacion(RegistroFacturacionAnulacionType registroFacturacionAnulacion) => CreateRegFactuSistemaFacturacion(new RegistroFacturaType { Item = registroFacturacionAnulacion });
    public RegFactuSistemaFacturacion CreateRegFactuSistemaFacturacion(RegistroFacturacionAltaType registroFacturacionAlta) => CreateRegFactuSistemaFacturacion(new RegistroFacturaType { Item = registroFacturacionAlta });
    public RegFactuSistemaFacturacion CreateRegFactuSistemaFacturacion(RegistroFacturaType registroFactura) => CreateRegFactuSistemaFacturacion([registroFactura]);
    public RegFactuSistemaFacturacion CreateRegFactuSistemaFacturacion(IEnumerable<RegistroFacturaType> registroFactura)
    {
        var cabecera = new CabeceraType
        {
            ObligadoEmision = new PersonaFisicaJuridicaESType
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
        var registroFactura = new List<RegistroFacturaType>();
        ReceiptRequest? previousReceiptRequest = null;
        ReceiptResponse? previousReceiptResponse = null;

        await foreach (var queueItem in queueItems)
        {
            var receiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(queueItem.request)!;
            var receiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(queueItem.response)!;
            if (receiptRequest.IsVoid())
            {
                registroFactura.Add(
                    new RegistroFacturaType
                    {
                        Item = await CreateRegistroFacturacionAnulacion(receiptRequest, receiptResponse, previousReceiptRequest is null || previousReceiptResponse is null ? null : (new IDFacturaExpedidaType
                        {
                            IDEmisorFactura = previousReceiptResponse.ftSignatures.First(x => x.ftSignatureType == (long) SignatureTypesES.IDEmisorFactura).Data,
                            NumSerieFactura = previousReceiptResponse.ftReceiptIdentification,
                            FechaExpedicionFactura = previousReceiptRequest.cbReceiptMoment.ToString("dd-MM-yyy")
                        },
                            previousReceiptResponse.ftSignatures.First(x => x.ftSignatureType == (long) SignatureTypesES.Huella).Data
                        ))
                    });
            }
            else
            {
                registroFactura.Add(
                    new RegistroFacturaType
                    {
                        Item = CreateRegistroFacturacionAlta(receiptRequest, receiptResponse, previousReceiptRequest is null || previousReceiptResponse is null ? null : (new IDFacturaExpedidaType
                        {
                            IDEmisorFactura = previousReceiptResponse.ftSignatures.First(x => x.ftSignatureType == (long) SignatureTypesES.IDEmisorFactura).Data,
                            NumSerieFactura = previousReceiptResponse.ftReceiptIdentification,
                            FechaExpedicionFactura = previousReceiptRequest.cbReceiptMoment.ToString("dd-MM-yyy")
                        },
                            previousReceiptResponse.ftSignatures.First(x => x.ftSignatureType == (long) SignatureTypesES.Huella).Data
                        ))
                    });
            }

            previousReceiptRequest = receiptRequest;
            previousReceiptResponse = receiptResponse;
        }

        return CreateRegFactuSistemaFacturacion(registroFactura);
    }

    public async Task<RegistroFacturacionAnulacionType> CreateRegistroFacturacionAnulacion(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, (IDFacturaExpedidaType id, string hash)? previous)
    {
        var previousQueueItems = _queueItemRepository.GetByReceiptReferenceAsync(receiptRequest.cbPreviousReceiptReference);
        if (await previousQueueItems.IsEmptyAsync())
        {
            throw new Exception($"Receipt with cbReceiptReference {receiptRequest.cbPreviousReceiptReference} not found.");
        }

        var voidedQueueItem = await previousQueueItems.SingleOrDefaultAsync() ?? throw new Exception($"Multiple receipts with cbReceiptReference {receiptRequest.cbPreviousReceiptReference} found.");

        var voidedReceiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(voidedQueueItem.request)!;
        var voidedReceiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(voidedQueueItem.response)!;

        var registroFacturacionAnulacion = new RegistroFacturacionAnulacionType
        {
            IDVersion = VersionType.Item10,
            IDFactura = new IDFacturaExpedidaBajaType
            {

                IDEmisorFacturaAnulada = voidedReceiptResponse.ftSignatures.First(x => x.ftSignatureType == (long) SignatureTypesES.IDEmisorFactura).Data,
                NumSerieFacturaAnulada = voidedReceiptResponse.ftReceiptIdentification.Split('#')[1],
                FechaExpedicionFacturaAnulada = voidedReceiptRequest.cbReceiptMoment.ToString("dd-MM-yyy")
            },
            Encadenamiento = new RegistroFacturacionAnulacionTypeEncadenamiento
            {
                Item = receiptRequest.IsInitialOperation()
                    ? PrimerRegistroCadenaType.S
                    : new EncadenamientoFacturaAnteriorType
                    {
                        IDEmisorFactura = previous!.Value.id.IDEmisorFactura,
                        NumSerieFactura = previous!.Value.id.NumSerieFactura,
                        FechaExpedicionFactura = previous!.Value.id.FechaExpedicionFactura,
                        Huella = previous!.Value.hash
                    }
            },
            // Which PosSystem from the list should we take? In DE we just take the first one...
            // Is this fiskaltrust or the dealer/creator
            SistemaInformatico = new SistemaInformaticoType
            {
                NombreRazon = "fiskaltrust", // add real name here... and maybe get that from the config
                // VatId of producing company. We don't have that right now.
                Item = "NIF-fiskaltrust",
                IdSistemaInformatico = "fiskaltrust.Middleware.Queue.AzureTableStorage", // or add cloudcashbox etc. like the launcher type? would be annoying ^^
                Version = "", // version
                NumeroInstalacion = receiptResponse.ftCashBoxIdentification,
            },
            FechaHoraHusoGenRegistro = receiptResponse.ftReceiptMoment,
            TipoHuella = TipoHuellaType.Item01,
        };

        registroFacturacionAnulacion.Huella = registroFacturacionAnulacion.GetHuella();

        return _certificate is null ? registroFacturacionAnulacion : XmlHelpers.Deserialize<RegistroFacturacionAnulacionType>(XmlHelpers.SignXmlContentWithXades(XmlHelpers.GetXMLIncludingNamespace(registroFacturacionAnulacion, "sf", "RegistroFacturacionAlta"), _certificate))!;
    }

    public RegistroFacturacionAltaType CreateRegistroFacturacionAlta(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, (IDFacturaExpedidaType id, string hash)? previous)
    {
        var registroFacturacionAlta = new RegistroFacturacionAltaType
        {
            IDVersion = VersionType.Item10,
            IDFactura = new IDFacturaExpedidaType
            {
                IDEmisorFactura = _masterData.Outlet.VatId,
                NumSerieFactura = receiptResponse.ftReceiptIdentification.Split('#')[1],
                FechaExpedicionFactura = receiptRequest.cbReceiptMoment.ToString("dd-MM-yyy")
            },

            // "Name and business name of the person required to issue the invoice."
            // Not sure how this needs to be formated. Maybe we'll need some extra fields in the master data?
            // Should this be the AccountName, or OutletName or sth from the Agencies?
            NombreRazonEmisor = _masterData.Account.AccountName,
            TipoFactura = (receiptRequest.ftReceiptCase & 0xF000) switch
            {
                _ => ClaveTipoFacturaType.F2, // QUESTION: is simplified invoice correct?
                // _ => throw new Exception($"Invalid receipt case {receiptRequest.ftReceiptCase}")
            },
            DescripcionOperacion = "test", // TODO: add descrpiton?
            ImporteRectificacion = new DesgloseRectificacionType
            {
                // Do we need rounding for all the the decimals or should we fail if it's not in the range?
                BaseRectificada = receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.Amount - chargeItem.GetVATAmount()).ToVeriFactuNumber(), // helper for tostring

                CuotaRectificada = receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.GetVATAmount()).ToVeriFactuNumber(),
                // CuotaRecargoRectificado = receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.GetVATAmount()).ToVeriFactuNumber(),
            },
            Desglose = receiptRequest.cbChargeItems.Select(chargeItem => new DetalleType
            {
                BaseImponibleOimporteNoSujeto = (chargeItem.Amount - chargeItem.GetVATAmount()).ToVeriFactuNumber(),
                Item = (chargeItem.ftChargeItemCase & 0xFF00) switch
                {
                    2 => (chargeItem.ftChargeItemCase & 0x0F00) switch
                    {
                        0 => CalificacionOperacionType.N1, // TODO: Document
                        1 => CalificacionOperacionType.N2, // TODO: Document
                        _ => throw new Exception($"Invalid charge item case {chargeItem.ftChargeItemCase}")
                    },
                    3 => (chargeItem.ftChargeItemCase & 0x0F00) switch
                    {
                        0 => OperacionExentaType.E1, // TODO: Document
                        1 => OperacionExentaType.E2, // TODO: Document
                        2 => OperacionExentaType.E3, // TODO: Document
                        3 => OperacionExentaType.E4, // TODO: Document
                        4 => OperacionExentaType.E5, // TODO: Document
                        5 => OperacionExentaType.E6, // TODO: Document
                        _ => throw new Exception($"Invalid charge item case {chargeItem.ftChargeItemCase}")
                    },
                    5 => CalificacionOperacionType.S2,
                    _ => CalificacionOperacionType.S1
                }
            }).ToArray(),
            CuotaTotal = receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.GetVATAmount()).ToVeriFactuNumber(),
            ImporteTotal = (receiptRequest.cbReceiptAmount ?? receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.Amount)).ToVeriFactuNumber(),
            Encadenamiento = new RegistroFacturacionAltaTypeEncadenamiento
            {
                Item = receiptRequest.IsInitialOperation()
                    ? PrimerRegistroCadenaType.S
                    : new EncadenamientoFacturaAnteriorType
                    {
                        IDEmisorFactura = previous!.Value.id.IDEmisorFactura,
                        NumSerieFactura = previous!.Value.id.NumSerieFactura,
                        FechaExpedicionFactura = previous!.Value.id.FechaExpedicionFactura,
                        Huella = previous!.Value.hash
                    }
            },
            // Which PosSystem from the list should we take? In DE we just take the first one...
            // Is this fiskaltrust or the dealer/creator
            SistemaInformatico = new SistemaInformaticoType
            {
                NombreRazon = "fiskaltrust", // add real name here... and maybe get that from the config
                NombreSistemaInformatico = "fiskaltrust.Middleware",
                // Identification code given by the producing person or entity to its computerised invoicing system (RIS) which, once installed, constitutes the RIS used.
                // It should distinguish it from any other possible different RIS produced by the same producing person or entity.
                // The possible restrictions to its values shall be detailed in the corresponding documentation in the AEAT electronic office (validations document...).
                IdSistemaInformatico = "00", // alphanumeric(2)
                // VatId of producing company. We don't have that right now.
                Item = "M0291081Q",
                Version = "1.0.0", // version
                NumeroInstalacion = receiptResponse.ftCashBoxIdentification,
            },
            FechaHoraHusoGenRegistro = receiptResponse.ftReceiptMoment,
            TipoHuella = TipoHuellaType.Item01,
        };

        registroFacturacionAlta.Huella = registroFacturacionAlta.GetHuella();

        return _certificate is null ? registroFacturacionAlta : XmlHelpers.Deserialize<RegistroFacturacionAltaType>(XmlHelpers.SignXmlContentWithXades(XmlHelpers.GetXMLIncludingNamespace(registroFacturacionAlta, "sf", "RegistroFacturacionAlta"), _certificate))!;
    }
}

public static class HuellaExt
{
    public static string GetHuella(this RegistroFacturacionAltaType registroFacturacionAlta)
        => registroFacturacionAlta.GetHuella(new List<(string key, Func<RegistroFacturacionAltaType, string> value)> {
            ("IDEmisorFactura", x => x.IDFactura.IDEmisorFactura),
            ("NumSerieFactura", x => x.IDFactura.NumSerieFactura),
            ("FechaExpedicionFactura", x => x.IDFactura.FechaExpedicionFactura),
            ("TipoFactura", x => Enum.GetName(x.TipoFactura)!),
            ("CuotaTotal", x => x.CuotaTotal),
            ("ImporteTotal", x => x.ImporteTotal),
            ("Huella", x => x.Encadenamiento.Item is EncadenamientoFacturaAnteriorType encadenamiento ? encadenamiento.Huella : "S"),
            ("FechaHoraHusoGenRegistro", x => x.FechaHoraHusoGenRegistro.ToString("yyyy-MM-ddThh:mm:sszzz")),
        });

    public static string GetHuella(this RegistroFacturacionAnulacionType registroFacturacionAnulacion)
        => registroFacturacionAnulacion.GetHuella(new List<(string key, Func<RegistroFacturacionAnulacionType, string> value)> {
            ("IDEmisorFacturaAnulada", x => x.IDFactura.IDEmisorFacturaAnulada),
            ("NumSerieFacturaAnulada", x => x.IDFactura.NumSerieFacturaAnulada),
            ("FechaExpedicionFacturaAnulada", x => x.IDFactura.FechaExpedicionFacturaAnulada),
            ("Huella", x => x.Encadenamiento.Item is EncadenamientoFacturaAnteriorType encadenamiento ? encadenamiento.Huella : "S"),
            ("FechaHoraHusoGenRegistro", x => x.FechaHoraHusoGenRegistro.ToString("yyyy-MM-ddThh:mm:sszzz")),
        });


    private static string GetHuella<T>(this T self, List<(string key, Func<T, string> value)> selectors)
    {
        var data = new StringBuilder();
        foreach (var (n, (key, value)) in selectors.Select((x, i) => (i + 1, x)))
        {
            data.AppendFormat(GetValue(key, value(self), selectors.Count == n));
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data.ToString()));

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
