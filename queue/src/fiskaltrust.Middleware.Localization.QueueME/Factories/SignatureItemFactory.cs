using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.storage.V0;

#nullable enable
namespace fiskaltrust.Middleware.Localization.QueueME.Factories
{
    public class SignatureItemFactory
    {
        private readonly QueueMEConfiguration _queueMEConfiguration;

        public SignatureItemFactory(QueueMEConfiguration queueMEConfiguration)
        {
            _queueMEConfiguration = queueMEConfiguration;
        }

        public IEnumerable<SignaturItem> CreatePosReceiptSignatures(ReceiptRequest request, ComputeIICResponse computeIICResponse, ulong yearlyOrdinalNumber, ftSignaturCreationUnitME scuME)
        {
            var qrCode = _queueMEConfiguration.Sandbox ? "https://efitest.tax.gov.me/ic/#/verify" : "https://mapr.tax.gov.me/ic/#/verify";
            qrCode += $"?iic={computeIICResponse.IIC}";
            qrCode += $"&tin={scuME.IssuerTin}";
            qrCode += $"&crtd={request.cbReceiptMoment.ToString(@"yyyy-MM-dd\THH:mm:ss\Z")}";
            qrCode += $"&ord={yearlyOrdinalNumber}";
            qrCode += $"&bu={scuME.BusinessUnitCode}";
            qrCode += $"&cr={scuME.TcrCode}";
            qrCode += $"&sw={scuME.SoftwareCode}";
            qrCode += $"&prc={request.cbChargeItems.Sum(x => x.Amount)}";

            return new List<SignaturItem>
            {
                new SignaturItem
                {
                    Caption = "Taxpayer identifying code (IIC/IKOF)",
                    Data = computeIICResponse.IIC,
                    ftSignatureFormat = 0x01,
                    ftSignatureType = 0x4D45000000000003
                },
                new SignaturItem
                {
                    Caption = "QR code content",
                    Data = qrCode,
                    ftSignatureFormat = 0x03,
                    ftSignatureType = 0x4D45000000000001
                }
            };
        }

        public SignaturItem CreateInitialOperationSignature(Guid queueId, string tcrCode)
        {
            return new SignaturItem()
            {
                ftSignatureType = 0x4D45000000000002,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                Caption = $"Initial-operation receipt",
                Data = $"TCR-Code: {tcrCode}, Queue-ID: {queueId}"
            };
        }

        public SignaturItem CreateOutOfOperationSignature(Guid queueId, string tcrCode)
        {
            return new SignaturItem()
            {
                ftSignatureType = 0x4D45000000000002,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                Caption = $"Out-of-operation receipt",
                Data = $"TCR-Code: {tcrCode}, Queue-ID: {queueId}"
            };
        }
    }
}
