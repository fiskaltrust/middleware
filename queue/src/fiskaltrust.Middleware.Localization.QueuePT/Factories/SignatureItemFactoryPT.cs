using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Interface;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePT.Factories;

public static class SignatureItemFactoryPT
{
    public static SignatureItem CreateInitialOperationSignature(ftQueue queue)
    {
        return new SignatureItem()
        {
            ftSignatureType = SignatureTypePT.InitialOperationReceipt.As<SignatureType>(),
            ftSignatureFormat = SignatureFormat.Text,
            Caption = $"Initial-operation receipt",
            Data = $"Queue-ID: {queue.ftQueueId}"
        };
    }

    public static SignatureItem CreateOutOfOperationSignature(ftQueue queue)
    {
        return new SignatureItem()
        {
            ftSignatureType = SignatureTypePT.OutOfOperationReceipt.As<SignatureType>(),
            ftSignatureFormat = SignatureFormat.Text,
            Caption = $"Out-of-operation receipt",
            Data = $"Queue-ID: {queue.ftQueueId}"
        };
    }

    public static SignatureItem CreatePTQRCode(string qrCode)
    {
        return new SignatureItem()
        {
            Caption = "[www.fiskaltrust.pt]",
            Data = qrCode,
            ftSignatureFormat = SignatureFormat.QRCode,
            ftSignatureType = SignatureTypePT.PosReceipt.As<SignatureType>()
        };
    }

    public static SignatureItem AddIvaIncluido()
    {
        return new SignatureItem
        {
            Caption = "",
            Data = "IVA incluido",
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypePT.PTAdditional.As<SignatureType>(),
        };
    }

    public static SignatureItem AddATCUD(NumberSeries series)
    {
        return new SignatureItem
        {
            Caption = "",
            Data = "ATCUD: " + series.ATCUD + "-" + series.Numerator,
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypePT.ATCUD.As<SignatureType>(),
        };
    }

    public static SignatureItem AddCertificateSignature(string printHash)
    {
        return new SignatureItem
        {
            Caption = $"-----",
            Data = $"{printHash} - Processado por programa certificado" + $" No {CertificationPosSystem.SoftwareCertificateNumber}/AT",
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypePT.CertificationNo.As<SignatureType>(),
        };
    }

    public static SignatureItem AddHash(string hash)
    {
        return new SignatureItem
        {
            Caption = "Hash",
            Data = hash,
            ftSignatureFormat = SignatureFormat.Text.WithPosition(SignatureFormatPosition.AfterHeader),
            ftSignatureType = SignatureTypePT.Hash.As<SignatureType>().WithFlag(SignatureTypeFlags.DontVisualize),
        };
    }

    public static SignatureItem AddProformaReference(List<(ReceiptRequest, ReceiptResponse)> receiptReferences)
    {
        return new SignatureItem
        {
            Caption = $"",
            Data = $"Referencia: Proforma {receiptReferences[0].Item2.ftReceiptIdentification.Split("#").Last()}",
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypePT.ReferenceForCreditNote.As<SignatureType>(),
        };
    }

    public static SignatureItem AddReferenceSignature(List<(ReceiptRequest, ReceiptResponse)> receiptReferences)
    {
        return new SignatureItem
        {
            Caption = $"Referencia {receiptReferences[0].Item2.ftReceiptIdentification.Split("#").Last()}",
            Data = $"Razão: Devolução",
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypePT.ReferenceForCreditNote.As<SignatureType>(),
        };
    }

    public static SignatureItem AddDocumentoNao()
    {
        return new SignatureItem
        {
            Caption = "",
            Data = "Este documento não serve de fatura",
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypePT.PTAdditional.As<SignatureType>(),
        };
    }
}
