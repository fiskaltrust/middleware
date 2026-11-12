using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace fiskaltrust.Middleware.Journal.FR.Archive.V2
{
    /// <summary>
    /// One signed entry of a QueueFR v2 chain, read from the stored queue item: what the POS sent, what
    /// the queue answered, and what the SCU signed. It is built from the raw <c>ftQueueItem</c> fields
    /// only, so the middleware and datapipes feed the same columns (ADR 0008) without this package
    /// referencing either one's storage.
    /// </summary>
    /// <remarks>
    /// The chain links (<see cref="StartOfChain"/>, <see cref="PreviousSignature"/>), <see cref="Siret"/>
    /// and <see cref="RequestHash"/> are read from the signed content of the receipt signature, never
    /// derived from the entries around it (archive-v2 §1.1, ADR 0001, ADR 0012), so an entry reads the
    /// same whatever else the export holds. An entry read through an <see cref="ArchiveEntrySpool"/>
    /// keeps its request and response on disk and loads them when a column needs them.
    /// </remarks>
    public sealed class ArchiveEntry
    {
        private const long CountryMask = unchecked((long) 0xFFFF_0000_0000_0000);
        private const long CountryFR = 0x4652_0000_0000_0000;
        private const long ReceiptSignatureType = 0x0001;
        private const long SiretType = 0x0009;
        private const long TrainingFlag = 0x0000_0000_0002_0000;

        private readonly ArchiveEntryContent? _content;
        private readonly ArchiveEntrySpool? _spool;
        private readonly long _spoolOffset;

        private ArchiveEntry(Guid queueItemId, Guid queueId, long queueRow, string? processingVersion, ArchiveEntryContent content,
            string chain, long chainCounter, string receiptIdentification, DateTime receiptMoment)
        {
            QueueItemId = queueItemId;
            QueueId = queueId;
            QueueRow = queueRow;
            ProcessingVersion = string.IsNullOrEmpty(processingVersion) ? null : processingVersion;
            _content = content;
            Chain = chain;
            ChainCounter = chainCounter;
            ReceiptIdentification = receiptIdentification;
            ReceiptMoment = receiptMoment;
            ReceiptCase = Long(content.Request, "ftReceiptCase") ?? 0;
            State = Long(content.Response, "ftState") ?? 0;
            StartOfChain = content.Links.StartOfChain;
            CertificateSerialNumber = content.Links.CertificateSerialNumber;
            Siret = content.Links.Siret ?? SignatureData(content.Response, SiretType);
        }

        private ArchiveEntry(ArchiveEntry entry, ArchiveEntrySpool spool, long spoolOffset, Func<string?, string?> intern)
        {
            QueueItemId = entry.QueueItemId;
            QueueId = entry.QueueId;
            QueueRow = entry.QueueRow;
            ProcessingVersion = intern(entry.ProcessingVersion);
            _spool = spool;
            _spoolOffset = spoolOffset;
            Chain = intern(entry.Chain)!;
            ChainCounter = entry.ChainCounter;
            ReceiptIdentification = entry.ReceiptIdentification;
            ReceiptMoment = entry.ReceiptMoment;
            ReceiptCase = entry.ReceiptCase;
            State = entry.State;
            StartOfChain = entry.StartOfChain;
            CertificateSerialNumber = intern(entry.CertificateSerialNumber);
            Siret = intern(entry.Siret);
        }

        public Guid QueueItemId { get; }

        /// <summary>
        /// The version of the middleware that processed the entry, as stored on the queue item
        /// (<c>ftQueueItem.ProcessingVersion</c>, the informational version with build and commit; market-fr#124).
        /// </summary>
        public string? ProcessingVersion { get; }

        /// <summary>The <c>FR</c> block of the response's <c>ftStateData</c>, where the queue stores totals and line values.</summary>
        public JsonElement? StateFR => Content.StateFR;

        public Guid QueueId { get; }

        public long QueueRow { get; }

        /// <summary>
        /// The canonical request value the SCU signed, <c>1:&lt;Base64 SHA-256&gt;</c> (ADR 0013): the field
        /// right after <c>ftQueueItemID</c> of the LNE and self-certified data set (field 9); never recomputed
        /// here. Null when the SCU signs none (InfoCert, the in-memory SCU). A verifier recomputes it from
        /// <see cref="RawRequest"/> and compares.
        /// </summary>
        public string? RequestHash => Content.Links.RequestHash;

        /// <summary>The ReceiptRequest as the queue stored it.</summary>
        public JsonElement Request => Content.Request;

        /// <summary>
        /// <c>ftQueueItem.request</c> exactly as stored, never re-serialized: the LNE request hash (ADR 0013)
        /// is recomputed from it (archive-v2 §9).
        /// </summary>
        public string RawRequest => Content.RawRequest;

        /// <summary>The ReceiptResponse as the queue stored it.</summary>
        public JsonElement Response => Content.Response;

        /// <summary>The chain letter, as the queue wrote it into <c>ftReceiptIdentification</c>.</summary>
        public string Chain { get; }

        /// <summary>The entry's position in its chain: the counter of <c>ftReceiptIdentification</c>, equal to the signed one.</summary>
        public long ChainCounter { get; }

        public string ReceiptIdentification { get; }

        public DateTime ReceiptMoment { get; }

        public long ReceiptCase { get; }

        public long State { get; }

        public bool Training => (ReceiptCase & TrainingFlag) != 0;

        /// <summary>The <c>Data</c> of the receipt signature item: a compact JWS (InfoCert) or <c>&lt;data set&gt;.&lt;signature&gt;</c> (LNE).</summary>
        public string Signature => Content.Signature;

        /// <summary>The part of <see cref="Signature"/> the next entry of the chain covers (ADR 0001).</summary>
        public string ChainedSignature => Signature.Substring(Signature.LastIndexOf('.') + 1);

        /// <summary>The signing certificate's serial number, as signed: the JWS payload's <c>csn</c>, the data set's fourth-from-last field.</summary>
        public string? CertificateSerialNumber { get; }

        /// <summary>
        /// The SIRET the SCU signed: the data set's first field, the JWS payload's <c>siret</c>. An InfoCert JET
        /// payload signs none; its SIRET is the one the SCU printed with it (the <c>Siret</c> item, 0x0009).
        /// </summary>
        public string? Siret { get; }

        /// <summary>
        /// The signed previous signature of the same chain, verbatim (ADR 0001); null exactly when
        /// <see cref="StartOfChain"/>. The first v2 entry of a chain continued from v1.2 carries the v1.2
        /// signature it was chained on (ADR 0003).
        /// </summary>
        public string? PreviousSignature => Content.Links.PreviousSignature;

        /// <summary>True on the first entry of a chain, which carries no previous signature: the signed flag <c>N</c> (ADR 0001).</summary>
        public bool StartOfChain { get; }

        private ArchiveEntryContent Content => _content ?? _spool!.Load(_spoolOffset);

        /// <summary>This entry, its request and response kept in <paramref name="spool"/> instead of memory.</summary>
        internal ArchiveEntry Spooled(ArchiveEntrySpool spool, long offset, Func<string?, string?> intern) => new ArchiveEntry(this, spool, offset, intern);

        /// <summary>
        /// Reads a stored queue item. Returns null when it is not a signed QueueFR v2 entry: no response yet,
        /// an error response, or a response without a v2 receipt signature (a v1 entry, whose signature items
        /// lack the v2 format nibble, or an unsigned one).
        /// </summary>
        /// <exception cref="ArchiveEntryException">
        /// The item is stored but cannot be read: a request or response that is not a JSON object, or a signed
        /// v2 entry whose identification, receipt moment or signed content cannot be read. An export never
        /// skips such an entry silently (LNE Ex. 10).
        /// </exception>
        public static ArchiveEntry? Read(Guid queueItemId, Guid queueId, long queueRow, string? request, string? response, string? processingVersion = null)
        {
            if (string.IsNullOrEmpty(response))
            {
                return null;
            }
            ArchiveEntryException Unreadable(string reason, Exception? inner)
                => new ArchiveEntryException(queueItemId, queueRow, $"The queue item {queueItemId} (queue row {queueRow}) cannot be archived: {reason}.", inner);

            var responseJson = ParseObject(response!, "response", Unreadable);
            if (IsErrorState(Long(responseJson, "ftState") ?? 0))
            {
                return null;
            }
            var signature = SignatureData(responseJson, ReceiptSignatureType);
            if (string.IsNullOrEmpty(signature))
            {
                return null;
            }
            if (string.IsNullOrEmpty(request))
            {
                throw Unreadable("its response is signed, and its request is empty", null);
            }
            var requestJson = ParseObject(request!, "request", Unreadable);

            var identification = String(responseJson, "ftReceiptIdentification");
            if (!TryReadChain(identification, out var chain, out var counter))
            {
                throw Unreadable($"its ftReceiptIdentification '{identification}' names no chain and counter", null);
            }
            var moment = Moment(responseJson, "ftReceiptMoment")
                ?? throw Unreadable($"its ftReceiptMoment '{Property(responseJson, "ftReceiptMoment")?.ToString()}' is no date", null);
            SignedLinks links;
            try
            {
                links = SignedLinks.Parse(signature!, queueItemId);
            }
            catch (FormatException ex)
            {
                throw Unreadable($"the signed content of its receipt signature cannot be read ({ex.Message})", ex);
            }
            if (links.ChainCounter != counter)
            {
                throw Unreadable($"its signed chain counter {links.ChainCounter} is not the counter of {identification}", null);
            }

            var content = new ArchiveEntryContent(request!, requestJson, responseJson, signature!, links);
            return new ArchiveEntry(queueItemId, queueId, queueRow, processingVersion, content, chain, counter, identification!, moment);
        }

        /// <summary>
        /// The moment a stored response says it was answered, or null when it holds none that can be read:
        /// the moment an export places an item in its period by, v1 and v2 items alike.
        /// </summary>
        public static DateTime? ReceiptMomentOf(string? response)
        {
            if (string.IsNullOrEmpty(response))
            {
                return null;
            }
            try
            {
                using var document = JsonDocument.Parse(response!);
                return document.RootElement.ValueKind == JsonValueKind.Object ? Moment(document.RootElement, "ftReceiptMoment") : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>The content of an entry <see cref="Read"/> accepted, loaded again from its stored request and response.</summary>
        internal static ArchiveEntryContent LoadContent(string request, string response)
        {
            static ArchiveEntryException Corrupt(string reason, Exception? inner) => new ArchiveEntryException(Guid.Empty, 0, "A spooled archive entry cannot be read back: " + reason + ".", inner);
            var responseJson = ParseObject(response, "response", Corrupt);
            var signature = SignatureData(responseJson, ReceiptSignatureType)!;
            return new ArchiveEntryContent(request, ParseObject(request, "request", Corrupt), responseJson, signature, SignedLinks.Parse(signature, null));
        }

        private static JsonElement ParseObject(string json, string what, Func<string, Exception?, ArchiveEntryException> unreadable)
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(json);
            }
            catch (JsonException ex)
            {
                throw unreadable($"its {what} is no valid JSON ({ex.Message})", ex);
            }
            using (document)
            {
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw unreadable($"its {what} is no JSON object", null);
                }
                return document.RootElement.Clone();
            }
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
        internal static string? SignatureData(JsonElement response, long type) => SignatureItems(response, type).FirstOrDefault();

        private static IEnumerable<string?> SignatureItems(JsonElement response, long type)
        {
            if (!response.TryGetProperty("ftSignatures", out var signatures) || signatures.ValueKind != JsonValueKind.Array)
            {
                yield break;
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
                yield return String(item, "Data");
            }
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

    /// <summary>What an entry holds beyond its scalar columns: the stored request and response, and what was signed.</summary>
    internal sealed class ArchiveEntryContent
    {
        public ArchiveEntryContent(string rawRequest, JsonElement request, JsonElement response, string signature, SignedLinks links)
        {
            RawRequest = rawRequest;
            Request = request;
            Response = response;
            Signature = signature;
            Links = links;
            StateFR = ArchiveEntry.Property(response, "ftStateData") is { ValueKind: JsonValueKind.Object } stateData
                && ArchiveEntry.Property(stateData, "FR") is { ValueKind: JsonValueKind.Object } fr ? fr : (JsonElement?) null;
        }

        public string RawRequest { get; }

        public JsonElement Request { get; }

        public JsonElement Response { get; }

        public JsonElement? StateFR { get; }

        public string Signature { get; }

        public SignedLinks Links { get; }
    }
}
