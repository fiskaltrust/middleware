using System;
using System.Globalization;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.SCU.IT.Abstraction;

public static class SignatureFactory
{
    public static SignaturItem[] CreateInitialOperationSignatures()
    {
        return new SignaturItem[] { };
    }
  
    public static SignaturItem[] CreateOutOfOperationSignatures()
    {
        return new SignaturItem[] { };
    }

    public static SignaturItem[] CreateDailyClosingReceiptSignatures(long zRepNumber)
    {
        return new SignaturItem[]
        {
            new SignaturItem
            {
                Caption = "<z-number>",
                Data = zRepNumber.ToString(),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = 0x4954000000000000 & (long) SignatureTypesIT.ZNumber
            },
        };
    }

    public static SignaturItem[] CreatePosReceiptSignatures(long receiptNumber, long zRepNumber, long amount, DateTime receiptDateTime)
    {
        return new SignaturItem[]
        {
            new SignaturItem
            {
                Caption = "<receipt-number>",
                Data = receiptNumber.ToString(),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = 0x4954000000000000 & (long) SignatureTypesIT.ReceiptNumber
            },
            new SignaturItem
            {
                Caption = "<z-number>",
                Data = zRepNumber.ToString(),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = 0x4954000000000000 & (long) SignatureTypesIT.ZNumber
            },
            new SignaturItem
            {
                Caption = "<receipt-amount>",
                Data = amount.ToString(ITConstants.CurrencyFormatter),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = 0x4954000000000000 & (long) SignatureTypesIT.ReceiptAmount
            },
            new SignaturItem
            {
                Caption = "<receipt-timestamp>",
                Data = receiptDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = 0x4954000000000000 & (long) SignatureTypesIT.ReceiptTimestamp
            }
        };
    }
}