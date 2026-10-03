using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using fiskaltrust.ifPOS.v1;
using Newtonsoft.Json.Linq;
using V2 = fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Queue.PostFiscalization
{
    /// <summary>
    /// Maps the legacy stack's <c>ifPOS.v1</c> receipt pair to the v2 contract the eInvoicing and eReporting services
    /// implement, and merges the returned v2 response back onto the v1 response (RFC 712, "Backporting to the legacy
    /// stack"). Services implement exactly one contract this way, and the wire protocol is identical on both stacks.
    /// <para>
    /// The mapping is a JSON round trip, because v1 and v2 share their property names, with the shape differences
    /// patched up: string ids become Guids, v1 string fields that hold JSON (<c>ftStateData</c>, <c>ftReceiptCaseData</c>,
    /// <c>cbCustomer</c>, ...) are embedded as JSON so that services see the objects the v2 contract promises, and on
    /// the way back <c>ftStateData</c> is serialized into the v1 string again.
    /// </para>
    /// </summary>
    public static class PostFiscalizationMapper
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>The legacy convention for failure signature types, mirroring the uncaught-exception signature of the legacy sign processor.</summary>
        public static long LegacyFailureSignatureType(long ftReceiptCase) => unchecked((long) (((ulong) ftReceiptCase & 0xFFFF_0000_0000_0000) | 0x2000_0000_3000));

        /// <summary>The same convention for the v2 request the services are called with, so captions and types are byte-identical across both stacks.</summary>
        public static V2.Cases.SignatureType LegacyFailureSignatureType(V2.ReceiptRequest request) => (V2.Cases.SignatureType) (((ulong) request.ftReceiptCase & 0xFFFF_0000_0000_0000) | 0x2000_0000_3000);

        /// <summary>The legacy convention for the error state of a response, mirroring the uncaught-exception response of the legacy sign processor.</summary>
        public static long LegacyErrorState(long ftReceiptCase) => unchecked((long) (((ulong) ftReceiptCase & 0xFFFF_0000_0000_0000) | 0x2000_EEEE_EEEE));

        /// <summary>The fail state (0xFFFF_FFFF) of a response for which no queue item was created, such as a receipt refused in the preflight.</summary>
        public static long LegacyFailState(long ftReceiptCase) => unchecked((long) (((ulong) ftReceiptCase & 0xFFFF_0000_0000_0000) | 0x2000_FFFF_FFFF));

        public static V2.ReceiptRequest ToV2(ReceiptRequest request)
        {
            var json = JObject.FromObject(request);
            NormalizeGuid(json, nameof(request.ftCashBoxID), null);
            NormalizeGuid(json, nameof(request.ftQueueID), null);
            NormalizeGuid(json, nameof(request.ftPosSystemId), null);
            EmbedJson(json, nameof(request.ftReceiptCaseData));
            EmbedJson(json, nameof(request.cbUser));
            EmbedJson(json, nameof(request.cbArea));
            EmbedJson(json, nameof(request.cbCustomer));
            EmbedJson(json, nameof(request.cbSettlement));
            if (json[nameof(request.cbPreviousReceiptReference)] is JValue { Type: JTokenType.String } previous && string.IsNullOrEmpty((string) previous))
            {
                json[nameof(request.cbPreviousReceiptReference)] = null;
            }

            EmbedItemCaseData(json[nameof(request.cbChargeItems)], "ftChargeItemCaseData");
            EmbedItemCaseData(json[nameof(request.cbPayItems)], "ftPayItemCaseData");
            return Deserialize<V2.ReceiptRequest>(json);
        }

        public static V2.ReceiptResponse ToV2(ReceiptResponse response)
        {
            var json = JObject.FromObject(response);
            NormalizeGuid(json, nameof(response.ftCashBoxID), null);
            NormalizeGuid(json, nameof(response.ftQueueID), Guid.Empty);
            NormalizeGuid(json, nameof(response.ftQueueItemID), Guid.Empty);
            EmbedJson(json, nameof(response.ftStateData));
            if (json[nameof(response.ftSignatures)] == null || json[nameof(response.ftSignatures)].Type == JTokenType.Null)
            {
                json[nameof(response.ftSignatures)] = new JArray();
            }

            EmbedItemCaseData(json[nameof(response.ftChargeItems)], "ftChargeItemCaseData");
            EmbedItemCaseData(json[nameof(response.ftPayItems)], "ftPayItemCaseData");
            return Deserialize<V2.ReceiptResponse>(json);
        }

        /// <summary>
        /// Merges what the contract lets a service change back onto the v1 response: the signatures, the state data and
        /// the state. Everything else stays exactly as the country processor produced it, so no v1-only field is lost.
        /// </summary>
        public static void MergeIntoV1(ReceiptResponse target, V2.ReceiptResponse source)
        {
            target.ftState = unchecked((long) (ulong) source.ftState);
            target.ftSignatures = (source.ftSignatures ?? new List<V2.SignatureItem>()).Select(ToV1).ToArray();
            target.ftStateData = StateDataToString(source.ftStateData);
        }

        public static SignaturItem ToV1(V2.SignatureItem signature) => new SignaturItem
        {
            ftSignatureFormat = unchecked((long) (ulong) signature.ftSignatureFormat),
            ftSignatureType = unchecked((long) (ulong) signature.ftSignatureType),
            Caption = signature.Caption,
            Data = signature.Data,
        };

        /// <summary>The v1 <c>ftStateData</c> is a string: objects are serialized, strings pass through, nothing is dropped.</summary>
        public static string StateDataToString(object stateData)
        {
            switch (stateData)
            {
                case null:
                    return null;
                case string text:
                    return text;
                case JsonElement { ValueKind: JsonValueKind.Null }:
                    return null;
                case JsonElement { ValueKind: JsonValueKind.String } element:
                    return element.GetString();
                case JsonElement element:
                    return element.GetRawText();
                default:
                    return System.Text.Json.JsonSerializer.Serialize(stateData, stateData.GetType(), _options);
            }
        }

        private static T Deserialize<T>(JObject json)
            => System.Text.Json.JsonSerializer.Deserialize<T>(json.ToString(Newtonsoft.Json.Formatting.None), _options)
               ?? throw new InvalidOperationException($"The receipt could not be converted to {typeof(T).Name}.");

        /// <summary>v1 ids are strings; v2 ids are Guids. Empty or invalid strings become the fallback (null for nullable ids).</summary>
        private static void NormalizeGuid(JObject json, string name, Guid? fallback)
        {
            var token = json[name];
            if (token != null && token.Type == JTokenType.String && Guid.TryParse((string) token, out _))
            {
                return;
            }

            json[name] = fallback.HasValue ? new JValue(fallback.Value.ToString()) : JValue.CreateNull();
        }

        /// <summary>A v1 string field that holds a JSON object or array is embedded as JSON, which is what the v2 contract promises for that field.</summary>
        private static void EmbedJson(JObject json, string name)
        {
            if (json == null || !(json[name] is JValue { Type: JTokenType.String } value))
            {
                return;
            }

            var text = ((string) value)?.Trim();
            if (string.IsNullOrEmpty(text) || !(text.StartsWith("{", StringComparison.Ordinal) || text.StartsWith("[", StringComparison.Ordinal)))
            {
                return;
            }

            try
            {
                json[name] = JToken.Parse(text);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                // Not JSON after all: the service gets the string, exactly as the POS sent it.
            }
        }

        private static void EmbedItemCaseData(JToken items, string name)
        {
            if (!(items is JArray array))
            {
                return;
            }

            foreach (var item in array.OfType<JObject>())
            {
                EmbedJson(item, name);
            }
        }
    }
}
