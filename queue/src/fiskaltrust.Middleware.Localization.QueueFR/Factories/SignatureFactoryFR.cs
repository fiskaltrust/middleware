using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueFR.Factories
{
    public class SignatureFactoryFR : ISignatureFactoryFR
    {
        private readonly ICryptoHelper _cryptoHelper;

        public SignatureFactoryFR(ICryptoHelper cryptoHelper)
        {
            _cryptoHelper = cryptoHelper;
        }

        public SignaturItem CreateMessagePendingSignature()
        {
            return new SignaturItem
            {
                Caption = "fiskaltrust-Message pending",
                Data = "Create a Zero receipt",
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = (long) SignaturItem.Types.Information
            };
        }

        public SignaturItem CreateFailureRegisteredSignature(string fromReceipt, string toReceipt)
        {
            return new SignaturItem
            {
                Caption = "Failure registered",
                Data = $"From {fromReceipt} to {toReceipt} ",
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = (long) SignaturItem.Types.Information
            };
        }


        public SignaturItem CreateTotalsSignatureWithoutSigning(string payload, string description, SignaturItem.Formats format, SignaturItem.Types type)
        {
            return new SignaturItem
            {
                Caption = description,
                Data = payload,
                ftSignatureFormat = (long) format,
                ftSignatureType = (long) type
            };
        }

        public (string hash, SignaturItem signatureItem, ftJournalFR journalFR) CreateTotalsSignature(ReceiptResponse receiptResponse, ftQueue queue, ftSignaturCreationUnitFR signaturCreationUnitFR, string payload, string description, SignaturItem.Formats format, SignaturItem.Types type)
        {
            (var hash, var jwsData) = _cryptoHelper.CreateJwsToken(payload, signaturCreationUnitFR.PrivateKey, signaturCreationUnitFR.ftSignaturCreationUnitFRId.ToByteArray());
            var signatureItem = new SignaturItem
            {
                Caption = description,
                Data = jwsData,
                ftSignatureFormat = (long) format,
                ftSignatureType = (long) type
            };
            var journalFR = new ftJournalFR()
            {
                ftJournalFRId = Guid.NewGuid(),
                ftQueueId = Guid.Parse(receiptResponse.ftQueueID),
                ftQueueItemId = Guid.Parse(receiptResponse.ftQueueItemID),
                JWT = signatureItem.Data,
                Number = queue.ftReceiptNumerator + 1
            };
            return (hash, signatureItem, journalFR);
        }

        public SignaturItem CreatePerpetualTotalSignature(ftQueueFR queueFR)
        {
            var data = new
            {
                PerpetualTotal = queueFR.TTotalizer + queueFR.ITotalizer,
                PerpetualCITotalNormal = queueFR.TCITotalNormal + queueFR.ICITotalNormal,
                PerpetualCITotalReduced1 = queueFR.TCITotalReduced1 + queueFR.ICITotalReduced1,
                PerpetualCITotalReduced2 = queueFR.TCITotalReduced2 + queueFR.ICITotalReduced2,
                PerpetualCITotalReducedS = queueFR.TCITotalReducedS + queueFR.ICITotalReducedS,
                PerpetualCITotalUnknown = queueFR.TCITotalUnknown + queueFR.ICITotalUnknown,
                PerpetualCITotalZero = queueFR.TCITotalZero + queueFR.ICITotalZero
            };

            return new SignaturItem
            {
                Caption = "Totals",
                Data = JsonConvert.SerializeObject(data),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = 0x4652000000000007
            };
        }
    }
}
