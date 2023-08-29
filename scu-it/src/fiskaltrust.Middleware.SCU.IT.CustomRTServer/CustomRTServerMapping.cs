using System.Collections.Generic;
using System;
using fiskaltrust.ifPOS.v1;
using System.Linq;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

public static class CustomRTServerMapping
{
    public static CommercialDocument CreateAnnuloDocument(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, QueueIdentification queueIdentification)
    {
        return GenerateFiscalDocument(receiptRequest, receiptResponse, queueIdentification, 3);
    }

    public static CommercialDocument CreateResoDocument(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, QueueIdentification queueIdentification)
    {
        return GenerateFiscalDocument(receiptRequest, receiptResponse, queueIdentification, 2);
    }

    public static CommercialDocument GenerateFiscalDocument(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, QueueIdentification queueIdentification)
    {
        return GenerateFiscalDocument(receiptRequest, receiptResponse, queueIdentification, 1);
    }

    private static CommercialDocument GenerateFiscalDocument(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, QueueIdentification queueIdentification, int docType)
    {
        var fiscalDocument = new FDocument
        {
            document = new DocumentData
            {
                cashuuid = queueIdentification.CashUuId,
                doctype = docType,
                dtime = receiptRequest.cbReceiptMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                docnumber = int.Parse(receiptResponse.ftReceiptIdentification.Split('#')[0]),
                docznumber = queueIdentification.CurrentZNumber,
                amount = (int) receiptRequest.cbChargeItems.Sum(x => x.Amount) * 100,
                fiscalcode = "",
                vatcode = "",
                fiscaloperator = "",
                businessname = "",
                prevSignature = queueIdentification.LastSignature,
                grandTotal = (int) receiptRequest.cbChargeItems.Sum(x => x.Amount) * 100,
                referenceClosurenumber = 999999,
                referenceDocnumber = 99999,
                referenceDtime = "",
            },
            items = GenerateItemDataForReceiptRequest(receiptRequest),
            taxs = GenerateTaxDataForReceiptRequest(receiptRequest)
        };
        fiscalDocument.document.doctype = 3;
        var qrCodeData = GenerateQRCodeData(fiscalDocument, queueIdentification.CashHmacKey);
        var commercialDocument = new CommercialDocument
        {
            fiscalData = fiscalDocument,
            qrCodeData = qrCodeData,
        };
        return commercialDocument;
    }

    private static QrCodeData GenerateQRCodeData(FDocument document, string key)
    {
        var data = JsonConvert.SerializeObject(document);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
        var keyByte = Encoding.UTF8.GetBytes(key);
        using var hmacsha256 = new HMACSHA256(keyByte);
        var signPayload = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(base64));
        var sign = Convert.ToBase64String(signPayload);
        return new QrCodeData
        {
            shaMetadata = base64,
            signature = sign,
        };
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

    public static string ConvertToFullAmount(decimal? value) => decimal.ToOACurrency(value ?? 0.0m).ToString();

    private static List<DocumentTaxData> GenerateTaxDataForReceiptRequest(ReceiptRequest receiptRequest)
    {
        var items = new List<DocumentTaxData>();
        var groupedItems = receiptRequest.cbChargeItems.GroupBy(x => (GetVatCodeForChargeItemCase(x.ftChargeItemCase), x.VATRate));
        foreach (var item in groupedItems)
        {
            items.Add(new DocumentTaxData
            {
                gross = (int) item.Sum(x => decimal.ToOACurrency(x.Amount)),
                tax = (int) item.Sum(x => decimal.ToOACurrency(x.VATAmount ?? 0.0m)),
                vatvalue = (int) decimal.ToOACurrency(item.Key.VATRate),
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
