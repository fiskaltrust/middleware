using System;
using System.Globalization;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v2.me;
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

        public static decimal GetVatRate(this ChargeItem chargeItem)
        {
            switch (chargeItem.ftChargeItemCase & 0xFFFF)
            {
                case 0x0001:
                    return 21;
                case 0x0002:
                    return 7;
                case 0x0003:
                    return 0;
                default:
                    throw new UnkownInvoiceTypeException("ChargeItemCase holds unkown Vat Rate!");
            }
        }

        public static InvoiceItemType GetInvoiceItemType(this ChargeItem chargeItem)
        {
            var invoiceItem = JsonConvert.DeserializeObject<InvoiceItem>(chargeItem.ftChargeItemCaseData);
            var invoiceItemType = new InvoiceItemType()
            {
                N = chargeItem.Description,
                C = chargeItem.ProductBarcode,
                IN = invoiceItem.IN,
                U = chargeItem.Unit,
                Q = (double) chargeItem.Quantity,
                UPB = chargeItem.UnitPrice.HasValue ? (decimal) (chargeItem.UnitPrice * (chargeItem.GetVatRate() / 100)) : 0,
                UPA = chargeItem.UnitPrice.HasValue ? (decimal) chargeItem.UnitPrice : 0,
                R = invoiceItem.R,
                RR = invoiceItem.RR,
                VR = chargeItem.GetVatRate(),
                EX = string.IsNullOrEmpty(invoiceItem.EX) ? null: (ExemptFromVATSType) Enum.Parse(typeof(ExemptFromVATSType), invoiceItem.EX),
                PA = chargeItem.Amount,
            };
            invoiceItemType.PB = invoiceItemType.UPB  * (decimal) invoiceItemType.Q;
            invoiceItemType.VA = (invoiceItemType.UPA - invoiceItemType.UPB) * (decimal) invoiceItemType.Q;
            if (chargeItem.IsVoucher())
            {
                var newDate = DateTime.ParseExact(invoiceItem.VD,
                                  "yyy-MM-dd",
                                   CultureInfo.InvariantCulture);
                invoiceItemType.VD = newDate;
                invoiceItemType.VN = chargeItem.Amount;
                invoiceItemType.VSN = invoiceItem.VSN;
            }
            return invoiceItemType;
        }
    }
}
