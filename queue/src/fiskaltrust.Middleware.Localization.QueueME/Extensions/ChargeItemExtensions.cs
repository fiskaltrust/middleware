using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueME.Extensions
{
    public static class ChargeItemExtensions
    {
        public static bool IsVoucher(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0x0_0001_0000) == 0x0_0001_0000;
        }

        public static bool IsExportGood(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0xFFFF) == 0x0004;
        }

        public static decimal GetVatRate(this ChargeItem chargeItem)
        {
            switch (chargeItem.ftChargeItemCase & 0xFFFF)
            {
                case 0x0001:
                    return 21;
                case 0x0002:
                    return 7;
                case 0x0003:
                case 0x0004:
                    return 0;
                default:
                    throw new UnkownInvoiceTypeException("ChargeItemCase holds unkown Vat Rate!");
            }
        }

        public static InvoiceItem GetInvoiceItem(this ChargeItem chargeItem)
        {
            var invoiceItemRequest = string.IsNullOrEmpty(chargeItem.ftChargeItemCaseData) ? new InvoiceItemRequest () : JsonConvert.DeserializeObject<InvoiceItemRequest>(chargeItem.ftChargeItemCaseData);
            var invoiceItem = new InvoiceItem()
            {
                Name = chargeItem.Description,
                Code = chargeItem.ProductBarcode,
                IsInvestment = invoiceItemRequest.IsInvestment,
                Unit = chargeItem.Unit,
                Quantity = chargeItem.Quantity,
                NetUnitPrice = chargeItem.UnitPrice.HasValue ? (decimal) (chargeItem.UnitPrice * (chargeItem.GetVatRate() / 100)) : 0,
                GrossUnitPrice = chargeItem.UnitPrice.HasValue ? (decimal) chargeItem.UnitPrice : 0,
                DiscountPercentage = invoiceItemRequest.DiscountPercentage,
                IsDiscountReducingBasePrice = invoiceItemRequest.IsDiscountReducingBasePrice,
                VatRate = chargeItem.GetVatRate(),
                ExemptFromVatReason = string.IsNullOrEmpty(invoiceItemRequest.ExemptFromVatReason) ? null : (ExemptFromVatReasons)Enum.Parse(typeof(ExemptFromVatReasons), invoiceItemRequest.ExemptFromVatReason),
                GrossAmount = chargeItem.Amount,
            };
            invoiceItem.NetAmount = invoiceItem.NetUnitPrice  * invoiceItem.Quantity;
            invoiceItem.GrossAmount = (invoiceItem.GrossUnitPrice - invoiceItem.NetUnitPrice) * invoiceItem.Quantity;
            if (chargeItem.IsVoucher())
            {
                 invoiceItem.Vouchers = new List<VoucherItem>()
                {
                    new VoucherItem()
                    {
                        ExpirationDate = chargeItem.Moment.Value,
                        NominalValue = chargeItem.Amount,
                        SerialNumbers = invoiceItemRequest.VoucherSerialNumbers.ToList()
                    }
                };
            }
            return invoiceItem;
        }
    }
}
