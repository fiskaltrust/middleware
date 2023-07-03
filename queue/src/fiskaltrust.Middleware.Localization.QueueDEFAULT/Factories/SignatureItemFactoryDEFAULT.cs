using System;
using System.Collections.Generic;
using System.Globalization;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Factories;
using fiskaltrust.Middleware.Localization.QueueDEFAULT;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Factories
{
    public class SignatureItemFactoryDEFAULT : SignatureItemFactory
    {
        public override long CountryBaseState => 0x4954000000000000;

        public SignatureItemFactoryDEFAULT() { 
        }

        protected static NumberFormatInfo CurrencyFormatter = new ()
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = "",
            CurrencyDecimalDigits = 2
        };

        public SignaturItem[] CreatePosReceiptSignatures(DailyClosingResponse dailyClosingResponse)
        {
            return new SignaturItem[]
            {
                new SignaturItem
                {
                    Caption = "<z-number>",
                    Data = dailyClosingResponse.ZRepNumber.ToString(),
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = CountryBaseState & (long) SignatureTypesDEFAULT.ZNumber
                },
                new SignaturItem
                {
                    Caption = "<z-dailyamount>",
                    Data = dailyClosingResponse.DailyAmount.ToString(CurrencyFormatter),
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = CountryBaseState & (long) SignatureTypesDEFAULT.ReceiptAmount
                }
            };
        }

        public SignaturItem[] CreateVoucherSignatures(NonFiscalRequest nonFiscalRequest)
        {

            var signs = new List<SignaturItem>();
            var cnt = nonFiscalRequest.NonFiscalPrints.Count;
            for (var i = 0; i < cnt; i ++ )
            {
                var dat = nonFiscalRequest.NonFiscalPrints[i].Data;
                if ( dat == "***Voucher***")
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
                            ftSignatureType = CountryBaseState & (long) SignatureTypesDEFAULT.ReceiptAmount
                        });
                    }
                }
            }
            return signs.ToArray();
        }

        public SignaturItem []  CreatePosReceiptSignatures(FiscalReceiptResponse fiscalReceiptResponse)
        { 
            return new SignaturItem[]
            {
                new SignaturItem
                {
                    Caption = "<receipt-number>",
                    Data = fiscalReceiptResponse.ReceiptNumber.ToString(),
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = CountryBaseState & (long) SignatureTypesDEFAULT.ReceiptNumber
                },
                new SignaturItem
                {
                    Caption = "<z-number>",
                    Data = fiscalReceiptResponse.ZRepNumber.ToString(),
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = CountryBaseState & (long) SignatureTypesDEFAULT.ZNumber
                },
                new SignaturItem
                {
                    Caption = "<receipt-amount>",
                    Data = fiscalReceiptResponse.Amount.ToString(CurrencyFormatter),
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = CountryBaseState & (long) SignatureTypesDEFAULT.ReceiptAmount
                },
                new SignaturItem
                {
                    Caption = "<receipt-timestamp>",
                    Data = fiscalReceiptResponse.ReceiptDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = CountryBaseState & (long) SignatureTypesDEFAULT.ReceiptTimestamp
                }
            };
        }
    }
}
