using System;
using System.Web;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.ifPOS.v2.es.Cases;
using Microsoft.Xades;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Factories;

public static class SignaturItemFactory
{
    public static SignatureItem CreateResponseMessageSignature((string code, string message) message)
    {
        return new SignatureItem()
        {
            Caption = $"Codigo {message.code}",
            Data = message.message,
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = ifPOS.v2.Cases.SignatureType.Unknown.WithCategory(SignatureTypeCategory.Information)
        };
    }

    public static SignatureItem CreateQRCodeSignature(string qrCodeContent)
    {
        return new SignatureItem()
        {
            Caption = "[www.fiskaltrust.es]",
            Data = qrCodeContent,
            ftSignatureFormat = SignatureFormat.QRCode,
            ftSignatureType = SignatureTypeES.Url.As<ifPOS.v2.Cases.SignatureType>()
        };
    }

    public static SignatureItem CreateTBAIIdentifierSignature(string identifier)
    {
        return new SignatureItem()
        {
            Caption = "",
            Data = identifier,
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = ifPOS.v2.Cases.SignatureType.Unknown.WithCountry("ES")
        };
    }

    public static SignatureItem CreateSignatureSignature(XadesSignedXml signature)
    {
        return new SignatureItem()
        {
            Caption = "Signature",
            Data = Convert.ToBase64String(signature.SignatureValue!),
            ftSignatureFormat = SignatureFormat.Base64,
            ftSignatureType = SignatureTypeES.Signature.As<ifPOS.v2.Cases.SignatureType>().WithFlag(SignatureTypeFlags.DontVisualize)
        };
    }
}
