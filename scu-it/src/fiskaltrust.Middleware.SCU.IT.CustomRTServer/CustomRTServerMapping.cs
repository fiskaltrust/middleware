using System.Collections.Generic;
using System;
using fiskaltrust.ifPOS.v1;
using System.Linq;
using Newtonsoft.Json;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.CustomRTServer.Models;
using System.Globalization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

#pragma warning disable

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
        (var totalAmount, var vatAmount, var items) = GenerateItemDataForReceiptRequest(receiptRequest, queueIdentification.LastZNumber + 1, queueIdentification.LastDocNumber + 1);
        var fiscalDocument = new FDocument
        {
            document = new DocumentData
            {
                cashuuid = queueIdentification.CashUuId,
                doctype = 5,
                dtime = receiptRequest.cbReceiptMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                docnumber = queueIdentification.LastDocNumber + 1,
                docznumber = queueIdentification.LastZNumber + 1,
                amount = ConvertToFullAmountInt(totalAmount),
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
            items = items,
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
        (var totalAmount, var vatAmount, var items) = GenerateItemDataForReceiptRequest(receiptRequest, queueIdentification.LastZNumber + 1, queueIdentification.LastDocNumber + 1);

        var fiscalDocument = new FDocument
        {
            document = new DocumentData
            {
                cashuuid = queueIdentification.CashUuId,
                doctype = 3,
                dtime = receiptRequest.cbReceiptMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                docnumber = queueIdentification.LastDocNumber + 1,
                docznumber = queueIdentification.LastZNumber + 1,
                amount = ConvertToFullAmountInt(totalAmount),
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
            items = items,
            taxs = GenerateTaxDataForReceiptRequest(receiptRequest)
        };
        var json = JsonConvert.SerializeObject(fiscalDocument);
        var qrCodeData = GenerateQRCodeData(json, queueIdentification.CashHmacKey);
        var commercialDocument = new CommercialDocument
        {
            fiscalData = json,
            qrData = qrCodeData,
        };

#pragma warning disable
        var data = JsonConvert.SerializeObject(fiscalDocument);
        return (commercialDocument, fiscalDocument);
    }

    public static (CommercialDocument commercialDocument, FDocument fiscalDocument) GenerateFiscalDocument(ReceiptRequest receiptRequest, QueueIdentification queueIdentification) => GenerateFiscalDocument(receiptRequest, queueIdentification, 1);

    public static (CommercialDocument commercialDocument, FDocument fiscalDocument) GenerateFiscalDocument(ReceiptRequest receiptRequest, QueueIdentification queueIdentification, int docType)
    {
        (var totalAmount, var vatAmount, var items) = GenerateItemDataForReceiptRequest(receiptRequest, queueIdentification.LastZNumber + 1, queueIdentification.LastDocNumber + 1);
        var fiscalDocument = new FDocument
        {
            document = new DocumentData
            {
                cashuuid = queueIdentification.CashUuId,
                doctype = docType,
                dtime = receiptRequest.cbReceiptMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                docnumber = queueIdentification.LastDocNumber + 1,
                docznumber = queueIdentification.LastZNumber + 1,
                amount = ConvertToFullAmountInt(totalAmount),
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
            items = items,
            taxs = GenerateTaxDataForReceiptRequest(receiptRequest)
        };
        var json = JsonConvert.SerializeObject(fiscalDocument);
        var qrCodeData = GenerateQRCodeData(json, queueIdentification.CashHmacKey);
        var commercialDocument = new CommercialDocument
        {
            fiscalData = json,
            qrData = qrCodeData,
        };

#pragma warning disable
        var data = JsonConvert.SerializeObject(fiscalDocument);

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

    public static bool InverseAmount(ReceiptRequest receiptRequest, ChargeItem chargeItem) => receiptRequest.IsRefund() || receiptRequest.IsVoid() || chargeItem.IsRefund() || chargeItem.IsVoid();

    public static (decimal totalAmount, decimal vatAmount, List<DocumentItemData>) GenerateItemDataForReceiptRequest(ReceiptRequest receiptRequest, long zNumber, long receiptNumber)
    {
        var items = new List<DocumentItemData>();
        var totalAmount = 0m;
        var totalVatAmount = 0m;
        foreach (var chargeItem in receiptRequest.cbChargeItems)
        {
            var amount = GetGrossAmount(receiptRequest, chargeItem);
            var vatAmount = GetVATAmount(chargeItem);
            totalAmount += amount;
            totalVatAmount += vatAmount;
            items.Add(new DocumentItemData
            {
                type = GetTypeForChargeItem(chargeItem),
                description = GenerateChargeItemCaseDescription(chargeItem),
                amount = ConvertToFullAmount(amount),
                quantity = ConvertTo1000FullAmount(GetQuantity(chargeItem)),
                unitprice = ConvertToFullAmount(GetUnitPrice(chargeItem)),
                vatvalue = ConvertToFullAmount(chargeItem.VATRate),
                paymentid = "",
                plu = "",
                department = "",
                vatcode = GetVatCodeForChargeItemCase(chargeItem.ftChargeItemCase)
            });
        }
        items.Add(new DocumentItemData
        {
            type = DocumentItemDataTaypes.DESCRITTIVA,
            description = $"TOTALE  COMPLESSIVO               {ConvertToString(totalAmount)}",
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
            type = DocumentItemDataTaypes.DESCRITTIVA,
            description = $"DI CUI IVA               {ConvertToString(totalVatAmount)}",
            amount = ConvertToFullAmount(totalVatAmount),
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
                description = GeneratePayItemCaseDescription(payitem),
                amount = ConvertToFullAmount(payitem.Amount),
                quantity = ConvertTo1000FullAmount(payitem.Quantity),
                unitprice = "",
                vatvalue = "",
                paymentid = GetPaymentIdForPayItem(payitem),
                plu = "",
                department = ""
            });
            items.Add(new DocumentItemData
            {
                type = DocumentItemDataTaypes.DESCRITTIVA,
                description = payitem.Description,
                amount = "0",
                quantity = ConvertTo1000FullAmount(1),
                unitprice = "",
                vatvalue = "",
                paymentid = "",
                plu = "",
                department = ""
            });
        }
        var payedAmount = receiptRequest.cbPayItems.Sum(x => x.Amount);
        items.Add(new DocumentItemData
        {
            type = DocumentItemDataTaypes.PAGAMENTO,
            description = $"NON RISCOSSO                        0,00",
            amount = "0",
            quantity = ConvertTo1000FullAmount(1),
            unitprice = "",
            vatvalue = "",
            paymentid = "",
            plu = "",
            department = ""
        });
        items.Add(new DocumentItemData
        {
            type = DocumentItemDataTaypes.PAGAMENTO,
            description = $"RESTO                               0,00",
            amount = "0",
            quantity = ConvertTo1000FullAmount(1),
            unitprice = "",
            vatvalue = "",
            paymentid = "",
            plu = "",
            department = ""
        });
        items.Add(new DocumentItemData
        {
            type = DocumentItemDataTaypes.PAGAMENTO,
            description = $"SCONTO A PAGARE                     0,00",
            amount = "0",
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
            description = $"IMPORTO PAGATO                      {ConvertToString(totalAmount)}",
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
            description = $"{receiptRequest.cbReceiptMoment:dd/MM/yyyy HH:mm:ss}       DOC.N.{zNumber.ToString().PadLeft(4, '0')}-{receiptNumber.ToString().PadLeft(4, '0')}",
            amount = ConvertToFullAmount(payedAmount),
            quantity = ConvertTo1000FullAmount(1),
            unitprice = "",
            vatvalue = "",
            paymentid = "",
            plu = "",
            department = ""
        });
        return (totalAmount, totalVatAmount, items);
    }

    public static string GetTypeForChargeItem(ChargeItem chargeItem) => chargeItem.ftChargeItemCase switch
    {
        _ => DocumentItemDataTaypes.VENDITA,
    };

    public static string GetTypeForPayItem(PayItem payItem) => ((long) payItem.ftPayItemCase & 0xFF) switch
    {
        _ => DocumentItemDataTaypes.PAGAMENTO
    };

    public static string GetPaymentIdForPayItem(PayItem payItem) => ((long) payItem.ftPayItemCase & 0xFF) switch
    {
        0x00 => DocumentItemPaymentIds.CONTANTE,
        0x01 => DocumentItemPaymentIds.CONTANTE,
        0x02 => DocumentItemPaymentIds.CONTANTE,
        0x03 => DocumentItemPaymentIds.CONTANTE,
        0x04 => DocumentItemPaymentIds.ELETTRONICO,
        0x05 => DocumentItemPaymentIds.ELETTRONICO,
        0x06 => DocumentItemPaymentIds.SCONTO_A_PAGARE,
        0x07 => DocumentItemPaymentIds.ELETTRONICO,
        0x08 => DocumentItemPaymentIds.SCONTO_A_PAGARE,
        0x09 => DocumentItemPaymentIds.ELETTRONICO,
        0x0A => DocumentItemPaymentIds.ELETTRONICO,
        0x0B => DocumentItemPaymentIds.ELETTRONICO,
        0x0C => DocumentItemPaymentIds.CONTANTE,
        _ => DocumentItemPaymentIds.CONTANTE
    };

    public static string GetDescriptionForPayItem(PayItem payItem) => ((long) payItem.ftPayItemCase & 0xFF) switch
    {
        0x00 => "PAGAMENTO CONTANTE",
        0x01 => "PAGAMENTO CONTANTE",
        0x02 => "PAGAMENTO CONTANTE",
        0x03 => "PAGAMENTO CONTANTE",
        0x04 => "PAGAMENTO ELETTRONICO",
        0x05 => "PAGAMENTO ELETTRONICO",
        0x06 => "SCONTO A PAGARE",
        0x07 => "PAGAMENTO ELETTRONICO",
        0x08 => "SCONTO A PAGARE",
        0x09 => "PAGAMENTO ELETTRONICO",
        0x0A => "PAGAMENTO ELETTRONICO",
        0x0B => "PAGAMENTO ELETTRONICO",
        0x0C => "PAGAMENTO CONTANTE",
        _ => "PAGAMENTO CONTANTE"
    };

    public static string ConvertTo1000FullAmount(decimal value) => ((int) (value * 1000)).ToString();

    public static string ConvertToFullAmount(decimal value) => ((int) (value * 100)).ToString();

    public static int ConvertToFullAmountInt(decimal value) => (int) (value * 100);

    public static List<DocumentTaxData> GenerateTaxDataForReceiptRequest(ReceiptRequest receiptRequest)
    {
        var items = new List<DocumentTaxData>();
        var groupedItems = receiptRequest.cbChargeItems.GroupBy(x => (GetVatCodeForChargeItemCase(x.ftChargeItemCase), x.VATRate));
        foreach (var chargeItems in groupedItems)
        {
            var tax = 0.0m;
            var gross = 0.0m;
            foreach (var chargeItem in chargeItems)
            {
                tax += GetVATAmount(chargeItem);
                gross += GetGrossAmount(receiptRequest, chargeItem);
            }

            var taxData = new DocumentTaxData
            {
                gross = ConvertToFullAmountInt(gross),
                tax = ConvertToFullAmountInt(tax),
                vatvalue = ConvertToFullAmountInt(chargeItems.Key.VATRate),
                vatcode = chargeItems.Key.Item1,
                additional_tax_data = new List<AdditionalTaxData>()
            };
            items.Add(taxData);
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

    public static string GeneratePayItemCaseDescription(PayItem payItem)
    {
        var payItemDesc = GetDescriptionForPayItem(payItem);
        var payItemAmount = ConvertToString(payItem.Amount);
        var lengthRest = 40 - payItemDesc.Length + payItemAmount.Length;
        return $"{payItemDesc.PadRight(lengthRest, ' ')}{payItemAmount}";
    }

    public static string GenerateChargeItemCaseDescription(ChargeItem chargeItem)
    {
        var chargeItemVatRate = "";
        if (chargeItem.VATRate > 0)
        {
            chargeItemVatRate = $"{chargeItem.VATRate}%";
        }
        else
        {
            var nature = GetVatCodeForChargeItemCase(chargeItem.ftChargeItemCase);
            chargeItemVatRate = $"{nature}*";
        }
        var chargeitemDesc = chargeItem.Description.TruncateLongString(20);
        var charegItemPrice = ConvertToString(chargeItem.Amount);
        return $"{chargeitemDesc.PadRight(20, ' ')}{chargeItemVatRate.PadLeft(6, ' ')}{charegItemPrice.PadLeft(14, ' ')}";
    }

    public static string ConvertToString(decimal value)
    {
        var info = CultureInfo.InvariantCulture.Clone() as CultureInfo;
        info.NumberFormat.NumberDecimalSeparator = ",";

        return value.ToString($"F2", info);
    }

    public static string TruncateLongString(this string str, int maxLength)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return str.Substring(0, Math.Min(str.Length, maxLength));
    }

    public static decimal GetQuantity(ChargeItem chargeItem) => Math.Abs(chargeItem.Quantity);

    public static decimal GetUnitPrice(ChargeItem chargeItem) => Math.Abs(chargeItem.Amount / chargeItem.Quantity);

    public static decimal GetGrossAmount(ReceiptRequest receiptRequest, ChargeItem chargeItem) => InverseAmount(receiptRequest, chargeItem) ? Math.Abs(chargeItem.Amount) : chargeItem.Amount;

    public static decimal GetVATAmount(ChargeItem chargeItem) => (decimal) (chargeItem.VATAmount.HasValue ? chargeItem.VATAmount : Math.Round((chargeItem.Amount - (chargeItem.Amount / (1m + (chargeItem.VATRate / 100m)))), 2, MidpointRounding.AwayFromZero));
}