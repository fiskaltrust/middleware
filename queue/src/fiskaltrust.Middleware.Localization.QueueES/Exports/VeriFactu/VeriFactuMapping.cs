using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Web;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueES.Exports;

public class VeriFactuMapping
{
    private readonly MasterDataConfiguration _masterData;

    public VeriFactuMapping(MasterDataConfiguration masterData)
    {
        _masterData = masterData;
    }

    public RegFactuSistemaFacturacion CreateRegFactuSistemaFacturacion(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, (IDFacturaExpedidaType id, string hash)? previous)
    {
        var cabecera = new Cabecera
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

        if (receiptRequest.IsVoid())
        {
            registroFactura.Add(
                new RegistroFacturaType
                {
                    Item = CreateRegistroFacturacionAnulacion(receiptRequest, receiptResponse)
                });
        }
        else
        {
            registroFactura.Add(
                new RegistroFacturaType
                {
                    Item = CreateRegistroFacturacionAlta(receiptRequest, receiptResponse, previous)
                });
        }

        return new RegFactuSistemaFacturacion
        {
            Cabecera = cabecera,
            RegistroFactura = registroFactura.ToArray()
        };
    }

    public RegistroFacturacionAnulacionType CreateRegistroFacturacionAnulacion(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        throw new NotImplementedException();
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
            // This field is described in the exel but not present in the xsd files
            // RefExterna = receiptRequest.ftQueueItemID,

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
                BaseRectificada = receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.GetVATAmount()).ToString("0.00"), // helper
                // whats the difference between `CuotaRectificada` and `BaseRectificada`
                CuotaRectificada = null,
            },
            Desglose = receiptRequest.cbChargeItems.Select(chargeItem => new DetalleType
            {
                BaseImponibleOimporteNoSujeto = (chargeItem.Amount - chargeItem.GetVATAmount()).ToString("0.00"),
                Item = chargeItem.ftChargeItemCase switch
                {
                    // figure out which ones map to which ones
                    _ => CalificacionOperacionType.S1
                    // _ => CalificacionOperacionType
                    // _ => OperacionExentaType
                }
            }).ToArray(),
            // is this correct? how should this differ from `ImporteRectificacion`
            CuotaTotal = receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.GetVATAmount()).ToString("0.00"),
            ImporteTotal = (receiptRequest.cbReceiptAmount ?? receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.Amount)).ToString("0.00"),
            Encadenamiento = new RegistroFacturacionAltaTypeEncadenamiento
            {
                Item = receiptRequest.IsInitialOperation()
                    ? PrimerRegistroCadenaType.S
                    : new EncadenamientoFacturaAnteriorType
                    {
                        // The `IDEmisorFactura` field needs to be the `IDFactura.IDEmisorFactura` of the previous receipt.
                        // We could either save the last IDEmisorFactura in the queueES like this
                        // or have a change masterdata reciept where we udpate the masterdata and handle this case
                        IDEmisorFactura = previous!.Value.id.IDEmisorFactura,
                        NumSerieFactura = previous!.Value.id.NumSerieFactura,
                        FechaExpedicionFactura = previous!.Value.id.FechaExpedicionFactura,
                        Huella = previous!.Value.hash,
                    }
            },
            // Which PosSystem from the list should we take? In DE we just take the first one...
            // Is this fiskaltrust or the dealer/creator
            SistemaInformatico = new SistemaInformaticoType
            {
                // "Name and company name of the producing person or entity."
                // The brand name is maybe not enough here?
                NombreRazon = _masterData.PosSystems.FirstOrDefault()!.Brand,
                // VatId of producing company. We don't have that right now.
                Item = null,
                // "Identification code given by the producing person or entity to its computerised invoicing system (RIS) which, once installed, constitutes the RIS used. It should distinguish it from any other possible different RIS produced by the same producing person or entity. The possible restrictions to its values shall be detailed in the corresponding documentation in the AEAT electronic office (validations document...)."
                // Is this correct? does this need to be in a specific format or something registered with the government somewhere?
                IdSistemaInformatico = _masterData.PosSystems.FirstOrDefault()!.Type,
                Version = _masterData.PosSystems.FirstOrDefault()!.SoftwareVersion,
                // "Installation number of the computerised invoicing system (RIS) used. It must be distinguished from any other possible RIS used for the invoicing of the person liable to issue invoices, i.e. from other possible past, present or future RIS installations used for the invoicing of the person liable to issue invoices, even if the same producer's RIS is used in these installations."
                // We don't have that right now.
                NumeroInstalacion = null,
            },
            FechaHoraHusoGenRegistro = receiptResponse.ftReceiptMoment,
            TipoHuella = TipoHuellaType.Item01
        };

        registroFacturacionAlta.Huella = registroFacturacionAlta.GetHuella();

        return registroFacturacionAlta;
    }
}

public static class RegistroFacturacionAltaTypeExt
{
    public static string GetHuella(this RegistroFacturacionAltaType registroFacturacionAlta)
        => registroFacturacionAlta.GetHuella(new List<(string key, Func<RegistroFacturacionAltaType, string> value)> {
            ("IDEmisorFactura", x => x.IDFactura.IDEmisorFactura),
            ("NumSerieFactura", x => x.IDFactura.NumSerieFactura),
            ("FechaExpedicionFactura", x => x.IDFactura.FechaExpedicionFactura),
            ("TipoFactura", x => Enum.GetName(x.TipoFactura)!),
            ("CuotaTotal", x => x.CuotaTotal),
            ("ImporteTotal", x => x.ImporteTotal),
            ("Huella", x => x.Encadenamiento.Item is EncadenamientoFacturaAnteriorType alta ? alta.Huella : "S"),
            ("FechaHoraHusoGenRegistro", x => x.FechaHoraHusoGenRegistro.ToString("yyyy-MM-ddThh:mm:sszzz")),
        });

    public static string GetHuella(this RegistroFacturacionAnulacionType registroFacturacionAnulacion)
        => registroFacturacionAnulacion.GetHuella(new List<(string key, Func<RegistroFacturacionAnulacionType, string> value)> {
            ("IDEmisorFacturaAnulada", x => x.IDFactura.IDEmisorFacturaAnulada),
            ("NumSerieFacturaAnulada", x => x.IDFactura.NumSerieFacturaAnulada),
            ("FechaExpedicionFacturaAnulada", x => x.IDFactura.FechaExpedicionFacturaAnulada),
            ("Huella", x => x.Encadenamiento.Item is EncadenamientoFacturaAnteriorType alta ? alta.Huella : "S"),
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