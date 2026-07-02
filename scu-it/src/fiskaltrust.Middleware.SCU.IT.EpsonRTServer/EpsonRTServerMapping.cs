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
            var moment = receiptRequest.cbReceiptMoment;

            var recAmount = GetReceiptTotal(receiptRequest, docType);
            var recVat = GetReceiptVat(receiptRequest, docType);
            var payments = GetPaymentTotals(receiptRequest);
            var paidAmount = payments.Values.Sum();

            var newDailyAmountCents = tillState.CurrentDailyAmount + (docType == 0 ? ToCents(recAmount) : 0);
            var dailyAmount = newDailyAmountCents / 100m;

            var printerFiscalReceipt = BuildPrinterFiscalReceipt(
                receiptRequest, tillState, docType, docNumber, zNumber, moment,
                recAmount, recVat, dailyAmount, payments, paidAmount,
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

        private static string BuildPrinterFiscalReceipt(
            ReceiptRequest receiptRequest, TillState tillState, int docType, long docNumber, long zNumber, DateTime moment,
            decimal recAmount, decimal recVat, decimal dailyAmount, Dictionary<int, decimal> payments, decimal paidAmount,
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

            if (payments.Count == 0)
            {
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
            sb.Append($" cashAmount=\"{FormatAmount(payments.TryGetValue(0, out var cash) ? cash : 0)}\"");
            sb.Append($" checkAmount=\"{FormatAmount(payments.TryGetValue(1, out var check) ? check : 0)}\"");
            sb.Append($" ePayAmount=\"{FormatAmount(payments.TryGetValue(2, out var epay) ? epay : 0)}\"");
            sb.Append($" ticketAmount=\"{FormatAmount(payments.TryGetValue(3, out var ticket) ? ticket : 0)}\"");
            sb.Append($" changeAmount=\"{FormatAmount(0)}\"");
            sb.Append($" paidAmount=\"{FormatAmount(paidAmount)}\"");
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
            sb.Append($" vatID=\"{GetVatId(ci)}\"");
            sb.Append(" type=\"B\" ateco=\"0\" />");
        }

        private static void AppendItemAdjustment(StringBuilder sb, int adjustmentType, ChargeItem ci)
        {
            sb.Append("<printRecItemAdjustment");
            sb.Append($" adjustmentType=\"{adjustmentType}\"");
            sb.Append($" description=\"{Escape(ci.Description)}\"");
            sb.Append($" amount=\"{FormatAmount(Math.Abs(ci.Amount))}\"");
            sb.Append($" vatID=\"{GetVatId(ci)}\"");
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

        private static Dictionary<int, decimal> GetPaymentTotals(ReceiptRequest receiptRequest)
        {
            var totals = new Dictionary<int, decimal>();
            foreach (var payItem in receiptRequest.cbPayItems ?? Array.Empty<PayItem>())
            {
                var type = GetEpsonPaymentType(payItem).PaymentType;
                var amount = Math.Abs(payItem.Amount);
                totals[type] = (totals.TryGetValue(type, out var current) ? current : 0) + amount;
            }
            return totals;
        }

        private static long ToCents(decimal value) => (long) Math.Round(value * 100, MidpointRounding.AwayFromZero);

        private static string FormatAmount(decimal value) => value.ToString("F2", _amountFormat);

        private static string FormatQuantity(decimal value) => value.ToString("0.###", _amountFormat);

        private static string Escape(string? value) => SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;

        // VAT index mapping — mirrors EpsonRTPrinter.GetVatGroup (same Epson fiscal engine).
        public static int GetVatId(ChargeItem chargeItem)
        {
            if ((chargeItem.ftChargeItemCase & 0xF) == 0x8)
            {
                return (chargeItem.ftChargeItemCase & 0xF000) switch
                {
                    0x8000 => 10,
                    0x2000 => 11,
                    0x1000 => 12,
                    0x3000 => 13,
                    0x4000 => 14,
                    0x5000 => 15,
                    _ => 0
                };
            }

            return (chargeItem.ftChargeItemCase & 0xF) switch
            {
                0x1 => 2, // 10%
                0x2 => 3, // 4%
                0x3 => 1, // 22%
                0x4 => 4, // 5%
                0x7 => 13, // 0%
                0x8 => 0, // not taxable
                _ => 1
            };
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
