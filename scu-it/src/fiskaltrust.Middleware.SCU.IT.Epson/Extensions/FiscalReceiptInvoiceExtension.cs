using System;
using System.Collections.Generic;
using System.Text;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Epson.Models;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Extensions
{
    public static class FiscalReceiptInvoiceExtension
    {
        public static List<AdjustmentAndMessage> GetAdjustmentAndMessages(this FiscalReceiptInvoice request)
        {
            var adjustmentAndMessages = new List<AdjustmentAndMessage>();
            if (request.PaymentAdjustments != null)
            {
                foreach (var adj in request.PaymentAdjustments)
                {
                    var printRecItemAdjustment = new PrintRecItemAdjustment
                    {
                        Description = adj.Description,
                        AdjustmentType = adj.PaymentAdjustmentType.GetAdjustmentType( adj.Amount),
                        Amount = Math.Abs(adj.Amount),
                        Department = adj.VatGroup ?? 0,
                    };
                    PrintRecMessage? printRecMessage = null;
                    if (!string.IsNullOrEmpty(adj.AdditionalInformation))
                    {
                        printRecMessage = new PrintRecMessage()
                        {
                            Message = adj.AdditionalInformation,
                            MessageType = 4
                        };
                    }
                    adjustmentAndMessages.Add(new()
                    {
                        PrintRecItemAdjustment = printRecItemAdjustment,
                        PrintRecMessage = printRecMessage
                    });
                }
            }
            return adjustmentAndMessages;
        }

        public static List<ItemAndMessage> GetItemAndMessages(this FiscalReceiptInvoice request)
        {
            var itemAndMessages = new List<ItemAndMessage>();
            foreach (var i in request.Items)
            {
                var printRecItem = new PrintRecItem
                {
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Department = i.VatGroup
                };
                PrintRecMessage? printRecMessage = null;
                if (!string.IsNullOrEmpty(i.AdditionalInformation))
                {
                    printRecMessage = new PrintRecMessage()
                    {
                        Message = i.AdditionalInformation,
                        MessageType = 4
                    };
                }
                itemAndMessages.Add(new() { PrintRecItem = printRecItem, PrintRecMessage = printRecMessage });
            }
            return itemAndMessages;
        }
    }
}
