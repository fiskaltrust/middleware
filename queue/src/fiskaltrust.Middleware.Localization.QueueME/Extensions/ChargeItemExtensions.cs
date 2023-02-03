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
            return (chargeItem.ftChargeItemCase & 0x0000_0000_000F_0000) == 0x1_0000;
        }

        public static bool IsExportGood(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0x0000_0000_000F_0000) == 0x2_0000;
        }

        public static bool IsNoInvestment(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0x0000_0000_000F_0000) == 0x6_0000;
        }

        public static bool DiscountIsReducingBasePrice(this ChargeItem chargeItem)
        {
            return (chargeItem.ftChargeItemCase & 0x0000_0000_000F_0000) == 0x7_0000;
        }
        public static decimal GetVatRate(this ChargeItem chargeItem)
        {
            switch (chargeItem.ftChargeItemCase & 0xFFFF)
            {
                case 0x0000:
                case 0x0005:
                case 0x0015:
                case 0x001D:
                case 0x0026:
                    return chargeItem.VATRate;
                case 0x0001:
                case 0x0011:
                case 0x0019:
                case 0x0022:
                    return 21;
                case 0x0002:
                case 0x0012:
                case 0x001A:
                case 0x0023:
                    return 7;
                case 0x0003:
                case 0x0004:
                case 0x0013:
                case 0x0014:
                case 0x001B:
                case 0x001C:
                case 0x0024:
                case 0x0025:
                    return 0;
                default:
                    throw new UnknownInvoiceTypeException($"The given ChargeItemCase 0x{chargeItem.ftChargeItemCase:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.");
            }
        }

        public static InvoiceItem GetInvoiceItem(this ChargeItem chargeItem, bool isVoid)
        {
            var invoiceItemRequest = string.IsNullOrEmpty(chargeItem.ftChargeItemCaseData) ? new InvoiceItemRequest() : JsonConvert.DeserializeObject<InvoiceItemRequest>(chargeItem.ftChargeItemCaseData);

            var invoiceItem = new InvoiceItem
            {
                Name = chargeItem.Description,
                Code = string.IsNullOrEmpty(chargeItem.ProductBarcode) ? null : chargeItem.ProductBarcode,
                IsInvestment = !chargeItem.IsNoInvestment(),
                Unit = !string.IsNullOrEmpty(chargeItem.Unit) ? chargeItem.Unit : "unit",
                Quantity = chargeItem.Quantity,
                NetUnitPrice = chargeItem.UnitPrice.HasValue ? (decimal) (chargeItem.UnitPrice / (1 + (chargeItem.GetVatRate() / 100))) : 0,
                GrossUnitPrice = chargeItem.UnitPrice ?? 0,
                DiscountPercentage = invoiceItemRequest.DiscountPercentage,
                IsDiscountReducingBasePrice = chargeItem.DiscountIsReducingBasePrice(),
                VatRate = chargeItem.GetVatRate(),
                ExemptFromVatReason = string.IsNullOrEmpty(invoiceItemRequest.ExemptFromVatReason) ? null : (ExemptFromVatReasons) Enum.Parse(typeof(ExemptFromVatReasons), invoiceItemRequest.ExemptFromVatReason),
                GrossAmount = chargeItem.Amount,
                NetAmount = chargeItem.Amount / (1 + (chargeItem.GetVatRate() / 100))
            };
            if (chargeItem.DiscountIsReducingBasePrice() && invoiceItemRequest.DiscountPercentage is > 0)
            {
                if (!chargeItem.UnitPrice.HasValue)
                {
                    invoiceItem.NetUnitPrice = chargeItem.Amount / Math.Abs(chargeItem.Quantity) / (1 + (chargeItem.GetVatRate() / 100));
                }
                var unitPriceWithDiscount = (decimal) (invoiceItem.NetUnitPrice * (100 - invoiceItemRequest.DiscountPercentage) / 100);
                invoiceItem.GrossUnitPrice = unitPriceWithDiscount * (1 + (chargeItem.GetVatRate() / 100));
                invoiceItem.GrossAmount = invoiceItem.GrossUnitPrice * Math.Abs(invoiceItem.Quantity);
                invoiceItem.NetAmount = invoiceItem.GrossAmount / (1 + (chargeItem.GetVatRate() / 100));
            }
            if (invoiceItem.Quantity < 0 || isVoid)
            {
                invoiceItem.GrossAmount *= -1;
                invoiceItem.NetAmount *= -1;
                if (isVoid)
                {
                    invoiceItem.Quantity *= -1;
                }
            }

            if (!chargeItem.IsVoucher())
            {
                return invoiceItem;
            }

            var voucher = new VoucherItem
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
            invoiceItem.Vouchers = new List<VoucherItem>
            {
                voucher
            };
            return invoiceItem;
        }
    }
}
