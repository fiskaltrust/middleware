using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.PostFiscalization;
using V2 = fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Queue.PostFiscalization
{
    /// <summary>
    /// Which receipts the eInvoicing service is called for on the legacy stack (RFC 712). The shared rule applies first:
    /// the <c>0x1000</c> invoice type nibble of <c>ftReceiptCase</c>, which the v2 markets and IT carry. The v1 receipt
    /// cases of the other legacy markets have no type nibble, so two rules are added for them. A receipt the POS sent as
    /// a v2 request reaches a v1 queue with its unmapped original embedded in <c>ftReceiptCaseData.v2ReceiptRequest</c>
    /// (PosSystem API since service-possystem-api#215); its <c>ftReceiptCase</c> is judged by the shared rule, so a v2
    /// invoice qualifies on every market, and the queue agrees with the service, which judges the same original. For POS
    /// systems that send v1 directly, the invoice document types are listed here per market; a case matches on country
    /// and case number, whatever flags are set in between.
    /// </summary>
    public static class LegacyInvoiceReceiptCases
    {
        private const ulong CountryAndCaseMask = 0xFFFF_0000_0000_FFFF;

        /// <summary>The property under which the PosSystem API embeds the unmapped v2 request into the v1 <c>ftReceiptCaseData</c>.</summary>
        public const string EmbeddedV2RequestProperty = "v2ReceiptRequest";

        /// <summary>The v1 receipt cases that are invoice document types, by market. AT and FR are pending decisions in the RFC.</summary>
        public static readonly IReadOnlyCollection<ulong> AllowList = new HashSet<ulong>
        {
            0x4445_0000_0000_000C, // DE B2B-invoice
            0x4445_0000_0000_000D, // DE B2C-invoice
        };

        public static bool IsInvoiceDocument(V2.ReceiptRequest request)
            => PostFiscalizationProcessor.IsInvoiceDocument(request)
               || EmbeddedV2ReceiptCase(request) is { } v2Case && v2Case.IsType(ReceiptCaseType.Invoice)
               || AllowList.Contains((ulong) request.ftReceiptCase & CountryAndCaseMask);

        /// <summary>
        /// The <c>ftReceiptCase</c> of the v2 request embedded in <c>ftReceiptCaseData</c>, or null when there is none or it
        /// cannot be read. <c>ftReceiptCaseData</c> is an open extension point, so nothing in it is ever rejected: a missing
        /// or malformed original falls through to the allow list, exactly like a request from a PosSystem API that does not
        /// embed one. The case data arrives as the <see cref="JsonElement"/> the mapper produced, or as the raw string when
        /// the filter is applied to a request that was not mapped.
        /// </summary>
        public static ReceiptCase? EmbeddedV2ReceiptCase(V2.ReceiptRequest request)
        {
            try
            {
                var caseData = AsJsonElement(request.ftReceiptCaseData);
                if (caseData == null || caseData.Value.ValueKind != JsonValueKind.Object
                    || !TryGetPropertyIgnoreCase(caseData.Value, EmbeddedV2RequestProperty, out var embedded) || embedded.ValueKind != JsonValueKind.Object
                    || !TryGetPropertyIgnoreCase(embedded, nameof(V2.ReceiptRequest.ftReceiptCase), out var receiptCase))
                {
                    return null;
                }

                switch (receiptCase.ValueKind)
                {
                    case JsonValueKind.Number when receiptCase.TryGetUInt64(out var unsigned):
                        return (ReceiptCase) unsigned;
                    case JsonValueKind.Number when receiptCase.TryGetInt64(out var signed):
                        return (ReceiptCase) unchecked((ulong) signed);
                    case JsonValueKind.String when TryParseReceiptCase(receiptCase.GetString(), out var parsed):
                        return (ReceiptCase) parsed;
                    default:
                        return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static JsonElement? AsJsonElement(object caseData)
        {
            switch (caseData)
            {
                case null:
                    return null;
                case JsonElement element:
                    return element;
                case string text:
                    var trimmed = text.Trim();
                    return trimmed.StartsWith("{", StringComparison.Ordinal) ? JsonSerializer.Deserialize<JsonElement>(trimmed) : (JsonElement?) null;
                default:
                    return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(caseData));
            }
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        /// <summary>A receipt case written as a decimal or a <c>0x</c>-prefixed hexadecimal number; a negative decimal is the signed form of the same bits.</summary>
        private static bool TryParseReceiptCase(string text, out ulong value)
        {
            value = 0;
            text = text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return ulong.TryParse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
            }

            if (ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var signed))
            {
                value = unchecked((ulong) signed);
                return true;
            }

            return false;
        }
    }
}
