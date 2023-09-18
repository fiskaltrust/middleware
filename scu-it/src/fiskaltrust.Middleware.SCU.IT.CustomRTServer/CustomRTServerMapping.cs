﻿using System.Collections.Generic;
using System;
using fiskaltrust.ifPOS.v1;
using System.Linq;
using Newtonsoft.Json;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.CustomRTServer.Models;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

public static class CustomRTServerMapping
{
    public static (CommercialDocument commercialDocument, FDocument fiscalDocument) CreateAnnuloDocument(ReceiptRequest receiptRequest, QueueIdentification queueIdentification, ReceiptResponse receiptResponse)
    {
        var referenceZNumber = receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceZNumber)?.Data;
        var referenceDocNumber = receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentNumber)?.Data;
        var referenceDateTime = receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data;
        string? refCashUuid = null;
        if (string.IsNullOrEmpty(referenceZNumber) || string.IsNullOrEmpty(referenceDocNumber) || string.IsNullOrEmpty(referenceDateTime))
        {
            referenceZNumber = "-1";
            referenceDocNumber = "-1";
            referenceDateTime = receiptRequest.cbReceiptMoment.ToString("yyyy-MM-dd HH:mm:ss");
            refCashUuid = "ND";
        }

        var fiscalDocument = new FDocument
        {
            document = new DocumentData
            {
                cashuuid = queueIdentification.CashUuId,
                doctype = 5,
                dtime = receiptRequest.cbReceiptMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                docnumber = queueIdentification.LastDocNumber + 1,
                docznumber = queueIdentification.LastZNumber + 1,
                amount = ConvertToFullAmountInt(receiptRequest.cbChargeItems.Sum(x => Math.Abs(x.Amount))),
                fiscalcode = "",
                vatcode = "",
                fiscaloperator = "",
                businessname = null,
                prevSignature = queueIdentification.LastSignature,
                grandTotal = queueIdentification.CurrentGrandTotal,
                referenceClosurenumber = long.Parse(referenceZNumber),
                referenceDocnumber = long.Parse(referenceDocNumber),
                referenceDtime = DateTime.Parse(referenceDateTime).ToString("yyyy-MM-dd 00:00:00"),
                referenceCashuuid = refCashUuid
            },
            items = GenerateItemDataForReceiptRequest(receiptRequest, queueIdentification.LastZNumber + 1, queueIdentification.LastDocNumber + 1),
            taxs = GenerateTaxDataForReceiptRequest(receiptRequest)
        };
        var json = JsonConvert.SerializeObject(fiscalDocument);
        var qrCodeData = GenerateQRCodeData(json, queueIdentification.CashHmacKey);
        var commercialDocument = new CommercialDocument
        {
            fiscalData = json,
            qrData = qrCodeData,
        };


        return (commercialDocument, fiscalDocument);
    }

    public static (CommercialDocument commercialDocument, FDocument fiscalDocument) CreateResoDocument(ReceiptRequest receiptRequest, QueueIdentification queueIdentification, ReceiptResponse receiptResponse)
    {
        var referenceZNumber = receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceZNumber)?.Data;
        var referenceDocNumber = receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentNumber)?.Data;
        var referenceDateTime = receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data;
        var refCashUuid = receiptResponse.ftCashBoxIdentification;
        if (string.IsNullOrEmpty(referenceZNumber) || string.IsNullOrEmpty(referenceDocNumber) || string.IsNullOrEmpty(referenceDateTime))
        {
            referenceZNumber = "0";
            referenceDocNumber = "0";
            referenceDateTime = receiptRequest.cbReceiptMoment.ToString("yyyy-MM-dd HH:mm:ss");
            refCashUuid = "ND";
        }

        var fiscalDocument = new FDocument
        {
            document = new DocumentData
            {
                cashuuid = queueIdentification.CashUuId,
                doctype = 3,
                dtime = receiptRequest.cbReceiptMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                docnumber = queueIdentification.LastDocNumber + 1,
                docznumber = queueIdentification.LastZNumber + 1,
                amount = ConvertToFullAmountInt(receiptRequest.cbChargeItems.Sum(x => Math.Abs(x.Amount))),
                fiscalcode = "",
                vatcode = "",
                fiscaloperator = "",
                businessname = null,
                prevSignature = queueIdentification.LastSignature,
                grandTotal = queueIdentification.CurrentGrandTotal,
                referenceClosurenumber = long.Parse(referenceZNumber),
                referenceDocnumber = long.Parse(referenceDocNumber),
                referenceDtime = DateTime.Parse(referenceDateTime).ToString("yyyy-MM-dd 00:00:00"),
                referenceCashuuid = refCashUuid
            },
            items = GenerateItemDataForReceiptRequest(receiptRequest, queueIdentification.LastZNumber + 1, queueIdentification.LastDocNumber + 1),
            taxs = GenerateTaxDataForReceiptRequest(receiptRequest)
        };
        var json = JsonConvert.SerializeObject(fiscalDocument);
        var qrCodeData = GenerateQRCodeData(json, queueIdentification.CashHmacKey);
        var commercialDocument = new CommercialDocument
        {
            fiscalData = json,
            qrData = qrCodeData,
        };


        return (commercialDocument, fiscalDocument);
    }

    public static (CommercialDocument commercialDocument, FDocument fiscalDocument) GenerateFiscalDocument(ReceiptRequest receiptRequest, QueueIdentification queueIdentification) => GenerateFiscalDocument(receiptRequest, queueIdentification, 1);

    public static (CommercialDocument commercialDocument, FDocument fiscalDocument) GenerateFiscalDocument(ReceiptRequest receiptRequest, QueueIdentification queueIdentification, int docType)
    {
        var fiscalDocument = new FDocument
        {
            document = new DocumentData
            {
                cashuuid = queueIdentification.CashUuId,
                doctype = docType,
                dtime = receiptRequest.cbReceiptMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                docnumber = queueIdentification.LastDocNumber + 1,
                docznumber = queueIdentification.LastZNumber + 1,
                amount = ConvertToFullAmountInt(receiptRequest.cbChargeItems.Sum(x => Math.Abs(x.Amount))),
                fiscalcode = "",
                vatcode = "",
                fiscaloperator = "",
                businessname = null,
                prevSignature = queueIdentification.LastSignature,
                grandTotal = queueIdentification.CurrentGrandTotal,
                referenceClosurenumber = -1,
                referenceDocnumber = -1,
                referenceDtime = null,
            },
            items = GenerateItemDataForReceiptRequest(receiptRequest, queueIdentification.LastZNumber + 1, queueIdentification.LastDocNumber + 1),
            taxs = GenerateTaxDataForReceiptRequest(receiptRequest)
        };
        var json = JsonConvert.SerializeObject(fiscalDocument);
        var qrCodeData = GenerateQRCodeData(json, queueIdentification.CashHmacKey);
        var commercialDocument = new CommercialDocument
        {
            fiscalData = json,
            qrData = qrCodeData,
        };


        return (commercialDocument, fiscalDocument);
    }

    public static QrCodeData GenerateQRCodeData(string data, string key)
    {
        var qrCode = new QrCodeData
        {
            shaMetadata = GlobalTools.GetSHA256(data)
        };
        qrCode.signature = GlobalTools.CreateHMAC(Convert.FromBase64String(key), qrCode.shaMetadata);
        return qrCode;
    }

    public static List<DocumentItemData> GenerateItemDataForReceiptRequest(ReceiptRequest receiptRequest, long zNumber, long receiptNumber)
    {
        var items = new List<DocumentItemData>();
        foreach (var chargeItem in receiptRequest.cbChargeItems)
        {
            if ((chargeItem.ftChargeItemCase & 0x000F_0000) == 0x4_0000)
            {
                items.Add(new DocumentItemData
                {
                    type = DocumentItemDataTaypes.OMAGGIO,
                    description = chargeItem.Description,
                    amount = ConvertToFullAmount(chargeItem.Amount),
                    quantity = ConvertTo1000FullAmount(chargeItem.Quantity),
                    unitprice = ConvertToFullAmount(chargeItem.Amount / chargeItem.Quantity),
                    vatvalue = ConvertToFullAmount(chargeItem.VATRate),
                    paymentid = "",
                    plu = "",
                    department = "",
                    vatcode = GetVatCodeForChargeItemCase(chargeItem.ftChargeItemCase)
                });
            }
            else
            {
                items.Add(new DocumentItemData
                {
                    type = GetTypeForChargeItem(chargeItem),
                    description = chargeItem.Description,
                    amount = ConvertToFullAmount(chargeItem.Amount),
                    quantity = ConvertTo1000FullAmount(chargeItem.Quantity),
                    unitprice = ConvertToFullAmount(chargeItem.Amount / chargeItem.Quantity),
                    vatvalue = ConvertToFullAmount(chargeItem.VATRate),
                    paymentid = "",
                    plu = "",
                    department = "",
                    vatcode = GetVatCodeForChargeItemCase(chargeItem.ftChargeItemCase)
                });
            }
        }
        var totalAmount = receiptRequest.cbChargeItems.Sum(x => Math.Abs(x.Amount));
        var vatAmount = receiptRequest.cbChargeItems.Sum(x => Math.Abs(x.VATAmount ?? 0.0m));
        items.Add(new DocumentItemData
        {
            type = "97",
            description = $"TOTALE  COMPLESSIVO               {totalAmount.ToString(ITConstants.CurrencyFormatter)}",
            amount = ConvertToFullAmount(totalAmount),
            quantity = ConvertTo1000FullAmount(1),
            unitprice = "",
            vatvalue = "",
            paymentid = "",
            plu = "",
            department = ""
        });
        items.Add(new DocumentItemData
        {
            type = "97",
            description = $"DI CUI IVA               {vatAmount.ToString(ITConstants.CurrencyFormatter)}",
            amount = ConvertToFullAmount(vatAmount),
            quantity = ConvertTo1000FullAmount(1),
            unitprice = "",
            vatvalue = "",
            paymentid = "",
            plu = "",
            department = ""
        });
        foreach (var payitem in receiptRequest.cbPayItems)
        {
            items.Add(new DocumentItemData
            {
                type = GetTypeForPayItem(payitem),
                description = payitem.Description,
                amount = ConvertToFullAmount(payitem.Amount),
                quantity = ConvertToFullAmount(payitem.Quantity),
                unitprice = "",
                vatvalue = "",
                paymentid = GetPaymentIdForPayItem(payitem),
                plu = "",
                department = ""
            });
        }
        var payedAmount = receiptRequest.cbPayItems.Sum(x => x.Amount);
        items.Add(new DocumentItemData
        {
            type = "97",
            description = $"IMPORTO PAGATO {payedAmount.ToString(ITConstants.CurrencyFormatter)}",
            amount = ConvertToFullAmount(payedAmount),
            quantity = ConvertTo1000FullAmount(1),
            unitprice = "",
            vatvalue = "",
            paymentid = "",
            plu = "",
            department = ""
        });
        items.Add(new DocumentItemData
        {
            type = "97",
            description = $"30/08/2023 12:01       DOC.N.{zNumber.ToString().PadLeft(4, '0')}-{receiptNumber.ToString().PadLeft(4, '0')}",
            amount = ConvertToFullAmount(payedAmount),
            quantity = ConvertTo1000FullAmount(1),
            unitprice = "",
            vatvalue = "",
            paymentid = "",
            plu = "",
            department = ""
        });
        return items;
    }

    public static string GetTypeForChargeItem(ChargeItem chargeItem) => chargeItem.ftChargeItemCase switch
    {
        _ => DocumentItemDataTaypes.VENDITA,
    };

    public static string GetTypeForPayItem(PayItem payItem) => payItem.ftPayItemCase switch
    {
        _ => DocumentItemDataTaypes.PAGAMENTO,
    };

    public static string GetPaymentIdForPayItem(PayItem payItem) => payItem.ftPayItemCase switch
    {
        _ => DocumentItemPaymentIds.CONTANTE
    };

    public static string ConvertTo1000FullAmount(decimal? value) => ((int) (Math.Abs(value ?? 0.0m) * 1000)).ToString();

    public static string ConvertToFullAmount(decimal? value) => ((int) (Math.Abs(value ?? 0.0m) * 100)).ToString();

    public static int ConvertToFullAmountInt(decimal? value) => (int) (Math.Abs(value ?? 0.0m) * 100);

    public static List<DocumentTaxData> GenerateTaxDataForReceiptRequest(ReceiptRequest receiptRequest)
    {
        var items = new List<DocumentTaxData>();
        var groupedItems = receiptRequest.cbChargeItems.GroupBy(x => (GetVatCodeForChargeItemCase(x.ftChargeItemCase), x.VATRate));
        foreach (var item in groupedItems)
        {
            items.Add(new DocumentTaxData
            {
                gross = ConvertToFullAmountInt(item.Sum(x => x.Amount)),
                tax = ConvertToFullAmountInt(item.Sum(x => x.VATAmount ?? 0.0m)),
                vatvalue = ConvertToFullAmountInt(item.Key.VATRate),
                vatcode = item.Key.Item1
            });
        }
        return items;
    }

    public static string GetVatCodeForChargeItemCase(long chargeItemCase) => chargeItemCase switch
    {
        0x4954_2000_0020_0013 => "",
        0x4954_2000_0020_0011 => "",
        0x4954_2000_0020_0012 => "",
        0x4954_2000_0020_0014 => "",
        0x4954_2000_0020_1014 => "N3",
        0x4954_2000_0020_2014 => "N2",
        0x4954_2000_0020_3014 => "N4",
        0x4954_2000_0020_4014 => "N5",
        0x4954_2000_0020_5014 => "N6",
        0x4954_2000_0020_8014 => "N1",
        0x4954_2000_0020_7014 => "VI",
        0x4954_2000_0000_8038 => "N1",
        0x4954_2000_0022_0013 => "",
        0x4954_2000_0022_0011 => "",
        0x4954_2000_0022_0012 => "",
        0x4954_2000_0022_0014 => "",
        0x4954_2000_0022_1014 => "N3",
        0x4954_2000_0022_2014 => "N2",
        0x4954_2000_0022_3014 => "N4",
        0x4954_2000_0022_4014 => "N5",
        0x4954_2000_0022_5014 => "N6",
        0x4954_2000_0022_8014 => "N1",
        0x4954_2000_0022_7014 => "VI",
        0x4954_2000_0021_0013 => "",
        0x4954_2000_0021_0011 => "",
        0x4954_2000_0021_0012 => "",
        0x4954_2000_0021_0014 => "",
        _ => ""
    };
}
