using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace fiskaltrust.Middleware.Journal.FR.Archive.V2
{
    /// <summary>
    /// One signed entry of a QueueFR v2 chain, read from the stored queue item: what the POS sent, what
    /// the queue answered, and the request hash the queue stored with it. It is built from the raw
    /// <c>ftQueueItem</c> fields only, so the middleware and datapipes feed the same columns (ADR 0008)
    /// without this package referencing either one's storage.
    /// </summary>
    public sealed class ArchiveEntry
    {
        private const long CountryMask = unchecked((long) 0xFFFF_0000_0000_0000);
        private const long CountryFR = 0x4652_0000_0000_0000;
        private const long ReceiptSignatureType = 0x0001;
        private const long CertificateSerialNumberType = 0x0002;
        private const long SiretType = 0x0009;
        private const long TrainingFlag = 0x0000_0000_0002_0000;

        private ArchiveEntry(Guid queueItemId, Guid queueId, long queueRow, string? requestHash, string rawRequest, JsonElement request, JsonElement response,
            string chain, long chainCounter, string signature)
        {
            RawRequest = rawRequest;
            QueueItemId = queueItemId;
            QueueId = queueId;
            QueueRow = queueRow;
            RequestHash = string.IsNullOrEmpty(requestHash) ? null : requestHash;
            Request = request;
            Response = response;
            Chain = chain;
            ChainCounter = chainCounter;
            Signature = signature;
            ReceiptIdentification = String(response, "ftReceiptIdentification")!;
            ReceiptMoment = Moment(response, "ftReceiptMoment") ?? DateTime.MinValue;
            ReceiptCase = Long(request, "ftReceiptCase") ?? 0;
            State = Long(response, "ftState") ?? 0;
            CertificateSerialNumber = SignatureData(response, CertificateSerialNumberType);
            Siret = SignatureData(response, SiretType);
        }

        public Guid QueueItemId { get; }

        public Guid QueueId { get; }

        public long QueueRow { get; }

        /// <summary><c>ftQueueItem.requestHash</c> as stored, never recomputed (ADR 0010).</summary>
        public string? RequestHash { get; }

        /// <summary>The ReceiptRequest as the queue stored it.</summary>
        public JsonElement Request { get; }

        /// <summary>
        /// <c>ftQueueItem.request</c> exactly as stored, never re-serialized: the LNE request hash (ADR 0013)
        /// is recomputed from it (archive-v2 §9).
        /// </summary>
        public string RawRequest { get; }

        /// <summary>The ReceiptResponse as the queue stored it.</summary>
        public JsonElement Response { get; }

        /// <summary>The chain letter, as the queue wrote it into <c>ftReceiptIdentification</c>.</summary>
        public string Chain { get; }

        public long ChainCounter { get; }

        public string ReceiptIdentification { get; }

        public DateTime ReceiptMoment { get; }

        public long ReceiptCase { get; }

        public long State { get; }

        public bool Training => (ReceiptCase & TrainingFlag) != 0;

        /// <summary>The <c>Data</c> of the receipt signature item: a compact JWS (InfoCert) or a bare Base64Url signature (LNE).</summary>
        public string Signature { get; }

        /// <summary>The part of <see cref="Signature"/> the next entry of the chain covers (ADR 0001).</summary>
        public string ChainedSignature => Signature.Substring(Signature.LastIndexOf('.') + 1);

        public string? CertificateSerialNumber { get; }

        public string? Siret { get; }

        /// <summary>
        /// The chained signature of the previous entry of the same chain; null on the start of the chain, and
        /// when that entry is not known (see <see cref="ArchiveChainLinker"/>).
        /// </summary>
        public string? PreviousSignature { get; internal set; }

        /// <summary>True on the first entry of a chain, which carries no previous signature (ADR 0001).</summary>
        public bool StartOfChain { get; internal set; }

        /// <summary>
        /// Reads a stored queue item. Returns null when it is not a signed QueueFR v2 entry: no response yet,
        /// an error response, a v1 entry (whose signature items lack the v2 format nibble), or a response
        /// without a receipt signature.
        /// </summary>
        public static ArchiveEntry? TryCreate(Guid queueItemId, Guid queueId, long queueRow, string? request, string? response, string? requestHash)
        {
            if (string.IsNullOrEmpty(request) || string.IsNullOrEmpty(response))
            {
                return null;
            }

            JsonElement requestJson, responseJson;
            try
            {
                using (var requestDocument = JsonDocument.Parse(request!))
                using (var responseDocument = JsonDocument.Parse(response!))
                {
                    requestJson = requestDocument.RootElement.Clone();
                    responseJson = responseDocument.RootElement.Clone();
                }
            }
            catch (JsonException)
            {
                return null;
            }

            if (requestJson.ValueKind != JsonValueKind.Object || responseJson.ValueKind != JsonValueKind.Object)
            {
                return null;
            }
            if (IsErrorState(Long(responseJson, "ftState") ?? 0))
            {
                return null;
            }
            var signature = SignatureData(responseJson, ReceiptSignatureType);
            if (string.IsNullOrEmpty(signature))
            {
                return null;
            }
            if (!TryReadChain(String(responseJson, "ftReceiptIdentification"), out var chain, out var counter))
            {
                return null;
            }

            return new ArchiveEntry(queueItemId, queueId, queueRow, requestHash, request!, requestJson, responseJson, chain, counter, signature!);
        }

        /// <summary>
        /// The chain letter and counter at the end of an identification such as <c>ft1A#T42</c>: the
        /// trailing letters and the digits after them (<c>ReceiptIdentificationHelper</c> appends them).
        /// </summary>
        internal static bool TryReadChain(string? identification, out string chain, out long counter)
        {
            chain = string.Empty;
            counter = 0;
            if (string.IsNullOrEmpty(identification))
            {
                return false;
            }
            var end = identification!.Length;
            var digitsStart = end;
            while (digitsStart > 0 && char.IsDigit(identification[digitsStart - 1]))
            {
                digitsStart--;
            }
            var lettersStart = digitsStart;
            while (lettersStart > 0 && identification[lettersStart - 1] >= 'A' && identification[lettersStart - 1] <= 'Z')
            {
                lettersStart--;
            }
            if (digitsStart == end || lettersStart == digitsStart
                || !long.TryParse(identification.Substring(digitsStart), NumberStyles.None, CultureInfo.InvariantCulture, out counter))
            {
                return false;
            }
            chain = identification.Substring(lettersStart, digitsStart - lettersStart);
            return true;
        }

        // ifPOS v2 State: the low 32 bits are 0xEEEE_EEEE on an error and 0xFFFF_FFFF on a failure.
        private static bool IsErrorState(long state) => (state & 0xFFFF_FFFF) is 0xEEEE_EEEE or 0xFFFF_FFFF;

        /// <summary>
        /// The data of the first FR v2 signature item of the given type (low word). The v2 format nibble
        /// (<c>0xCCCC_2…</c>) tells a v2 item from a v1 one, whose types are <c>0x4652_0000_…</c>.
        /// </summary>
        internal static string? SignatureData(JsonElement response, long type)
        {
            if (!response.TryGetProperty("ftSignatures", out var signatures) || signatures.ValueKind != JsonValueKind.Array)
            {
                return null;
            }
            foreach (var item in signatures.EnumerateArray())
            {
                var signatureType = Long(item, "ftSignatureType");
                if (signatureType is null
                    || (signatureType.Value & CountryMask) != CountryFR
                    || ((signatureType.Value >> 44) & 0xF) != 2
                    || (signatureType.Value & 0xFFFF) != type)
                {
                    continue;
                }
                return String(item, "Data");
            }
            return null;
        }

        internal static IEnumerable<JsonElement> Array(JsonElement element, string name)
            => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
                ? value.EnumerateArray()
                : Enumerable.Empty<JsonElement>();

        internal static JsonElement? Property(JsonElement element, string name)
            => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null && value.ValueKind != JsonValueKind.Undefined
                ? value
                : (JsonElement?) null;

        internal static string? String(JsonElement element, string name)
            => Property(element, name) is { } value && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        /// <summary>
        /// A string as is; any other JSON value as its raw text. The contract writes <c>cbUser</c> and
        /// <c>cbArea</c> this way ("JSON if structured"); <c>case-data.csv</c> keeps the lossless copy.
        /// </summary>
        internal static string? Text(JsonElement element, string name)
            => Property(element, name) is { } value
                ? value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText()
                : null;

        /// <summary>A number, or a <c>0x…</c> hex string as some serializers write cases.</summary>
        internal static long? Long(JsonElement element, string name)
        {
            if (Property(element, name) is not { } value)
            {
                return null;
            }
            if (value.ValueKind == JsonValueKind.Number)
            {
                if (value.TryGetInt64(out var signed))
                {
                    return signed;
                }
                return value.TryGetUInt64(out var unsigned) ? unchecked((long) unsigned) : (long?) null;
            }
            if (value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString() ?? string.Empty;
                if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    && ulong.TryParse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
                {
                    return unchecked((long) hex);
                }
                return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : (long?) null;
            }
            return null;
        }

        internal static decimal? Decimal(JsonElement element, string name)
            => Property(element, name) is { } value && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number) ? number : (decimal?) null;

        internal static DateTime? Moment(JsonElement element, string name)
        {
            if (Property(element, name) is not { } value || value.ValueKind != JsonValueKind.String)
            {
                return null;
            }
            return DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var moment)
                ? DateTime.SpecifyKind(moment, DateTimeKind.Utc)
                : (DateTime?) null;
        }
    }
}
