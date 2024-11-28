using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Web;
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

namespace fiskaltrust.Middleware.Localization.QueueES.Exports;

public class VeriFactuMapping
{
    private readonly MasterDataConfiguration _masterData;
    private readonly IMiddlewareQueueItemRepository _queueItemRepository;

    public VeriFactuMapping(MasterDataConfiguration masterData, IMiddlewareQueueItemRepository queueItemRepository)
    {
        _masterData = masterData;
        _queueItemRepository = queueItemRepository;
    }

    public async Task<RegFactuSistemaFacturacion> CreateRegFactuSistemaFacturacionAsync(IAsyncEnumerable<ftQueueItem> queueItems)
    {
        var cabecera = new CabeceraType
        {
            ObligadoEmision = new PersonaFisicaJuridicaESType
            {
                // "Name and company name of the person responsible for issuing the invoices."
                // Not sure how this needs to be formated. Maybe we'll need some extra fields in the master data?
                // Should this be the AccountName, or OutletName or sth from the Agencies?
                NombreRazon = _masterData.Account.AccountName,
                NIF = _masterData.Outlet.VatId
            },
        };

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

        return new RegFactuSistemaFacturacion
        {
            Cabecera = cabecera,
            RegistroFactura = registroFactura.ToArray()
        };
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

        using var rsa = RSA.Create();

        var request = new CertificateRequest("CN=SelfSignedCert", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, true));

        var notBefore = DateTimeOffset.UtcNow;
        var notAfter = notBefore.AddYears(1);

        var cert = request.CreateSelfSigned(notBefore, notAfter);

        return XmlHelpers.Deserialize<RegistroFacturacionAnulacionType>(XmlHelpers.SignXmlContentWithXades(XmlHelpers.GetXMLIncludingNamespace(registroFacturacionAnulacion, "sf", "RegistroFacturacionAlta"), cert))!;
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
            TipoFactura = receiptRequest.ftReceiptCase switch
            {
                // figure out which ones map to which ones
                _ => ClaveTipoFacturaType.F1,
            },
            ImporteRectificacion = new DesgloseRectificacionType
            {
                // Do we need rounding for all the the decimals or should we fail if it's not in the range?
                BaseRectificada = receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.Amount - chargeItem.GetVATAmount()).ToVeriFactuNumber(), // helper for tostring

                CuotaRectificada = receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.GetVATAmount()).ToVeriFactuNumber(),
                // CuotaRecargoRectificado = receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.GetVATAmount()).ToVeriFactuNumber()
            },
            Desglose = receiptRequest.cbChargeItems.Select(chargeItem => new DetalleType
            {
                BaseImponibleOimporteNoSujeto = (chargeItem.Amount - chargeItem.GetVATAmount()).ToVeriFactuNumber(),
                Item = chargeItem.ftChargeItemCase switch
                {
                    // figure out which ones map to which ones
                    _ => CalificacionOperacionType.S1
                    // _ => CalificacionOperacionType
                    // _ => OperacionExentaType
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
                // VatId of producing company. We don't have that right now.
                Item = "NIF-fiskaltrust",
                IdSistemaInformatico = "fiskaltrust.Middleware.Queue.AzureTableStorage", // or add cloudcashbox etc. like the launcher type? would be annoying ^^
                Version = "", // version
                NumeroInstalacion = receiptResponse.ftCashBoxIdentification,
            },
            FechaHoraHusoGenRegistro = receiptResponse.ftReceiptMoment,
            TipoHuella = TipoHuellaType.Item01,
        };

        registroFacturacionAlta.Huella = registroFacturacionAlta.GetHuella();

        using var rsa = RSA.Create();

        var request = new CertificateRequest("CN=SelfSignedCert", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, true));

        var notBefore = DateTimeOffset.UtcNow;
        var notAfter = notBefore.AddYears(1);

        var cert = request.CreateSelfSigned(notBefore, notAfter);

        return XmlHelpers.Deserialize<RegistroFacturacionAltaType>(XmlHelpers.SignXmlContentWithXades(XmlHelpers.GetXMLIncludingNamespace(registroFacturacionAlta, "sf", "RegistroFacturacionAlta"), cert))!;
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
    public static string Serialize(this RegistroFacturacionAltaType registroFacturacionAlta)
    {
        var serializer = new XmlSerializer(typeof(RegistroFacturacionAltaType));
        using var writer = new Utf8StringWriter();

        serializer.Serialize(writer, registroFacturacionAlta);

        return writer.ToString();
    }

    public static string Serialize(this RegFactuSistemaFacturacion registroFacturacionAlta)
    {
        var serializer = new XmlSerializer(typeof(RegFactuSistemaFacturacion));
        using var writer = new Utf8StringWriter();

        serializer.Serialize(writer, registroFacturacionAlta);

        return writer.ToString();
    }
}
