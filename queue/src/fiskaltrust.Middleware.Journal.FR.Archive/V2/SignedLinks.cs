using System;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace fiskaltrust.Middleware.Journal.FR.Archive.V2
{
    /// <summary>
    /// The fields of an entry's signed content the archive reports as signed (archive-v2 §1.1): the chain
    /// counter, the start-of-chain flag and the previous signature (ADR 0001), the SIRET, the signing
    /// certificate's serial number and the canonical request value (ADR 0013).
    /// </summary>
    /// <remarks>
    /// The signed content is the receipt QR code before its last <c>.</c>:
    /// <list type="bullet">
    /// <item>a <c>|</c>-separated data set (LNE, self-certified and in-memory SCUs). The SIRET is its first field;
    /// it ends with the serial number, the chain counter, the flag and the previous signature. The LNE and
    /// self-certified data set has 25 fields, the request value is field 9, right after <c>ftQueueItemID</c>;</item>
    /// <item>a JWS <c>header.payload</c> (InfoCert), whose payload carries <c>siret</c> (receipts only),
    /// <c>csn</c>, <c>chainCounter</c>, <c>previousSignatureCarriedOver</c> and <c>previousSignature</c>.</item>
    /// </list>
    /// The flag is coded <c>O</c> (the previous signature is carried over) or <c>N</c> (start of the chain).
    /// </remarks>
    internal sealed class SignedLinks
    {
        /// <summary>The fields of the LNE and self-certified data set (<c>LneDataSetBuilder</c>, <c>SelfCertifiedDataSetBuilder</c>).</summary>
        private const int LneDataSetFieldCount = 25;
        private const int LneQueueItemIdField = 7;
        private const int LneRequestHashField = 8;

        // FRCanonicalRequest.SignedField: the canonical-form version, a colon, the padded Base64 SHA-256 (ADR 0013).
        private static readonly Regex SignedRequestHashField = new Regex("^[0-9]+:[A-Za-z0-9+/]{43}=$", RegexOptions.CultureInvariant);

        private SignedLinks(long chainCounter, string flag, string? previousSignature, string? siret, string? certificateSerialNumber, string? requestHash)
        {
            if (flag != "O" && flag != "N")
            {
                throw new FormatException($"the start-of-chain flag '{flag}' is neither O nor N");
            }
            StartOfChain = flag == "N";
            if (StartOfChain != string.IsNullOrEmpty(previousSignature))
            {
                throw new FormatException(StartOfChain
                    ? "the flag N starts the chain, and a previous signature is signed with it"
                    : "the flag O carries a previous signature over, and none is signed");
            }
            ChainCounter = chainCounter;
            PreviousSignature = StartOfChain ? null : previousSignature;
            Siret = string.IsNullOrEmpty(siret) ? null : siret;
            CertificateSerialNumber = string.IsNullOrEmpty(certificateSerialNumber) ? null : certificateSerialNumber;
            RequestHash = string.IsNullOrEmpty(requestHash) ? null : requestHash;
        }

        public long ChainCounter { get; }

        public bool StartOfChain { get; }

        public string? PreviousSignature { get; }

        /// <summary>Null when the signed content carries none (an InfoCert JET payload).</summary>
        public string? Siret { get; }

        public string? CertificateSerialNumber { get; }

        public string? RequestHash { get; }

        /// <summary>Reads the signed content of a receipt QR code.</summary>
        /// <param name="queueItemId">The entry's queue item, checked against the one an LNE data set signs; null to skip the check.</param>
        /// <exception cref="FormatException">The signed content lacks a field, or a field does not read.</exception>
        public static SignedLinks Parse(string qrCode, Guid? queueItemId)
        {
            var dot = qrCode.LastIndexOf('.');
            var content = dot < 0 ? "" : qrCode.Substring(0, dot);
            return content.IndexOf('|') >= 0 ? FromDataSet(content, queueItemId) : FromJws(content);
        }

        private static SignedLinks FromDataSet(string dataSet, Guid? queueItemId)
        {
            var fields = dataSet.Split('|');
            var n = fields.Length;
            if (n < 5)
            {
                throw new FormatException($"the data set has {n} fields, fewer than the SIRET, serial number, chain counter, flag and previous signature");
            }
            if (!long.TryParse(fields[n - 3], NumberStyles.None, CultureInfo.InvariantCulture, out var counter))
            {
                throw new FormatException($"the chain counter '{fields[n - 3]}' is no number");
            }

            string? requestHash = null;
            if (n == LneDataSetFieldCount)
            {
                if (queueItemId is { } id && !(Guid.TryParse(fields[LneQueueItemIdField], out var signedId) && signedId == id))
                {
                    throw new FormatException($"the data set is signed for the queue item '{fields[LneQueueItemIdField]}'");
                }
                requestHash = fields[LneRequestHashField];
                if (requestHash.Length > 0 && !SignedRequestHashField.IsMatch(requestHash))
                {
                    throw new FormatException($"field {LneRequestHashField + 1} '{requestHash}' is no canonical request value");
                }
            }
            return new SignedLinks(counter, fields[n - 2], fields[n - 1], fields[0], fields[n - 4], requestHash);
        }

        private static SignedLinks FromJws(string headerAndPayload)
        {
            var segments = headerAndPayload.Split('.');
            if (segments.Length != 2)
            {
                throw new FormatException("the signed content is neither a data set nor a JWS");
            }
            JsonDocument document;
            try
            {
                var payload = segments[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                document = JsonDocument.Parse(Convert.FromBase64String(payload));
            }
            catch (Exception ex) when (ex is FormatException || ex is JsonException)
            {
                throw new FormatException("the JWS payload is no Base64Url JSON", ex);
            }
            using (document)
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    throw new FormatException("the JWS payload is no JSON object");
                }
                var counter = ArchiveEntry.Long(root, "chainCounter") ?? throw new FormatException("the JWS payload has no chainCounter");
                var flag = ArchiveEntry.String(root, "previousSignatureCarriedOver") ?? throw new FormatException("the JWS payload has no previousSignatureCarriedOver");
                return new SignedLinks(counter, flag, ArchiveEntry.String(root, "previousSignature"), ArchiveEntry.String(root, "siret"), ArchiveEntry.String(root, "csn"), null);
            }
        }
    }
}
