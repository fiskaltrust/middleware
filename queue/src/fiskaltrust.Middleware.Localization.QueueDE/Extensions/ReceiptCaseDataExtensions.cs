using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace fiskaltrust.Middleware.Localization.QueueDE.Extensions
{
    public static class ReceiptCaseDataExtensions
    {
        /// <summary>
        /// Returns the effective <c>ftReceiptCaseData</c> JSON that carries the DE-specific fields.
        /// <para>
        /// A direct v1 request sends the fields flat, e.g. <c>{ "RefReceiptId": "..." }</c>.
        /// The v2 → v0 mapping instead nests the market case data under the market key and adds the
        /// original request under <c>v2ReceiptRequest</c>, e.g.
        /// <c>{ "DE": { "RefReceiptId": "..." }, "v2ReceiptRequest": { ... } }</c>.
        /// </para>
        /// When the nested (market-keyed) shape is detected, the market section is returned; otherwise the
        /// value is returned unchanged, so callers read the DE fields regardless of which shape was sent.
        /// </summary>
        public static string GetMarketReceiptCaseData(this string ftReceiptCaseData, string market = "DE")
        {
            if (string.IsNullOrWhiteSpace(ftReceiptCaseData))
            {
                return ftReceiptCaseData;
            }

            try
            {
                if (JToken.Parse(ftReceiptCaseData) is JObject root && root[market] is JObject marketSection)
                {
                    return marketSection.ToString(Formatting.None);
                }
            }
            catch (JsonException)
            {
                // Not valid JSON / not an object — fall through and return the original value.
            }

            return ftReceiptCaseData;
        }
    }
}
