using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Contracts.Models;

namespace fiskaltrust.Middleware.Localization.QueueDE
{
    public class SignatureFactoryDE
    {
        private const long OPTIONAL_FLAG = 0x10000;
        private readonly QueueDEConfiguration _queueDEConfiguration;

        public SignatureFactoryDE(QueueDEConfiguration queueDEConfiguration)
        {
            _queueDEConfiguration = queueDEConfiguration;
        }

        public SignaturItem CreateInitialOperationSignature(Guid queueId, string clientId, string serialnumberOctet)
        {
            return new SignaturItem()
            {
                ftSignatureType = (long) SignaturItem.Types.DE_StorageObligation,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                Caption = $"In-Betriebnahme-Beleg",
                Data = $"Kassenseriennummer: {clientId}, TSE-Seriennummer: {serialnumberOctet}, Queue-ID: {queueId}"
            };
        }

        public SignaturItem CreateOutOfOperationSignature(Guid queueId, string clientId, string serialnumberOctet)
        {
            return new SignaturItem()
            {
                ftSignatureType = (long) SignaturItem.Types.DE_StorageObligation,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                Caption = $"Außer-Betriebnahme-Beleg",
                Data = $"Kassenseriennummer: {clientId}, TSE-Seriennummer: {serialnumberOctet}, Queue-ID: {queueId}"
            };
        }

        public SignaturItem CreateInitiateScuSwitchSignature(Guid queueId, string clientId, string serialnumberOctet)
        {
            return new SignaturItem()
            {
                ftSignatureType = (long) SignaturItem.Types.DE_StorageObligation,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                Caption = $"TSE-Trennungs-Beleg",
                Data = $"Kassenseriennummer: {clientId}, TSE-Seriennummer: {serialnumberOctet}, Queue-ID: {queueId}"
            };
        }

        public SignaturItem CreateFinishScuSwitchSignature(Guid queueId, string clientId, string serialnumberOctet)
        {
            return new SignaturItem()
            {
                ftSignatureType = (long) SignaturItem.Types.DE_StorageObligation,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                Caption = $"TSE-Verbindungs-Beleg",
                Data = $"Kassenseriennummer: {clientId}, TSE-Seriennummer: {serialnumberOctet}, Queue-ID: {queueId}"
            };
        }

        public SignaturItem GetSignaturForStartTransaction(StartTransactionResponse startTransactionResponse) 
            => CreateBase64Signature(SignatureTypesDE.StartTransactionResult, "start-transaction-signature", startTransactionResponse.SignatureData.SignatureBase64, true);

        public List<SignaturItem> GetSignaturesForUpdateTransaction(UpdateTransactionResponse updateTransactionResponse)
        {
            return new List<SignaturItem> {
                CreateBase64Signature(SignatureTypesDE.UpdateTransactionPayload, "update-transaction-payload", updateTransactionResponse.ProcessDataBase64, true),
                CreateBase64Signature(SignatureTypesDE.UpdateTransactionResult, "update-transaction-signature", updateTransactionResponse.SignatureData.SignatureBase64, true),
                CreateTextSignature(SignatureTypesDE.ProcessType, "<processType>", updateTransactionResponse.ProcessType, true)
            };
        }

        public List<SignaturItem> GetSignaturesForFinishTransaction(FinishTransactionResponse finishTransactionResponse)
        {
            return new List<SignaturItem>{
                CreateBase64Signature(SignatureTypesDE.FinishTransactionPayload, "finish-transaction-payload", finishTransactionResponse.ProcessDataBase64, true),
                CreateBase64Signature(SignatureTypesDE.FinishTransactionResult, "finish-transaction-signature", finishTransactionResponse.SignatureData.SignatureBase64, true),
                CreateTextSignature(SignatureTypesDE.ProcessType, "<processType>", finishTransactionResponse.ProcessType, true)
            };
        }

