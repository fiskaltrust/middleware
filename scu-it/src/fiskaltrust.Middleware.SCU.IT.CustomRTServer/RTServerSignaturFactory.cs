using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.ifPOS.v1;
using System.Collections.Generic;
using System.Linq;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

public static class RTServerSignaturFactory
{
    public const long BASE_STATE = 0x4954_2000_0000_0000;

    public static List<SignaturItem> CreateDocumentoCommercialeSignatures(DocumentData document, CommercialDocument commercialDocument, string rtSerialNumber, string? codiceLotteria = null, string? customerIdentification = null)
    {
        var signatureItems = new List<SignaturItem>()
        {
            new SignaturItem
            {
                Caption = "<rt-serialnumber>",
                Data = rtSerialNumber,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = BASE_STATE |(long) SignatureTypesIT.RTSerialNumber
            },
            new SignaturItem
            {
                Caption = "<rt-z-number>",
                Data =  document.docznumber.ToString(),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = BASE_STATE | (long) SignatureTypesIT.RTZNumber
            },
            new SignaturItem
            {
                Caption = "<rt-doc-number>",
                Data = document.docnumber.ToString(),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = BASE_STATE |(long) SignatureTypesIT.RTDocumentNumber
            },
             new SignaturItem
            {
                Caption = "<rt-doc-moment>",
                Data = document.dtime,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = BASE_STATE |(long) SignatureTypesIT.RTDocumentMoment
            },
            new SignaturItem
            {
                Caption = "<rt-document-type>",
                Data = document.doctype.ToString(),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = BASE_STATE |(long) SignatureTypesIT.RTDocumentType
            },
            new SignaturItem
            {
                Caption = "<rt-server-shametadata>",
                Data = commercialDocument.qrData.shaMetadata,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = BASE_STATE |(long) SignatureTypesIT.CustomRTServerShaMetadata
            }
        };

        if (!string.IsNullOrEmpty(codiceLotteria))
        {
            signatureItems.Add(new SignaturItem
            {
                Caption = "<rt-lottery-id>",
                Data = codiceLotteria,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = BASE_STATE | (long) SignatureTypesIT.RTLotteryID
            });
        }

        if (!string.IsNullOrEmpty(customerIdentification))
        {
            signatureItems.Add(new SignaturItem
            {
                Caption = "<rt-customer-id>",
                Data = customerIdentification,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = BASE_STATE | (long) SignatureTypesIT.RTCustomerID
            });
        }

        if (document.doctype != 1)
        {
            signatureItems.Add(new SignaturItem
            {
                Caption = "<rt-reference-z-number>",
                Data = document.referenceClosurenumber.ToString(),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber
            });
            signatureItems.Add(new SignaturItem
            {
                Caption = "<rt-reference-doc-number>",
                Data = document.referenceDocnumber.ToString(),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber
            });
            signatureItems.Add(new SignaturItem
            {
                Caption = "<rt-reference-doc-moment>",
                Data = document.referenceDtime,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment
            });
        }
        return signatureItems.ToList();
    }
}
