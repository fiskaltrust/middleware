using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es.Cases;
using fiskaltrust.Middleware.SCU.ES.VeriFactu.Helpers;
using fiskaltrust.Middleware.SCU.ES.VeriFactu.Mapping;
using fiskaltrust.Middleware.SCU.ES.VeriFactu.Models;
using Version = fiskaltrust.Middleware.SCU.ES.VeriFactu.Models.Version;
// Use the VeriFactu-owned, NN-byte-correct nature enum rather than the (wrong) interface enum of the
// same name imported via fiskaltrust.ifPOS.v2.es.Cases.
using ChargeItemCaseNatureOfVatES = fiskaltrust.Middleware.SCU.ES.VeriFactu.Mapping.ChargeItemCaseNatureOfVatES;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu;

public class VeriFactuMapping
{
    private readonly VeriFactuSCUConfiguration _veriFactuSCUConfiguration;
    private readonly bool _signXml;
    public VeriFactuMapping(VeriFactuSCUConfiguration configuration, bool signXml = true)
    {
        _veriFactuSCUConfiguration = configuration;
        _signXml = signXml;
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
                NombreRazon = _veriFactuSCUConfiguration.NombreRazonEmisor,
                NIF = _veriFactuSCUConfiguration.Nif
            },

        };

        return new RegFactuSistemaFacturacion
        {
            Cabecera = cabecera,
            RegistroFactura = registroFactura.ToArray()
        };
    }

    public RegistroFacturacionAnulacion CreateRegistroFacturacionAnulacion(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, ReceiptResponse lastReceiptResponse, ReceiptRequest referencedReceiptRequest, ReceiptResponse referencedReceiptResponse)
    {
        if (receiptRequest.cbPreviousReceiptReference is null)
        {
            throw new Exception("cbPreviousReceiptReference is required for voiding a receipt.");
        }
        if (receiptRequest.cbPreviousReceiptReference.IsGroup)
        {
            throw new NotSupportedException("Grouping of receipts is not supported.");
        }

        var registroFacturacionAnulacion = new RegistroFacturacionAnulacion
        {
            IDVersion = Version.Item10,
            IDFactura = new IDFacturaExpedidaBaja
            {

                IDEmisorFacturaAnulada = referencedReceiptResponse.ftSignatures.First(x => x.ftSignatureType.IsType(SignatureTypeES.NIF)).Data,
                NumSerieFacturaAnulada = referencedReceiptResponse.GetNumSerieFactura(),
                FechaExpedicionFacturaAnulada = referencedReceiptRequest.cbReceiptMoment.ToString("dd-MM-yyy")
            },
            Encadenamiento = new RegistroFacturacionAnulacionEncadenamiento
            {
                Item = GetEncadenamientoFacturaAnteriorAnulacion(lastReceiptResponse, referencedReceiptRequest, referencedReceiptResponse)
            },
            // Which PosSystem from the list should we take? In DE we just take the first one...
            // Is this fiskaltrust or the dealer/creator
            SistemaInformatico = new SistemaInformatico
            {
                NombreRazon = _veriFactuSCUConfiguration.NombreRazonEmisor,
                NombreSistemaInformatico = "fiskaltrust.Middleware",
                // Identification code given by the producing person or entity to its computerised invoicing system (RIS) which, once installed, constitutes the RIS used.
                // It should distinguish it from any other possible different RIS produced by the same producing person or entity.
                // The possible restrictions to its values shall be detailed in the corresponding documentation in the AEAT electronic office (validations document...).
                IdSistemaInformatico = "00", // alphanumeric(2)
                // VatId of producing company. We don't have that right now.
                Item = _veriFactuSCUConfiguration.Nif,
                Version = "1.0.0", // version
                NumeroInstalacion = receiptResponse.ftCashBoxIdentification,
                TipoUsoPosibleSoloVerifactu = Booleano.N,
                TipoUsoPosibleMultiOT = Booleano.N,
                IndicadorMultiplesOT = Booleano.N
            },
            FechaHoraHusoGenRegistro = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(new DateTimeOffset(receiptResponse.ftReceiptMoment, TimeSpan.Zero), "Europe/Madrid"),
            TipoHuella = TipoHuella.Item01,
            Huella = null!,
        };

        registroFacturacionAnulacion.Huella = registroFacturacionAnulacion.GetHuella();

        return (!_signXml || _veriFactuSCUConfiguration.Certificate is null) ? registroFacturacionAnulacion : XmlHelpers.Deserialize<RegistroFacturacionAnulacion>(XmlHelpers.SignXmlContentWithXades(XmlHelpers.GetXMLIncludingNamespace(registroFacturacionAnulacion, "sf", "RegistroFacturacionAlta"), _veriFactuSCUConfiguration.Certificate))!;
    }

    public RegistroFacturacionAlta CreateRegistroFacturacionAlta(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, ReceiptRequest? lastReceiptRequest, ReceiptResponse? lastReceiptResponse)
    {
        var registroFacturacionAlta = new RegistroFacturacionAlta
        {
            IDVersion = Version.Item10,
            IDFactura = new IDFactura
            {
                IDEmisorFactura = _veriFactuSCUConfiguration.Nif,
                NumSerieFactura = receiptResponse.GetNumSerieFactura(),
                FechaExpedicionFactura = receiptRequest.cbReceiptMoment.ToString("dd-MM-yyy")
            },

            // "Name and business name of the person required to issue the invoice."
            // Not sure how this needs to be formated. Maybe we'll need some extra fields in the master data?
            // Should this be the AccountName, or OutletName or sth from the Agencies?
            NombreRazonEmisor = _veriFactuSCUConfiguration.NombreRazonEmisor,
            TipoFactura = receiptRequest.ftReceiptCase.GetType() switch
            {
                _ => ClaveTipoFactura.F2, // QUESTION: is simplified invoice correct?
                                          // _ => throw new Exception($"Invalid receipt case {receiptRequest.ftReceiptCase}")
            },
            DescripcionOperacion = "test", // TODO: add descrpiton?,
                                           // FacturaSinIdentifDestinatarioArt61d = Booleano.S, // TODO: do we need this art. 61d?
            Desglose = receiptRequest.cbChargeItems.Select(chargeItem => BuildDetalle(chargeItem)).ToArray(),
            CuotaTotal = receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.GetVATAmount()).ToVeriFactuNumber(),
            ImporteTotal = (receiptRequest.cbReceiptAmount ?? receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.Amount)).ToVeriFactuNumber(),
            Encadenamiento = new RegistroFacturacionAltaEncadenamiento
            {
                Item = GetEncadenamientoFacturaAnteriorAlta(lastReceiptRequest, lastReceiptResponse)
            },
            // Which PosSystem from the list should we take? In DE we just take the first one...
            // Is this fiskaltrust or the dealer/creator
            SistemaInformatico = new SistemaInformatico
            {
                NombreRazon = _veriFactuSCUConfiguration.NombreRazonEmisor,
                NombreSistemaInformatico = "fiskaltrust.Middleware",
                // Identification code given by the producing person or entity to its computerised invoicing system (RIS) which, once installed, constitutes the RIS used.
                // It should distinguish it from any other possible different RIS produced by the same producing person or entity.
                // The possible restrictions to its values shall be detailed in the corresponding documentation in the AEAT electronic office (validations document...).
                IdSistemaInformatico = "00", // alphanumeric(2)
                                             // VatId of producing company. We don't have that right now.
                Item = _veriFactuSCUConfiguration.Nif,
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

        return (!_signXml || _veriFactuSCUConfiguration.Certificate is null) ? registroFacturacionAlta : XmlHelpers.Deserialize<RegistroFacturacionAlta>(XmlHelpers.SignXmlContentWithXades(XmlHelpers.GetXMLIncludingNamespace(registroFacturacionAlta, "sf", "RegistroFacturacionAlta"), _veriFactuSCUConfiguration.Certificate))!;
    }

    private Detalle BuildDetalle(ChargeItem chargeItem)
    {
        var nature = chargeItem.ftChargeItemCase.NatureOfVatES();
        var item = MapNatureOfVatToOperacion(nature, chargeItem.ftChargeItemCase);
        var impuesto = ResolveImpuesto(_veriFactuSCUConfiguration.TaxRegime);
        var claveRegimen = ResolveClaveRegimen(nature, _veriFactuSCUConfiguration.TaxRegime);

        ValidateOperacionCombination(impuesto, item);

        return new Detalle
        {
            // L1 Impuesto: 01 VAT mainland, 02 IPSI (Ceuta/Melilla), 03 IGIC (Canary Islands), 05 Other.
            Impuesto = impuesto,
            // L8A ClaveRegimen (shared XML element for L8A / L8B; AEAT validates allowed values per L1).
            ClaveRegimen = claveRegimen,
            BaseImponibleOimporteNoSujeto = (chargeItem.Amount - chargeItem.GetVATAmount()).ToVeriFactuNumber(),
            Item = item,
            TipoImpositivo = chargeItem.VATRate.ToVeriFactuNumber(),
            CuotaRepercutida = chargeItem.GetVATAmount().ToVeriFactuNumber()
        };
    }

    private static object MapNatureOfVatToOperacion(ChargeItemCaseNatureOfVatES nature, ChargeItemCase chargeItemCase) => nature switch
    {
        // If CalificacionOperacion is S1 and BaseImponibleACoste is not filled in, TipoImpositivo and CuotaRepercutida are mandatory.
        ChargeItemCaseNatureOfVatES.UsualVatApplies => CalificacionOperacion.S1,                  // NN [00]

        ChargeItemCaseNatureOfVatES.NotSubjectLocationRules => CalificacionOperacion.N2,          // NN [20]
        ChargeItemCaseNatureOfVatES.NotSubjectArticle7and14 => CalificacionOperacion.N1,          // NN [21]

        ChargeItemCaseNatureOfVatES.Exports => OperacionExenta.E2,                                // NN [10]
        ChargeItemCaseNatureOfVatES.IntraCommunityDelivery => OperacionExenta.E5,                 // NN [11]
        ChargeItemCaseNatureOfVatES.TransactionsTreatedAsExports => OperacionExenta.E3,           // NN [13]
        ChargeItemCaseNatureOfVatES.CustomsAndTaxExemptions => OperacionExenta.E4,                // NN [14]
        ChargeItemCaseNatureOfVatES.ExemptedDomestic => OperacionExenta.E1,                       // NN [30]
        ChargeItemCaseNatureOfVatES.OtherExemptions => OperacionExenta.E6,                        // NN [31]

        ChargeItemCaseNatureOfVatES.ReverseCharge => CalificacionOperacion.S2,                    // NN [50]

        // VeriFactu has no dedicated L9/L10 key for "foreign tax applies" (NN[60]) or "excluded" (NN[80]);
        // they are routed via L1 (Impuesto) and categorised under non-subject "others" (N1).
        ChargeItemCaseNatureOfVatES.ForeignTaxApplies => CalificacionOperacion.N1,                // NN [60]
        ChargeItemCaseNatureOfVatES.ExcludedThirdParty => CalificacionOperacion.N1,               // NN [80]

        _ => throw new Exception($"Invalid charge item case {chargeItemCase}")
    };

    private static Impuesto ResolveImpuesto(VeriFactuTaxRegime regime) => regime switch
    {
        VeriFactuTaxRegime.MainlandVat => Impuesto.Item01,
        VeriFactuTaxRegime.IPSI => Impuesto.Item02,
        VeriFactuTaxRegime.IGIC => Impuesto.Item03,
        VeriFactuTaxRegime.Other => Impuesto.Item05,
        _ => throw new Exception($"Unsupported tax regime {regime}")
    };

    private static IdOperacionesTrascendenciaTributaria ResolveClaveRegimen(ChargeItemCaseNatureOfVatES nature, VeriFactuTaxRegime regime) => nature switch
    {
        // Exports / treated-as-exports / customs exemptions use ClaveRegimen 02 ("Export operations").
        ChargeItemCaseNatureOfVatES.Exports => IdOperacionesTrascendenciaTributaria.Item02,                      // NN [10]
        ChargeItemCaseNatureOfVatES.TransactionsTreatedAsExports => IdOperacionesTrascendenciaTributaria.Item02, // NN [13]
        ChargeItemCaseNatureOfVatES.CustomsAndTaxExemptions => IdOperacionesTrascendenciaTributaria.Item02,      // NN [14]
        _ => IdOperacionesTrascendenciaTributaria.Item01
    };

    private static void ValidateOperacionCombination(Impuesto impuesto, object item)
    {
        // AEAT rejects L8B (IGIC-specific keys) when L1 != 03. The current Detalle uses a single
        // ClaveRegimen field for both L8A and L8B; this guard is reserved for future IGIC-specific codes.
        if (item is not CalificacionOperacion && item is not OperacionExenta)
        {
            throw new Exception($"Detalle.Item must be CalificacionOperacion or OperacionExenta, got {item?.GetType()}.");
        }
    }

    private object GetEncadenamientoFacturaAnteriorAlta(ReceiptRequest? lastReceiptRequest, ReceiptResponse? lastReceiptResponse)
    {
        if (lastReceiptRequest is null || lastReceiptResponse is null)
        {
            return PrimerRegistroCadena.S;
        }

        return new EncadenamientoFacturaAnterior
        {
            IDEmisorFactura = lastReceiptResponse.ftSignatures.First(x => x.ftSignatureType.IsType(SignatureTypeES.NIF)).Data,
            NumSerieFactura = lastReceiptResponse.GetNumSerieFactura(),
            FechaExpedicionFactura = lastReceiptRequest!.cbReceiptMoment.ToString("dd-MM-yyy"),
            Huella = lastReceiptResponse.ftSignatures.First(x => x.ftSignatureType.IsType(SignatureTypeES.Huella)).Data
        };

    }


    private object GetEncadenamientoFacturaAnteriorAnulacion(ReceiptResponse lastReceiptResponse, ReceiptRequest referencedReceiptRequest, ReceiptResponse referencedReceiptResponse)
    {
        return new EncadenamientoFacturaAnterior
        {
            IDEmisorFactura = referencedReceiptResponse.ftSignatures.First(x => x.ftSignatureType.IsType(SignatureTypeES.NIF)).Data,
            NumSerieFactura = referencedReceiptResponse.GetNumSerieFactura(),
            FechaExpedicionFactura = referencedReceiptRequest!.cbReceiptMoment.ToString("dd-MM-yyy"),
            Huella = lastReceiptResponse.ftSignatures.First(x => x.ftSignatureType.IsType(SignatureTypeES.Huella)).Data
        };
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