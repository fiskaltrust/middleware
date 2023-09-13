using System;
using System.Collections.Generic;
using System.Globalization;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.SCU.IT.Abstraction;

public static class SignatureFactory
{
    public static SignaturItem[] CreateInitialOperationSignatures() => new SignaturItem[] { };

    public static SignaturItem[] CreateOutOfOperationSignatures() => new SignaturItem[] { };

    public static SignaturItem[] CreateZeroReceiptSignatures() => new SignaturItem[] { };

    public static SignaturItem[] CreateDailyClosingReceiptSignatures(long zRepNumber)
    {
        return new SignaturItem[]
        {
            new SignaturItem
            {
                Caption = "<rt-z-number>",
                Data = zRepNumber.ToString().PadLeft(4, '0'),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber
            },
        };
    }

    public static SignaturItem[] CreateVoucherSignatures(NonFiscalRequest nonFiscalRequest)
    {

        var signs = new List<SignaturItem>();
        var cnt = nonFiscalRequest.NonFiscalPrints.Count;
        for (var i = 0; i < cnt; i++)
        {
            var dat = nonFiscalRequest.NonFiscalPrints[i].Data;
            if (dat == "***Voucher***")
            {
                var dat2 = i + 1 < cnt ? nonFiscalRequest.NonFiscalPrints[i + 1].Data : null;
                var isAmount = decimal.TryParse(dat2, NumberStyles.Number, new CultureInfo("it-It", false), out var amnt);
                if (!isAmount)
                {
                    dat2 = i + 2 < cnt ? nonFiscalRequest.NonFiscalPrints[i + 2].Data : null;
                    isAmount = decimal.TryParse(dat2, NumberStyles.Number, new CultureInfo("it-It", false), out amnt);
                }
                if (isAmount)
                {
                    signs.Add(new SignaturItem
                    {
                        Caption = "<voucher>",
                        Data = Math.Abs(amnt).ToString(new NumberFormatInfo
                        {
                            NumberDecimalSeparator = ",",
                            NumberGroupSeparator = "",
                            CurrencyDecimalDigits = 2
                        }),
                        ftSignatureFormat = (long) SignaturItem.Formats.Text,
                        ftSignatureType = ITConstants.BASE_STATE & (long) SignatureTypesIT.RTAmount
                    });
                }
            }
        }
        return signs.ToArray();
    }

    public static List<SignaturItem> CreateDocumentoCommercialeSignatures(POSReceiptSignatureData data)
    {
        var signatureItems = new List<SignaturItem>()
        {
            new SignaturItem
            {
                Caption = "<rt-serialnumber>",
                Data = data.RTSerialNumber,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = ITConstants.BASE_STATE |(long) SignatureTypesIT.RTSerialNumber
            },
            new SignaturItem
            {
                Caption = "<rt-z-number>",
                Data =  data.RTZNumber.ToString().PadLeft(4, '0'),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber
            },
            new SignaturItem
            {
                Caption = "<rt-doc-number>",
                Data = data.RTDocNumber.ToString().PadLeft(4, '0'),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = ITConstants.BASE_STATE |(long) SignatureTypesIT.RTDocumentNumber
            },
             new SignaturItem
            {
                Caption = "<rt-doc-moment>",
                Data = data.RTDocMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = ITConstants.BASE_STATE |(long) SignatureTypesIT.RTDocumentMoment
            },
            new SignaturItem
            {
                Caption = "<rt-document-type>",
                Data = data.RTDocType, // TODO CoNVert
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = ITConstants.BASE_STATE |(long) SignatureTypesIT.RTDocumentType
            }
        };

        if (!string.IsNullOrEmpty(data.RTServerSHAMetadata))
        {
            signatureItems.Add(new SignaturItem
            {
                Caption = "<rt-server-shametadata>",
                Data = data.RTServerSHAMetadata,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTServerShaMetadata
            });
        }

        if (!string.IsNullOrEmpty(data.RTCodiceLotteria))
        {
            signatureItems.Add(new SignaturItem
            {
                Caption = "<rt-lottery-id>",
                Data = data.RTCodiceLotteria,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTLotteryID
            });
        }

        if (!string.IsNullOrEmpty(data.RTCustomerID))
        {
            signatureItems.Add(new SignaturItem
            {
                Caption = "<rt-customer-id>",
                Data = data.RTCustomerID,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTCustomerID
            });
        }

        if (data.RTReferenceZNumber.HasValue && data.RTReferenceDocNumber.HasValue && data.RTReferenceDocMoment.HasValue) // TODO WE NEED TO CHECK THAT
        {
            signatureItems.Add(new SignaturItem
            {
                Caption = "<rt-reference-z-number>",
                Data = data.RTReferenceZNumber.Value.ToString().PadLeft(4, '0'),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber
            });
            signatureItems.Add(new SignaturItem
            {
                Caption = "<rt-reference-doc-number>",
                Data = data.RTReferenceDocNumber.Value.ToString().PadLeft(4, '0'),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber
            });
            signatureItems.Add(new SignaturItem
            {
                Caption = "<rt-reference-doc-moment>",
                Data = data.RTReferenceDocMoment.Value.ToString("yyyy-MM-dd"),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment
            });
        }
        return signatureItems;
    }
}
