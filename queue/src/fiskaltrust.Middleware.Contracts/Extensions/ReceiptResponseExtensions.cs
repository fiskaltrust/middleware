using System;
using fiskaltrust.ifPOS.v1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace fiskaltrust.Middleware.Contracts.Extensions
{
    public struct StateDetail{
        public bool SigningDeviceAvailable;
        public int FailedReceiptCount;
        public DateTime? FailMoment;
    }

    public static class ReceiptResponseExtensions
    {
        /// <summary>The error state every market marks a failed receipt with, in the low 32 bits of <c>ftState</c>.</summary>
        private const long ErrorState = 0xEEEE_EEEE;

        public static void SetFtStateData(this ReceiptResponse receiptResponse, StateDetail stateDetail)
        {
            receiptResponse.ftStateData = JsonConvert.SerializeObject(stateDetail);
        }

        /// <summary>
        /// Whether a persisted receipt exists fiscally (RFC 712), the v1 flavour of the v2 predicate of the same name. True
        /// when <c>ftState</c> is not the error state, or when the persisted <c>ftStateData</c> carries
        /// <c>PostFiscalization.FiscalizationSucceeded = true</c>: a receipt whose eInvoicing/eReporting finalize call
        /// failed carries the error state although the fiscal record exists. Every lookup that needs to know whether a
        /// persisted receipt is fiscalized must use this predicate instead of testing <c>ftState</c> directly. The v1
        /// <c>ftStateData</c> is a string; anything that is not JSON carrying the marker counts as no marker.
        /// </summary>
        public static bool IsFiscalized(this ReceiptResponse receiptResponse)
            => (receiptResponse.ftState & 0xFFFF_FFFF) != ErrorState || HasFiscalizationSucceededMarker(receiptResponse.ftStateData);

        private static bool HasFiscalizationSucceededMarker(string stateData)
        {
            if (string.IsNullOrWhiteSpace(stateData))
            {
                return false;
            }

            try
            {
                var token = JToken.Parse(stateData);
                if (token.Type == JTokenType.String)
                {
                    // The state data was serialized into a JSON string once more on its way through a v1 store.
                    token = JToken.Parse((string) token);
                }

                return token is JObject stateObject
                    && stateObject["PostFiscalization"] is JObject postFiscalization
                    && postFiscalization["FiscalizationSucceeded"] is JValue { Type: JTokenType.Boolean } succeeded
                    && (bool) succeeded;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
