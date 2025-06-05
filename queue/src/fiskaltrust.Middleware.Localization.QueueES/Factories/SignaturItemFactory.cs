using System.Web;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueES.Models.Cases;
using fiskaltrust.Middleware.SCU.ES.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueES.Factories;

public static class SignaturItemFactory
{
    public static SignatureItem CreateInitialOperationSignature(ftQueue queue)
    {
        return new SignatureItem()
        {
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypeES.InitialOperationReceipt.As<SignatureType>().WithFlag(SignatureTypeFlags.ArchivingRequired).WithCategory(SignatureTypeCategory.Information),
            Data = $"Queue-ID: {queue.ftQueueId}"
        };
    }

    public static SignatureItem CreateOutOfOperationSignature(ftQueue queue)
    {
        return new SignatureItem()
        {
            ftSignatureType = SignatureTypeES.OutOfOperationReceipt.As<SignatureType>().WithFlag(SignatureTypeFlags.ArchivingRequired).WithCategory(SignatureTypeCategory.Information),
            ftSignatureFormat = SignatureFormat.Text,
            Caption = $"Out-of-operation receipt",
            Data = $"Queue-ID: {queue.ftQueueId}"
        };
    }

    public static SignatureItem[] CreateVeriFactuQRCode(string baseUrl, RegistroFacturacionAlta registroFacturacionAlta)
    {
        var query = HttpUtility.ParseQueryString(String.Empty);
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

    public static SignatureItem CreateESSignature(byte[] signature)
    {
        return new SignatureItem()
        {
            Caption = "Signature",
            Data = Convert.ToBase64String(signature),
            ftSignatureFormat = SignatureFormat.Base64,
            ftSignatureType = SignatureTypeES.Signature.As<SignatureType>().WithFlag(SignatureTypeFlags.VisualizationOptional)
        };
    }

    public static SignatureItem CreateESHuella(string huella)
    {
        return new SignatureItem()
        {
            Caption = "Huella",
            Data = huella,
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypeES.Huella.As<SignatureType>()
        };
    }
}
