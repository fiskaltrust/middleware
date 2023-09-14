using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest
{
    public class POSReceiptSignatureData
    {
        public string RTSerialNumber { get; set; } = string.Empty;
        public long RTZNumber { get; set; }
        public long RTDocNumber { get; set; }
        public DateTime RTDocMoment { get; set; }
        public string RTDocType { get; set; } = string.Empty;
        public string RTCodiceLotteria { get; set; }
        public string RTCustomerID { get; set; }
        public string RTServerSHAMetadata { get; set; }

        public long? RTReferenceZNumber { get; set; }
        public long? RTReferenceDocNumber { get; set; }
        public DateTime? RTReferenceDocMoment { get; set; }

        public static List<SignaturItem> CreateDocumentoCommercialeSignatures(POSReceiptSignatureData data)
        {
            var signatureItems = new List<SignaturItem>()
        {
            new SignaturItem
            {
                Caption = "<rt-serialnumber>",
                Data = data.RTSerialNumber,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = Cases.BASE_STATE |(long) SignatureTypesIT.RTSerialNumber
            },
            new SignaturItem
            {
                Caption = "<rt-z-number>",
                Data =  data.RTZNumber.ToString().PadLeft(4, '0'),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTZNumber
            },
            new SignaturItem
            {
                Caption = "<rt-doc-number>",
                Data = data.RTDocNumber.ToString().PadLeft(4, '0'),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = Cases.BASE_STATE |(long) SignatureTypesIT.RTDocumentNumber
            },
             new SignaturItem
            {
                Caption = "<rt-doc-moment>",
                Data = data.RTDocMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = Cases.BASE_STATE |(long) SignatureTypesIT.RTDocumentMoment
            },
            new SignaturItem
            {
                Caption = "<rt-document-type>",
                Data = data.RTDocType, // TODO CoNVert
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = Cases.BASE_STATE |(long) SignatureTypesIT.RTDocumentType
            }
        };

            if (!string.IsNullOrEmpty(data.RTServerSHAMetadata))
            {
                signatureItems.Add(new SignaturItem
                {
                    Caption = "<rt-server-shametadata>",
                    Data = data.RTServerSHAMetadata,
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTServerShaMetadata
                });
            }

            if (!string.IsNullOrEmpty(data.RTCodiceLotteria))
            {
                signatureItems.Add(new SignaturItem
                {
                    Caption = "<rt-lottery-id>",
                    Data = data.RTCodiceLotteria,
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTLotteryID
                });
            }

            if (!string.IsNullOrEmpty(data.RTCustomerID))
            {
                signatureItems.Add(new SignaturItem
                {
                    Caption = "<rt-customer-id>",
                    Data = data.RTCustomerID,
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTCustomerID
                });
            }

            if (data.RTReferenceZNumber.HasValue && data.RTReferenceDocNumber.HasValue && data.RTReferenceDocMoment.HasValue) // TODO WE NEED TO CHECK THAT
            {
                signatureItems.Add(new SignaturItem
                {
                    Caption = "<rt-reference-z-number>",
                    Data = data.RTReferenceZNumber.Value.ToString().PadLeft(4, '0'),
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber
                });
                signatureItems.Add(new SignaturItem
                {
                    Caption = "<rt-reference-doc-number>",
                    Data = data.RTReferenceDocNumber.Value.ToString().PadLeft(4, '0'),
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber
                });
                signatureItems.Add(new SignaturItem
                {
                    Caption = "<rt-reference-doc-moment>",
                    Data = data.RTReferenceDocMoment.Value.ToString("yyyy-MM-dd"),
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment
                });
            }
            return signatureItems;
        }
    }
}
