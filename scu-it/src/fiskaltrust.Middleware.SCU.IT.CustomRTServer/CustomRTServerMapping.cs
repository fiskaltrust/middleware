using System.Collections.Generic;
using System;
using fiskaltrust.ifPOS.v1;
using System.Linq;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

public static class CustomRTServerMapping
{
    public static (CommercialDocument commercialDocument, FDocument fiscalDocument) CreateAnnuloDocument(ReceiptRequest receiptRequest, QueueIdentification queueIdentification) => GenerateFiscalDocument(receiptRequest, queueIdentification, 3);

    public static (CommercialDocument commercialDocument, FDocument fiscalDocument) CreateResoDocument(ReceiptRequest receiptRequest, QueueIdentification queueIdentification) => GenerateFiscalDocument(receiptRequest, queueIdentification, 2);

    public static (CommercialDocument commercialDocument, FDocument fiscalDocument) GenerateFiscalDocument(ReceiptRequest receiptRequest, QueueIdentification queueIdentification) => GenerateFiscalDocument(receiptRequest, queueIdentification, 1);

    private static (CommercialDocument commercialDocument, FDocument fiscalDocument) GenerateFiscalDocument(ReceiptRequest receiptRequest, QueueIdentification queueIdentification, int docType)
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
                amount = (int) receiptRequest.cbChargeItems.Sum(x => x.Amount) * 100,
                fiscalcode = "",
                vatcode = "",
                fiscaloperator = "technician",
                businessname = "",
                prevSignature = queueIdentification.LastSignature,
                grandTotal = queueIdentification.CurrentGrandTotal,
                referenceClosurenumber = -1,
                referenceDocnumber = -1,
                referenceDtime = null,
            },
            items = GenerateItemDataForReceiptRequest(receiptRequest),
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

    private static QrCodeData GenerateQRCodeData(string data, string key)
    {
        var qrCode = new QrCodeData
        {
            shaMetadata = GlobalTools.GetSHA256(data)
        };
        qrCode.signature = GlobalTools.CreateHMAC(Convert.FromBase64String(key), qrCode.shaMetadata);
        return qrCode;
    }

    private static List<DocumentItemData> GenerateItemDataForReceiptRequest(ReceiptRequest receiptRequest)
    {
        var items = new List<DocumentItemData>();
        foreach (var chargeItem in receiptRequest.cbChargeItems)
        {
            items.Add(new DocumentItemData
            {
                type = GetTypeForChargeItem(chargeItem),
                description = chargeItem.Description,
                amount = ConvertToFullAmount(chargeItem.Amount),
                quantity = ConvertToFullAmount(chargeItem.Quantity),
                unitprice = ConvertToFullAmount(chargeItem.UnitPrice),
                vatvalue = ConvertToFullAmount(chargeItem.VATRate),
                paymentid = "",
                plu = "",
                department = ""
            });
        }
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
        return items;
    }

    private static string GetTypeForChargeItem(ChargeItem chargeItem)
    {
        return chargeItem.ftChargeItemCase switch
        {
            _ => DocumentItemDataTaypes.VENDITA,
        };
    }

    private static string GetTypeForPayItem(PayItem payItem)
    {
        return payItem.ftPayItemCase switch
        {
            _ => DocumentItemDataTaypes.PAGAMENTO,
        };
    }

    private static string GetPaymentIdForPayItem(PayItem payItem)
    {
        return payItem.ftPayItemCase switch
        {
            _ => DocumentItemPaymentIds.CONTANTE
        };
    }

    public static string ConvertToFullAmount(decimal? value) => ((int) (value ?? 0.0m) * 100).ToString();

    public static int ConvertToFullAmountInt(decimal? value) => (int) ((value ?? 0.0m) * 100);

    private static List<DocumentTaxData> GenerateTaxDataForReceiptRequest(ReceiptRequest receiptRequest)
    {
        var items = new List<DocumentTaxData>();
        var groupedItems = receiptRequest.cbChargeItems.GroupBy(x => (GetVatCodeForChargeItemCase(x.ftChargeItemCase), x.VATRate));
        foreach (var item in groupedItems)
        {
            items.Add(new DocumentTaxData
            {
                gross = ConvertToFullAmountInt(item.Sum(x => x.Amount)),
                tax = ConvertToFullAmountInt(item.Sum(x => x.VATAmount)),
                vatvalue = ConvertToFullAmountInt(item.Key.VATRate),
                vatcode = item.Key.Item1
            });
        }
        return items;
    }

    public static string GetVatCodeForChargeItemCase(long chargeItemCase)
    {
        return chargeItemCase switch
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
}
