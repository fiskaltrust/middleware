using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.storage.V0;

#nullable enable
namespace fiskaltrust.Middleware.Localization.QueueME.Factories
{
    internal class SignatureItemFactory
    {
        private readonly ReceiptRequest? _request;
        private readonly ComputeIICResponse? _computeIICResponse;
        private readonly ulong? _yearlyOrdinalNumber;
        private readonly QueueMEConfiguration? _queueMEConfiguration;
        private readonly ftSignaturCreationUnitME? _scuME;

        public SignatureItemFactory(ReceiptRequest? request = null,  ComputeIICResponse? computeIICResponse = null, ulong? yearlyOrdinalNumber = null, ftSignaturCreationUnitME? scuME = null, QueueMEConfiguration? queueMEConfiguration = null)
        {
            _request = request;
            _computeIICResponse = computeIICResponse;
            _scuME = scuME;
            _queueMEConfiguration = queueMEConfiguration;
            _yearlyOrdinalNumber = yearlyOrdinalNumber;
        }

        public IEnumerable<SignaturItem> CreateSignatures()
        {
            var signatureItems = new List<SignaturItem>();

            if (_computeIICResponse is not null)
            {
                signatureItems.Add(new SignaturItem
                {
                    Caption = "Taxpayer identifying code (IIC/IKOF)",
                    Data = _computeIICResponse.IIC,
                    ftSignatureFormat = 0x01,
                    ftSignatureType = 0x4D45000000000003
                });
                if (_queueMEConfiguration is not null && _scuME is not null && _request is not null && _yearlyOrdinalNumber.HasValue)
                {
                    var qrCode = _queueMEConfiguration.Sandbox ? "https://efitest.tax.gov.me/ic/#/verify" : "https://mapr.tax.gov.me/ic/#/verify";
                    qrCode += $"?iic={_computeIICResponse.IIC}";
                    qrCode += $"&tin={_scuME.IssuerTin}";
                    qrCode += $"&crtd={_request.cbReceiptMoment.ToString(@"yyyy-MM-dd\THH:mm:ss\Z")}";
                    qrCode += $"&ord={_yearlyOrdinalNumber.Value}";
                    qrCode += $"&bu={_scuME.BusinessUnitCode}";
                    qrCode += $"&cr={_scuME.TcrCode}";
                    qrCode += $"&sw={_scuME.SoftwareCode}";
                    qrCode += $"&prc={_request.cbChargeItems.Sum(x => x.Amount)}";
                    

                    signatureItems.Add(new SignaturItem
                    {
                        Caption = "QR code content",
                        Data = qrCode,
                        ftSignatureFormat = 0x03,
                        ftSignatureType = 0x4D45000000000001
                    });
                }
            }

            return signatureItems;
        }
    }
}