        public List<SignaturItem> GetSignaturesForTransaction(string startTransactionSignatureBase64, FinishTransactionResponse finishTransactionResponse, string certificationIdentification)
        {
            var signatures = new List<SignaturItem>();

            if (startTransactionSignatureBase64 != null)
            {
                signatures.Add(CreateBase64Signature(SignatureTypesDE.StartTransactionResult, "start-transaction-result", startTransactionSignatureBase64, true));
            }

            signatures.Add(CreateBase64Signature(SignatureTypesDE.FinishTransactionPayload, "finish-transaction-payload", finishTransactionResponse.ProcessDataBase64, true));
            signatures.Add(CreateBase64Signature(SignatureTypesDE.FinishTransactionResult, "finish-transaction-result", finishTransactionResponse.SignatureData.SignatureBase64, true));
            signatures.Add(CreateTextSignature(SignatureTypesDE.ProcessType, "<processType>", finishTransactionResponse.ProcessType, true));
            signatures.Add(CreateTextSignature(SignatureTypesDE.CashBoxIdentification, "<kassen-seriennummer>", finishTransactionResponse.ClientId, true));
            signatures.Add(CreateTextSignature(SignatureTypesDE.ProcessData, "<processData>", Encoding.UTF8.GetString(Convert.FromBase64String(finishTransactionResponse.ProcessDataBase64)), true));
            signatures.Add(CreateTextSignature(SignatureTypesDE.TransactionNumber, "<transaktions-nummer>", finishTransactionResponse.TransactionNumber.ToString(), true));
            signatures.Add(CreateTextSignature(SignatureTypesDE.SignatureCounter, "<signatur-zaehler>", finishTransactionResponse.SignatureData.SignatureCounter.ToString(), true));
            signatures.Add(CreateTextSignature(SignatureTypesDE.StartTime, "<start-zeit>", finishTransactionResponse.StartTransactionTimeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), true));
            signatures.Add(CreateTextSignature(SignatureTypesDE.LogTime, "<log-time>", finishTransactionResponse.TimeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), true));
            signatures.Add(CreateTextSignature(SignatureTypesDE.Signature, "<signatur>", finishTransactionResponse.SignatureData.SignatureBase64, true));
            signatures.Add(CreateTextSignature(SignatureTypesDE.SignaturAlgorithm, "<sig-alg>", finishTransactionResponse.SignatureData.SignatureAlgorithm, true));
            signatures.Add(CreateTextSignature(SignatureTypesDE.LogTimeFormat, "<log-time-format>", finishTransactionResponse.TseTimeStampFormat, true));
            signatures.Add(CreateTextSignature(SignatureTypesDE.CertificationId, "<certification-id>", certificationIdentification, false));
            signatures.Add(CreateTextSignature(SignatureTypesDE.PublicKey, "<public-key>", finishTransactionResponse.SignatureData.PublicKeyBase64, true));

