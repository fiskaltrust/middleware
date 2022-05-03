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
            return (chargeItem.ftChargeItemCase & 0x0_0002_0000) == 0x0_0002_0000;
        }

        public static bool IsNoInvestment(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0x0_0004_0000) == 0x0_0004_0000;
        }

        public static bool DiscountIsNotReducingBasePrice(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0x0_0008_0000) == 0x0_0008_0000;
        }

        public static bool IsCashDeposite(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0xFFFF) == 0x0020;
        }

        public static bool IsCashWithdrawal(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0xFFFF) == 0x0021;
        }

        public static decimal GetVatRate(this ChargeItem chargeItem)
        {
            switch (chargeItem.ftChargeItemCase & 0xFFFF)
            {
                case 0x0000:
                case 0x0005:
                case 0x0015:
                case 0x001D:
                    return chargeItem.VATRate;
                case 0x0001:
                case 0x0011:
                case 0x0019:
                    return 21;
                case 0x0002:
                case 0x0012:
                case 0x001A:
                    return 7;
                case 0x0003:
                case 0x0004:
                case 0x0013:
                case 0x0014:
                case 0x001B:
                case 0x001C:
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
                IsInvestment = !chargeItem.IsNoInvestment(),
                Unit = chargeItem.Unit,
                Quantity = chargeItem.Quantity,
                NetUnitPrice = chargeItem.UnitPrice.HasValue ? (decimal) (chargeItem.UnitPrice * (chargeItem.GetVatRate() / 100)) : 0,
                GrossUnitPrice = chargeItem.UnitPrice.HasValue ? (decimal) chargeItem.UnitPrice : 0,
                DiscountPercentage = invoiceItemRequest.DiscountPercentage,
                IsDiscountReducingBasePrice = !chargeItem.DiscountIsNotReducingBasePrice(),
                VatRate = chargeItem.GetVatRate(),
                ExemptFromVatReason = string.IsNullOrEmpty(invoiceItemRequest.ExemptFromVatReason) ? null : (ExemptFromVatReasons)Enum.Parse(typeof(ExemptFromVatReasons), invoiceItemRequest.ExemptFromVatReason),
                GrossAmount = chargeItem.Amount,
            };
            invoiceItem.NetAmount = invoiceItem.NetUnitPrice  * invoiceItem.Quantity;
            invoiceItem.GrossAmount = (invoiceItem.GrossUnitPrice - invoiceItem.NetUnitPrice) * invoiceItem.Quantity;
            if (chargeItem.IsVoucher())
            {
                var voucher = new VoucherItem()
                {
                    NominalValue = chargeItem.Amount,
                    SerialNumbers = invoiceItemRequest.VoucherSerialNumbers.ToList()
                };
                if (!string.IsNullOrEmpty(invoiceItemRequest.VoucherExpirationDate))
                {
                    if (DateTime.TryParseExact(invoiceItemRequest.VoucherExpirationDate, "yyyy-mm-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out var dateValue))
                    {
                        voucher.ExpirationDate = dateValue;
                    }
                }
                invoiceItem.Vouchers = new List<VoucherItem>()
                {
                    voucher
                };
            }
            return invoiceItem;
        }
    }
}
