using System.Web;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es.Cases;
using fiskaltrust.Middleware.SCU.ES.Models;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Helpers;

public static class SignaturItemHelper
{
    public static SignatureItem[] CreateVeriFactuQRCode(string baseUrl, RegistroFacturacionAlta registroFacturacionAlta)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query.Add("nif", registroFacturacionAlta.IDFactura.IDEmisorFactura);
        query.Add("numserie", registroFacturacionAlta.IDFactura.NumSerieFactura);
        query.Add("fecha", registroFacturacionAlta.IDFactura.FechaExpedicionFactura);
        query.Add("importe", registroFacturacionAlta.ImporteTotal);

        var uriBuider = new UriBuilder(baseUrl)
        {
            Query = query.ToString()
        };

        return [
            new SignatureItem()
            {
                Caption = "QR tributario:",
                Data = uriBuider.Uri.ToString(),
                ftSignatureFormat = SignatureFormat.QRCode.WithPosition(SignatureFormatPosition.BeforeHeader),
                ftSignatureType = SignatureTypeES.Url.As<SignatureType>(),
            },
            new SignatureItem {
                Data = "Factura verificable en la Sede electrónica de la AEAT",
                ftSignatureFormat = SignatureFormat.Text.WithPosition(SignatureFormatPosition.BeforeHeader),
                ftSignatureType = SignatureType.Unknown,
            }
        ];
    }

    
}
