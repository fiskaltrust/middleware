using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueGR.Extensions;

public static class ReceiptRequestExtensions
{
    public static List<(ChargeItem chargeItem, List<ChargeItem> modifiers)> GetGroupedChargeItems(this ReceiptRequest receiptRequest)
    {
        var data = new List<(ChargeItem chargeItem, List<ChargeItem> modifiers)>();
        foreach (var receiptChargeItem in receiptRequest.cbChargeItems)
        {
            if (receiptChargeItem.IsVoucherRedeem() || receiptChargeItem.IsDiscountOrExtra())
            {
                var last = data.LastOrDefault();
                if (last == default)
                {
                    data.Add((receiptChargeItem, new List<ChargeItem>()));
                }
                else
                {
                    last.modifiers.Add(receiptChargeItem);
                }
            }
            else
            {
                data.Add((receiptChargeItem, new List<ChargeItem>()));
            }
        }
        return data;
    }
}