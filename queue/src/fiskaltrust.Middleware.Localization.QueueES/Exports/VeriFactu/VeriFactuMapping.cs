using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Web;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
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

    public RegFactuSistemaFacturacion CreateRegFactuSistemaFacturacion(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, ReceiptResponse? previous)
    {
        var cabecera = new Cabecera
        {
            ObligadoEmision = new PersonaFisicaJuridicaESType
            {
                NombreRazon = _masterData.Outlet.OutletName, // Should be "Name and company name of the person responsible for issuing the invoices."
                NIF = _masterData.Outlet.VatId
            },
        };

        var registroFactura = new List<RegistroFacturaType>();

        if (receiptRequest.IsVoid()) // also refund?
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
                    Item = CreateRegistroFacturacionAlta(receiptRequest, receiptResponse)
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

    public RegistroFacturacionAltaType CreateRegistroFacturacionAlta(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var registroFacturacionAlta = new RegistroFacturacionAltaType
        {
            IDVersion = VersionType.Item10,
            IDFactura = new IDFacturaExpedidaType
            {
                IDEmisorFactura = _masterData.Outlet.VatId,
                NumSerieFactura = receiptResponse.ftReceiptIdentification, // Maybe split from '#'
                FechaExpedicionFactura = receiptRequest.cbReceiptMoment.ToString("dd-MM-yyy")
            },
            // RefExterna
            NombreRazonEmisor = _masterData.Outlet.OutletName, // "Name and business name of the person required to issue the invoice."
            TipoFactura = ClaveTipoFacturaType.F2, // From ReceiptCase
            ImporteRectificacion = new DesgloseRectificacionType
            {
                BaseRectificada = receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.VATAmount).ToString(), // Maybe need to truncate to 12,2
                CuotaRectificada = null, // whats the difference between these two
            },
            Desglose = receiptRequest.cbChargeItems.Select(chargeItem => new DetalleType
            {
                BaseImponibleOimporteNoSujeto = (chargeItem.Amount - chargeItem.VATAmount).ToString(), // Maybe need to truncate to 12,2
                Item = chargeItem.ftChargeItemCase switch
                {
                    _ => CalificacionOperacionType.S1 // figure out
                    // CalificacionOperacionType
                    // OperacionExentaType
                }
            }).ToArray(),
            CuotaTotal = receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.VATAmount).ToString(), // is this correct? how should this differ from ImporteRectificacion
            ImporteTotal = (receiptRequest.cbReceiptAmount ?? receiptRequest.cbChargeItems.Sum(chargeItem => chargeItem.Amount)).ToString(), // Maybe need to truncate to 12,2
            Encadenamiento = new RegistroFacturacionAltaTypeEncadenamiento
            {
                Item = receiptRequest.IsInitialOperation()
                    ? PrimerRegistroCadenaType.S
                    : new EncadenamientoFacturaAnteriorType
                    {
                        IDEmisorFactura = _masterData.Outlet.VatId, // This needs to be actually the `IDEmisorFactura` of the previous receipt
                        NumSerieFactura = null, // NumSerieFactura of previous receipt
                        FechaExpedicionFactura = null, // FechaExpedicionFactura of previous receipt
                        Huella = null, // Calculated Hash
                    }
            },
            SistemaInformatico = new SistemaInformaticoType
            {
                NombreRazon = _masterData.PosSystems.FirstOrDefault()!.Brand, // Name and Companyname?
                Item = null, // VatId of producing company 
                IdSistemaInformatico = _masterData.PosSystems.FirstOrDefault()!.Type, // need to clarify
                Version = _masterData.PosSystems.FirstOrDefault()!.SoftwareVersion,
                NumeroInstalacion = null, // Installation number of the possystem. Unique number of possystems use by the issuing entity.
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
    {
        var data = new StringBuilder()
            .AppendFormat(GetValue("IDEmisorFactura", registroFacturacionAlta.IDFactura.IDEmisorFactura))
            .AppendFormat(GetValue("NumSerieFactura", registroFacturacionAlta.IDFactura.NumSerieFactura))
            .AppendFormat(GetValue("FechaExpedicionFactura", registroFacturacionAlta.IDFactura.FechaExpedicionFactura))
            .AppendFormat(GetValue("TipoFactura", Enum.GetName(registroFacturacionAlta.TipoFactura)!))
            .AppendFormat(GetValue("CuotaTotal", registroFacturacionAlta.CuotaTotal))
            .AppendFormat(GetValue("ImporteTotal", registroFacturacionAlta.ImporteTotal))
            .AppendFormat(GetValue("Huella", registroFacturacionAlta.Encadenamiento.Item is EncadenamientoFacturaAnteriorType alta
                ? alta.Huella
                : "S"))
            .AppendFormat(GetValue("NumSerieFactura", registroFacturacionAlta.FechaHoraHusoGenRegistro.ToString("O"), separator: false))
            .ToString();

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));

        return Convert.ToHexString(hash);
    }

    private static string GetValue(string key, string value, bool encoded = false, bool separator = true)
        => key + "=" + (encoded ? HttpUtility.UrlEncode(value.Trim()) : value.Trim()) + (separator ? "&" : "");
}