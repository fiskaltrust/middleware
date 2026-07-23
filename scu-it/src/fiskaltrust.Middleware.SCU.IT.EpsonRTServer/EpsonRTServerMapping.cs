using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Text;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.EpsonRTServer.Models;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer
{
    /// <summary>
    /// Builds the Epson RT Server "printerFiscalReceipt" metadata and the surrounding createReceipt command,
    /// and computes the CCDC (SHA-256 fingerprint) chain locally.
    ///
    /// The metadata layout follows the "RT Server Fiscal ePOS Metadata Development Guide" (ch. 3, 4).
    /// VAT-index and payment-type mappings mirror the sibling EpsonRTPrinter SCU because both target the same
    /// Epson fiscal engine.
    /// </summary>
    public static class EpsonRTServerMapping
    {
        // The RT Server expects amounts with a DOT decimal separator (confirmed against an accepted request:
        // unitPrice="1.00", dailyAmount="702.00"). Use the invariant culture for all numeric formatting.
        private static readonly CultureInfo _amountFormat = CultureInfo.InvariantCulture;

        public static FiscalDocumentResult BuildFiscalDocument(
            ReceiptRequest receiptRequest,
            TillState tillState,
            int docType,
            long? referenceZNumber = null,
            long? referenceDocNumber = null,
            DateTime? referenceDocMoment = null,
            string? referenceTillId = null)
        {
            var docNumber = tillState.LastDocNumber + 1;
            var zNumber = tillState.LastZNumber;
            // The RT Server records the LOCAL emission time (Metadata Guide 3.8: dateTime "YYYYMMDDThhmmss"
            // + srtUtcOffset 1=winter/2=summer). cbReceiptMoment is defined as UTC (fiskaltrust interface-doc),
            // so convert with the device-reported offset — correct on any host (the cloud host runs in UTC,
            // where ToLocalTime would be wrong).
            var moment = ToRtServerLocalTime(receiptRequest.cbReceiptMoment, tillState.SrtUtcOffset);

            var recAmount = GetReceiptTotal(receiptRequest, docType);
            var recVat = GetReceiptVat(receiptRequest, docType);
            var payments = GetPaymentTotals(receiptRequest, recAmount);

            var newDailyAmountCents = tillState.CurrentDailyAmount + (docType == 0 ? ToCents(recAmount) : 0);
            var dailyAmount = newDailyAmountCents / 100m;

            var printerFiscalReceipt = BuildPrinterFiscalReceipt(
                receiptRequest, tillState, docType, docNumber, zNumber, moment,
                recAmount, recVat, dailyAmount, payments,
                referenceZNumber, referenceDocNumber, referenceDocMoment, referenceTillId);

            var sectionA = tillState.LastFingerPrint;
            // The CCDC is the SHA-256 of the whole <receipt> element exactly as transmitted, including the
            // <hash> tag (no space before /> to match the device's canonical form). Build it once and reuse
            // the identical string for both hashing and sending — see GlobalTools.ComputeCcdc.
            var receiptElement = $"<receipt><hash fingerPrint=\"{sectionA}\"/>{printerFiscalReceipt}</receipt>";
            var ccdc = GlobalTools.ComputeCcdc(receiptElement);
            var createReceiptXml = $"<createReceipt>{receiptElement}<receiptSecurity><hash fingerPrint=\"{ccdc}\" /></receiptSecurity></createReceipt>";

            return new FiscalDocumentResult
            {
                CreateReceiptXml = createReceiptXml,
                Ccdc = ccdc,
                PreviousFingerPrint = sectionA,
                DocNumber = docNumber,
                ZNumber = zNumber,
                DocType = docType,
                DocMoment = moment,
                AmountCents = ToCents(recAmount),
                LotteryCode = GetLotteryCode(receiptRequest),
                ReferenceZNumber = referenceZNumber,
                ReferenceDocNumber = referenceDocNumber,
                ReferenceDocMoment = referenceDocMoment
            };
        }

        /// <summary>
        /// Converts the receipt moment to the RT Server's local wall-clock time using the device-reported
        /// <paramref name="srtUtcOffset"/> (1 = winter/+1h, 2 = summer/+2h). cbReceiptMoment is defined as UTC
        /// (fiskaltrust interface-doc: "Must be provided in UTC"); since the DateTimeKind is not guaranteed
        /// after deserialization, a Local value is normalised via ToUniversalTime and a zoneless (Unspecified)
        /// value is honoured as UTC per that contract. Returns an Unspecified-kind value so it formats without
        /// a timezone designator, as the metadata format requires.
        /// </summary>
        public static DateTime ToRtServerLocalTime(DateTime moment, int srtUtcOffset)
        {
            var utc = moment.Kind == DateTimeKind.Local
                ? moment.ToUniversalTime()
                : DateTime.SpecifyKind(moment, DateTimeKind.Utc);
            return DateTime.SpecifyKind(utc.AddHours(srtUtcOffset), DateTimeKind.Unspecified);
        }

        private static string BuildPrinterFiscalReceipt(
            ReceiptRequest receiptRequest, TillState tillState, int docType, long docNumber, long zNumber, DateTime moment,
            decimal recAmount, decimal recVat, decimal dailyAmount, PaymentTotals payments,
            long? referenceZNumber, long? referenceDocNumber, DateTime? referenceDocMoment, string? referenceTillId)
        {
            var sb = new StringBuilder();
            sb.Append("<printerFiscalReceipt>");

            // beginFiscalReceipt — for refund (docType 1) / void (docType 3) the reference document must be provided.
            if (docType == 0)
            {
                sb.Append("<beginFiscalReceipt />");
            }
            else
            {
                sb.Append($"<beginFiscalReceipt docType=\"{docType}\"");
                if (referenceZNumber.HasValue) sb.Append($" refZRepNum=\"{referenceZNumber.Value:D4}\"");
                if (referenceDocNumber.HasValue) sb.Append($" refRecNum=\"{referenceDocNumber.Value:D4}\"");
                if (referenceDocMoment.HasValue) sb.Append($" refDateTime=\"{referenceDocMoment.Value:yyyyMMddTHHmmss}\"");
                if (!string.IsNullOrEmpty(referenceTillId)) sb.Append($" refTillID=\"{referenceTillId}\"");
                sb.Append(" />");
            }

            AppendChargeItemLines(sb, receiptRequest, docType);

            foreach (var payItem in receiptRequest.cbPayItems ?? Array.Empty<PayItem>())
            {
                var paymentType = GetEpsonPaymentType(payItem);
                sb.Append("<printRecTotal");
                sb.Append($" description=\"{Escape(payItem.Description)}\"");
                sb.Append($" payment=\"{FormatAmount(Math.Abs(payItem.Amount))}\"");
                sb.Append($" paymentType=\"{paymentType.PaymentType}\" index=\"{paymentType.Index}\" />");
            }

            if ((receiptRequest.cbPayItems?.Length ?? 0) == 0)
            {
                // No pay items: fall back to a single cash payment covering the receipt total. The cash bucket in
                // fiscalInformation is aligned by GetPaymentTotals so the amounts stay consistent (error -38).
                sb.Append($"<printRecTotal description=\"CONTANTE\" payment=\"{FormatAmount(recAmount)}\" paymentType=\"0\" index=\"0\" />");
            }

            // Deferred lottery code (Lotteria degli scontrini) and customer tax code are mutually exclusive
            // (Metadata Guide 3.6.8/3.6.9). Lottery takes precedence.
            var lotteryCode = GetLotteryCode(receiptRequest);
            if (!string.IsNullOrEmpty(lotteryCode))
            {
                sb.Append($"<printRecLotteryID lotteryID=\"{Escape(lotteryCode)}\" />");
            }
            else
            {
                var customerTaxId = receiptRequest.GetCustomer()?.CustomerVATId;
                if (!string.IsNullOrEmpty(customerTaxId))
                {
                    sb.Append($"<printRecTaxID taxID=\"{Escape(customerTaxId)}\" />");
                }
            }

            sb.Append("<fiscalInformation");
            sb.Append($" dailyAmount=\"{FormatAmount(dailyAmount)}\"");
            sb.Append($" tillId=\"{tillState.TillId}\"");
            sb.Append($" zRepNumber=\"{zNumber:D4}\"");
            sb.Append($" recNumber=\"{docNumber:D4}\"");
            sb.Append($" dateTime=\"{moment:yyyyMMddTHHmmss}\"");
            sb.Append($" recAmount=\"{FormatAmount(recAmount)}\"");
            sb.Append($" recVAT=\"{FormatAmount(recVat)}\"");
            sb.Append($" docType=\"{docType}\"");
            // Payment buckets per Metadata Guide 3.8 (XML 7.1 attributes; the deprecated noPayAmount is not used).
            // Semantics validated against the device (-39 otherwise): the buckets contain the TENDERED amounts,
            // changeAmount the change due, and paidAmount the NET amount paid (tendered - change == recAmount).
            var changeAmount = Math.Max(0, payments.Paid - recAmount);
            var paidAmount = payments.Paid - changeAmount;
            sb.Append($" cashAmount=\"{FormatAmount(payments.Cash)}\"");
            sb.Append($" checkAmount=\"{FormatAmount(payments.Check)}\"");
            sb.Append($" ePayAmount=\"{FormatAmount(payments.EPay)}\"");
            sb.Append($" ticketAmount=\"{FormatAmount(payments.Ticket)}\"");
            if (payments.TicketNum > 0)
            {
                sb.Append($" ticketNum=\"{payments.TicketNum}\"");
            }
            sb.Append($" changeAmount=\"{FormatAmount(changeAmount)}\"");
            sb.Append($" paidAmount=\"{FormatAmount(paidAmount)}\"");
            sb.Append($" discountPayment=\"{FormatAmount(payments.Discount)}\"");
            sb.Append($" noPayAmountGoods=\"{FormatAmount(payments.NoPayGoods)}\"");
            sb.Append($" noPayAmountServices=\"{FormatAmount(payments.NoPayServices)}\"");
            sb.Append($" noPayAmountInvoices=\"{FormatAmount(payments.NoPayInvoices)}\"");
            sb.Append($" noPayAmountSSN=\"{FormatAmount(payments.NoPaySSN)}\"");
            if (!string.IsNullOrEmpty(tillState.RTServerSerialNumber))
            {
                sb.Append($" rtSerialNumber=\"{tillState.RTServerSerialNumber}\"");
            }
            sb.Append($" srtUtcOffset=\"{tillState.SrtUtcOffset}\"");
            sb.Append(" />");

            sb.Append("<endFiscalReceipt />");
            sb.Append("</printerFiscalReceipt>");
            return sb.ToString();
        }

        public static string GetLotteryCode(ReceiptRequest receiptRequest)
            => receiptRequest.GetLotteryData()?.servizi_lotteriadegliscontrini_gov_it?.codicelotteria ?? string.Empty;

        /// <summary>
        /// Emits the item lines, mapping each charge item to the correct Metadata-Guide tag: sale (printRecItem),
        /// item void/storno (printRecItemVoid), item discount / single-use voucher (printRecItemAdjustment),
        /// descriptive line (printRecMessage) and subtotal discount/surcharge (printRecSubtotalAdjustment,
        /// emitted after the item lines). Refund/void documents (docType != 0) use plain positive sale lines,
        /// matching the guide's refund/void examples. Mirrors the item semantics of the sibling EpsonRTPrinter SCU.
        /// </summary>
        private static void AppendChargeItemLines(StringBuilder sb, ReceiptRequest receiptRequest, int docType)
        {
            var items = receiptRequest.cbChargeItems ?? Array.Empty<ChargeItem>();

            if (docType != 0)
            {
                foreach (var ci in items)
                {
                    AppendItem("printRecItem", sb, ci, Math.Abs(ci.Quantity), AbsUnitPrice(ci));
                }
                return;
            }

            var subtotalAdjustments = new StringBuilder();
            foreach (var ci in items)
            {
                if (ci.IsSubtotalDiscount())
                {
                    AppendSubtotalAdjustment(subtotalAdjustments, 1, ci);
                }
                else if (ci.IsSubtotalSurcharge())
                {
                    AppendSubtotalAdjustment(subtotalAdjustments, 6, ci);
                }
                else if (ci.Amount == 0 || ci.Quantity == 0)
                {
                    sb.Append($"<printRecMessage message=\"{Escape(ci.Description)}\" />");
                }
                else if (ci.IsVoid())
                {
                    AppendItem("printRecItemVoid", sb, ci, Math.Abs(ci.Quantity), AbsUnitPrice(ci));
                }
                else if (ci.IsTip() || ci.IsMultiUseVoucher())
                {
                    // Tip / multi-use voucher: outside the taxable base -> a Non soggetta (NS) sale line,
                    // mirroring the dedicated non-VAT department 11 of the EpsonRTPrinter / CustomRTPrinter SCUs.
                    AppendItem("printRecItem", sb, ci, Math.Abs(ci.Quantity), AbsUnitPrice(ci));
                }
                else if (ci.IsSingleUseVoucher() && ci.Amount < 0)
                {
                    AppendItemAdjustment(sb, 12, ci); // single-use voucher (buono monouso)
                }
                else if (ci.Amount < 0)
                {
                    AppendItemAdjustment(sb, 3, ci); // discount on item (sconto)
                }
                else
                {
                    AppendItem("printRecItem", sb, ci, ci.Quantity, ci.Quantity == 0 ? 0 : ci.Amount / ci.Quantity);
                }
            }
            sb.Append(subtotalAdjustments);
        }

        private static void AppendItem(string tag, StringBuilder sb, ChargeItem ci, decimal quantity, decimal unitPrice)
        {
            sb.Append($"<{tag}");
            sb.Append($" description=\"{Escape(ci.Description)}\"");
            sb.Append($" quantity=\"{FormatQuantity(quantity)}\"");
            sb.Append($" unitPrice=\"{FormatAmount(unitPrice)}\"");
            sb.Append($" vatID=\"{ResolveItemVatId(ci)}\"");
            sb.Append(" type=\"B\" ateco=\"0\" />");
        }

        private static void AppendItemAdjustment(StringBuilder sb, int adjustmentType, ChargeItem ci)
        {
            sb.Append("<printRecItemAdjustment");
            sb.Append($" adjustmentType=\"{adjustmentType}\"");
            sb.Append($" description=\"{Escape(ci.Description)}\"");
            sb.Append($" amount=\"{FormatAmount(Math.Abs(ci.Amount))}\"");
            sb.Append($" vatID=\"{ResolveItemVatId(ci)}\"");
            sb.Append(" type=\"B\" ateco=\"0\" />");
        }

        private static void AppendSubtotalAdjustment(StringBuilder sb, int adjustmentType, ChargeItem ci)
        {
            sb.Append("<printRecSubtotalAdjustment");
            sb.Append($" adjustmentType=\"{adjustmentType}\"");
            sb.Append($" description=\"{Escape(ci.Description)}\"");
            sb.Append($" amount=\"{FormatAmount(Math.Abs(ci.Amount))}\" />");
        }

        private static decimal AbsUnitPrice(ChargeItem ci)
        {
            var quantity = Math.Abs(ci.Quantity);
            return quantity == 0 ? 0 : Math.Abs(ci.Amount) / quantity;
        }

        private static decimal GetReceiptTotal(ReceiptRequest receiptRequest, int docType)
            => (receiptRequest.cbChargeItems ?? Array.Empty<ChargeItem>()).Sum(x => LineNetSign(x, docType) * Math.Abs(x.Amount));

        private static decimal GetReceiptVat(ReceiptRequest receiptRequest, int docType)
            => (receiptRequest.cbChargeItems ?? Array.Empty<ChargeItem>()).Sum(x => LineNetSign(x, docType) * GetVatAmountAbs(x));

        // How a line contributes to the net receipt total: +1 adds, -1 subtracts (discounts/vouchers/voids),
        // 0 = descriptive line. Kept in sync with AppendChargeItemLines so recAmount matches the emitted lines.
        private static int LineNetSign(ChargeItem ci, int docType)
        {
            if (docType != 0) return 1;
            if (ci.Amount == 0 || ci.Quantity == 0) return 0;
            if (ci.IsSubtotalDiscount()) return -1;
            if (ci.IsSubtotalSurcharge()) return 1;
            if (ci.IsVoid()) return -1;
            if (ci.IsTip() || ci.IsMultiUseVoucher()) return 1; // emitted as a positive Non-soggetta sale line
            if (ci.IsSingleUseVoucher() && ci.Amount < 0) return -1;
            if (ci.Amount < 0) return -1;
            return 1;
        }

        private static decimal GetVatAmountAbs(ChargeItem ci)
        {
            if (ci.VATAmount.HasValue)
            {
                return Math.Abs(ci.VATAmount.Value);
            }
            var gross = Math.Abs(ci.Amount);
            return Math.Round(gross - (gross / (1m + (ci.VATRate / 100m))), 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Per-method payment totals used to fill the fiscalInformation buckets (Metadata Guide 3.8):
        /// paymentType 0=cash, 1=cheque, 2=electronic, 3=ticket (+ticket count), 4=not-paid services,
        /// 5=not-paid goods, 6=not-paid invoice, 7=not-paid SSN, 8/9=payment discount.
        /// </summary>
        internal sealed class PaymentTotals
        {
            public decimal Cash;
            public decimal Check;
            public decimal EPay;
            public decimal Ticket;
            public decimal NoPayGoods;
            public decimal NoPayServices;
            public decimal NoPayInvoices;
            public decimal NoPaySSN;
            public decimal Discount;
            public int TicketNum;

            public decimal Paid => Cash + Check + EPay + Ticket + NoPayGoods + NoPayServices + NoPayInvoices + NoPaySSN + Discount;
        }

        private static PaymentTotals GetPaymentTotals(ReceiptRequest receiptRequest, decimal recAmount)
        {
            var totals = new PaymentTotals();
            var payItems = receiptRequest.cbPayItems ?? Array.Empty<PayItem>();
            if (payItems.Length == 0)
            {
                // Keep in sync with the fallback cash printRecTotal emitted in BuildPrinterFiscalReceipt.
                totals.Cash = recAmount;
                return totals;
            }

            foreach (var payItem in payItems)
            {
                var amount = Math.Abs(payItem.Amount);
                switch (GetEpsonPaymentType(payItem).PaymentType)
                {
                    case 0: totals.Cash += amount; break;
                    case 1: totals.Check += amount; break;
                    case 2: totals.EPay += amount; break;
                    case 3:
                        totals.Ticket += amount;
                        totals.TicketNum += Math.Max(1, (int) Math.Abs(payItem.Quantity));
                        break;
                    case 4: totals.NoPayServices += amount; break;
                    case 5: totals.NoPayGoods += amount; break;
                    case 6: totals.NoPayInvoices += amount; break;
                    case 7: totals.NoPaySSN += amount; break;
                    case 8:
                    case 9: totals.Discount += amount; break;
                    default: totals.Cash += amount; break;
                }
            }
            return totals;
        }

        private static long ToCents(decimal value) => (long) Math.Round(value * 100, MidpointRounding.AwayFromZero);

        private static string FormatAmount(decimal value) => value.ToString("F2", _amountFormat);

        private static string FormatQuantity(decimal value) => value.ToString("0.###", _amountFormat);

        private static string Escape(string? value) => SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;

        // VAT index mapping — mirrors EpsonRTPrinter.GetVatGroup (same Epson fiscal engine).
        // Unknown cases throw instead of silently defaulting to a VAT rate: emitting the wrong rate on a
        // fiscal document is worse than failing the receipt. Tips and vouchers are the documented exceptions
        // and are routed through ResolveItemVatId (they carry no own VAT nibble).
        public static int GetVatId(ChargeItem chargeItem)
            => TryGetVatId(chargeItem, out var vatId)
                ? vatId
                : throw new NotSupportedException($"The ftChargeItemCase 0x{chargeItem.ftChargeItemCase:X} has no VAT-index mapping for the Epson RT Server.");

        private static bool TryGetVatId(ChargeItem chargeItem, out int vatId)
        {
            if ((chargeItem.ftChargeItemCase & 0xF) == 0x8)
            {
                switch (chargeItem.ftChargeItemCase & 0xF000)
                {
                    case 0x8000: vatId = 10; return true; // EE - Esclusa
                    case 0x2000: vatId = 11; return true; // NS - Non soggetta
                    case 0x1000: vatId = 12; return true; // NI - Non imponibile
                    case 0x3000: vatId = 13; return true; // ES - Esente
                    case 0x4000: vatId = 14; return true; // RM - Regime del margine
                    case 0x5000: vatId = 15; return true; // AL - Operazione non IVA
                    case 0x0000: vatId = 0; return true;  // not taxable (Esente N4)
                    default: vatId = -1; return false;
                }
            }

            switch (chargeItem.ftChargeItemCase & 0xF)
            {
                case 0x1: vatId = 2; return true;  // 10%
                case 0x2: vatId = 3; return true;  // 4%
                case 0x3: vatId = 1; return true;  // 22%
                case 0x4: vatId = 4; return true;  // 5%
                case 0x7: vatId = 13; return true; // 0%
                default: vatId = -1; return false;
            }
        }

        // Resolves the vatID for an emitted item line. Tips and multi-use vouchers are outside the taxable
        // base (Non soggetta, NS = index 11), mirroring the dedicated non-VAT department 11 of the sibling
        // EpsonRTPrinter / CustomRTPrinter SCUs. Single-use vouchers carry the item's own VAT when specified
        // and fall back to NS when the VAT nibble is absent. Genuine goods stay strict (GetVatId throws).
        private static int ResolveItemVatId(ChargeItem chargeItem)
        {
            if (chargeItem.IsTip() || chargeItem.IsMultiUseVoucher())
            {
                return 11;
            }
            if (chargeItem.IsSingleUseVoucher())
            {
                return TryGetVatId(chargeItem, out var vatId) ? vatId : 11;
            }
            return GetVatId(chargeItem);
        }

        public struct EpsonPaymentType
        {
            public int PaymentType;
            public int Index;
        }

        // Payment-type mapping — mirrors EpsonRTPrinter.GetEpsonPaymentType.
        public static EpsonPaymentType GetEpsonPaymentType(PayItem payItem)
        {
            return (payItem.ftPayItemCase & 0xFF) switch
            {
                0x00 => new EpsonPaymentType { PaymentType = 0, Index = 0 },
                0x01 => new EpsonPaymentType { PaymentType = 0, Index = 0 },
                0x02 => new EpsonPaymentType { PaymentType = 0, Index = 0 },
                0x03 => new EpsonPaymentType { PaymentType = 1, Index = 0 },
                0x04 => new EpsonPaymentType { PaymentType = 2, Index = 1 },
                0x05 => new EpsonPaymentType { PaymentType = 2, Index = 1 },
                0x06 => new EpsonPaymentType { PaymentType = 6, Index = 1 },
                0x07 => new EpsonPaymentType { PaymentType = 5, Index = 0 },
                0x08 => new EpsonPaymentType { PaymentType = 5, Index = 0 },
                0x09 => new EpsonPaymentType { PaymentType = 5, Index = 3 },
                0x0A => new EpsonPaymentType { PaymentType = 2, Index = 1 },
                0x0B => new EpsonPaymentType { PaymentType = 2, Index = 1 },
                0x0C => new EpsonPaymentType { PaymentType = 0, Index = 0 },
                0x0D => new EpsonPaymentType { PaymentType = 5, Index = 0 },
                0x0E => new EpsonPaymentType { PaymentType = 5, Index = 0 },
                0x0F => new EpsonPaymentType { PaymentType = 3, Index = 1 },
                _ => new EpsonPaymentType { PaymentType = 0, Index = 0 }
            };
        }
    }
}
