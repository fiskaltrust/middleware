using System;
using System.Collections.Generic;
using System.Text;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.storage.V0;

#nullable enable
namespace fiskaltrust.Middleware.Localization.QueueME.Factories
{
    internal class SignatureItemFactory
    {
        private readonly ftQueueItem? _queueItem;
        private readonly ReceiptRequest? _request;
        private readonly ComputeIICResponse? _computeIICResponse;
        private readonly ftQueueME? _queueME;

        public SignatureItemFactory(ftQueueItem? queueItem = null, ReceiptRequest? request = null, ComputeIICResponse? computeIICResponse = null, ftQueueME? queueME = null)
        {
            _queueItem = queueItem;
            _request = request;
            _computeIICResponse = computeIICResponse;
            _queueME = queueME;
        }

        public IEnumerable<SignaturItem> CreateSignatures()
        {
            var signatureItems = new List<SignaturItem>();

            if(_computeIICResponse is not null)
            {
                signatureItems.Add(new SignaturItem
                {
                    Caption = "Taxpayer identifying code (IIC/IKOF)",
                    Data = _computeIICResponse.IIC,
                    ftSignatureFormat = 0x01,
                    ftSignatureType = 0x4D45000000000003
                });
            }

            return signatureItems;
        }
    }
}
