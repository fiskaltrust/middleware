using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;
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

    public static SignatureItem CreatePTQRCode(ProcessResponse processResponse, bool sandbox, string qrCode)
    {
        if (sandbox)
        {
            return new SignatureItem()
            {
                Caption = $"https://receipts-sandbox.fiskaltrust.eu/{processResponse.ReceiptResponse.ftQueueID}/{processResponse.ReceiptResponse.ftQueueItemID}",
                Data = qrCode,
                ftSignatureFormat = SignatureFormat.QRCode,
                ftSignatureType = SignatureTypePT.PosReceipt.As<SignatureType>()
            };
        }
        else
        {
            return new SignatureItem()
            {
                Caption = $"https://receipts.fiskaltrust.eu/{processResponse.ReceiptResponse.ftQueueID}/{processResponse.ReceiptResponse.ftQueueItemID}",
                Data = qrCode,
                ftSignatureFormat = SignatureFormat.QRCode,
                ftSignatureType = SignatureTypePT.PosReceipt.As<SignatureType>()
            };
        }
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
            Data = $"{printHash} - Processado por programa certificado" + $" No {fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.PTMappings.CertificationPosSystem.SoftwareCertificateNumber}/AT",
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

    public static SignatureItem AddProformaReference(List<Receipt> receiptReferences)
    {
        return new SignatureItem
        {
            Caption = $"",
            Data = $"Referencia: Proforma {receiptReferences[0].Response.ftReceiptIdentification.Split("#").Last()}",
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypePT.ReferenceForCreditNote.As<SignatureType>(),
        };
    }

    public static SignatureItem AddReferenceSignature(List<Receipt> receiptReferences)
    {
        return new SignatureItem
        {
            Caption = $"Referencia {receiptReferences[0].Response.ftReceiptIdentification.Split("#").Last()}",
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

    public static SignatureItem AddManualDocumentIdentification(string series, long number)
    {
        return new SignatureItem
        {
            Caption = "",
            Data = $"Cópia do documento original - FSM {series}/{number:D4}",
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypePT.PTAdditional.As<SignatureType>(),
        };
    }

    public static SignatureItem AddConsumidorFinal()
    {
        return new SignatureItem
        {
            Caption = "",
            Data = "Consumidor final",
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypePT.PTAdditional.As<SignatureType>(),
        };
    }

    public static void AddCustomerSignaturesIfNecessary(ProcessCommandRequest request, ProcessResponse response)
    {
        var cbCustomer = request.ReceiptRequest.GetCustomerOrNull();
        if (cbCustomer is null || cbCustomer.CustomerVATId == "999999990")
        {
            response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddConsumidorFinal());
        }
    }

    public static void AddHandWrittenSignatures(ProcessCommandRequest request, ProcessResponse response)
    {
        if (request.ReceiptRequest.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayload>(out var data) && data.PT is not null && data.PT.Series is not null && data.PT.Number.HasValue)
        {
            response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddManualDocumentIdentification(data.PT.Series, data.PT.Number.Value));
        }
    }
}