            return signatures;
        }

        public List<SignaturItem> GetSignaturesForPosReceiptTransaction(string startTransactionSignatureBase64, FinishTransactionResponse finishResultResponse, string certificationIdentification)
        {
            var qrCodeSignatures = new List<SignaturItem>{
                CreateTextSignature(SignatureTypesDE.QrCodeVersion, "<qr-code-version>", "V0", true),
                CreateTextSignature(SignatureTypesDE.CashBoxIdentification, "<kassen-seriennummer>", finishResultResponse.ClientId, true),
                CreateTextSignature(SignatureTypesDE.ProcessType, "<processType>", finishResultResponse.ProcessType, true),
                CreateTextSignature(SignatureTypesDE.ProcessData, "<processData>", Encoding.UTF8.GetString(Convert.FromBase64String(finishResultResponse.ProcessDataBase64)), true),
                CreateTextSignature(SignatureTypesDE.TransactionNumber, "<transaktions-nummer>", finishResultResponse.TransactionNumber.ToString(), true),
                CreateTextSignature(SignatureTypesDE.SignatureCounter, "<signatur-zaehler>", finishResultResponse.SignatureData.SignatureCounter.ToString(), true),
                CreateTextSignature(SignatureTypesDE.StartTime, "<start-zeit>", finishResultResponse.StartTransactionTimeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), true),
                CreateTextSignature(SignatureTypesDE.LogTime, "<log-time>", finishResultResponse.TimeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), true),
                CreateTextSignature(SignatureTypesDE.SignaturAlgorithm, "<sig-alg>", finishResultResponse.SignatureData.SignatureAlgorithm, true),
                CreateTextSignature(SignatureTypesDE.LogTimeFormat, "<log-time-format>", finishResultResponse.TseTimeStampFormat, true),
                CreateTextSignature(SignatureTypesDE.Signature, "<signatur>", finishResultResponse.SignatureData.SignatureBase64, true),
                CreateTextSignature(SignatureTypesDE.PublicKey, "<public-key>", finishResultResponse.SignatureData.PublicKeyBase64, true)
            };

            var qrCode = string.Join(";", qrCodeSignatures.Select(x => x.Data).ToArray());

            var signatures = new List<SignaturItem>{
                CreateQrCodeSignature(SignatureTypesDE.QrCodeAccordingKassenSichV, "www.fiskaltrust.de", qrCode, false),
                CreateBase64Signature(SignatureTypesDE.StartTransactionResult, "start-transaction-signature", startTransactionSignatureBase64, true),
                CreateBase64Signature(SignatureTypesDE.FinishTransactionPayload, "finish-transaction-payload", finishResultResponse.ProcessDataBase64, true),
                CreateBase64Signature(SignatureTypesDE.FinishTransactionResult, "finish-transaction-signature", finishResultResponse.SignatureData.SignatureBase64, true),
                CreateTextSignature(SignatureTypesDE.CertificationId, "<certification-id>", certificationIdentification, false), //TODO clarify
                CreateTextSignature(SignatureTypesDE.TseSerialNumber, "<tse-seriennummer>", finishResultResponse.TseSerialNumberOctet, true)
            };
            signatures.AddRange(qrCodeSignatures);
            return signatures;
        }

        public SignaturItem GetSignatureForPosReceiptActionStartMoment(DateTime moment)
            => CreateTextSignature(SignatureTypesDE.VorgangsBeginn, "<vorgangsbeginn>", moment.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), false);

        public SignaturItem GetSignatureForTraining()
            => CreateTextSignature(0x0000_0000_0000_1000, "Trainingsbuchung", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), false);

        public SignaturItem CreateQrCodeSignature(SignatureTypesDE type, string caption, string data, bool optional) => new SignaturItem
        {
            Caption = caption,
            ftSignatureType = (long) type | 0x4445_0000_0000_0000,
            ftSignatureFormat = AddOptionalFlagIfRequired(SignaturItem.Formats.QR_Code, optional),
            Data = data
        };

        public SignaturItem CreateBase64Signature(SignatureTypesDE type, string caption, string data, bool optional) => new SignaturItem
        {
            Caption = caption,
            ftSignatureType = (long) type | 0x4445_0000_0000_0000,
            ftSignatureFormat = AddOptionalFlagIfRequired(SignaturItem.Formats.Base64, optional),
            Data = data
        };

        public SignaturItem CreateTextSignature(SignatureTypesDE type, string caption, string data, bool optional) => new SignaturItem
        {
            Caption = caption,
            ftSignatureType = (long) type | 0x4445_0000_0000_0000,
            ftSignatureFormat = AddOptionalFlagIfRequired(SignaturItem.Formats.Text, optional),
            Data = data
        };

        public SignaturItem CreateTextSignature(long type, string caption, string data, bool optional) => new SignaturItem
        {
            Caption = caption,
            ftSignatureType = type,
            ftSignatureFormat = AddOptionalFlagIfRequired(SignaturItem.Formats.Text, optional),
            Data = data
        };

        private long AddOptionalFlagIfRequired(SignaturItem.Formats format, bool optional) 
            => _queueDEConfiguration.FlagOptionalSignatures && optional ? (long) format | OPTIONAL_FLAG : (long) format;
    }
}
