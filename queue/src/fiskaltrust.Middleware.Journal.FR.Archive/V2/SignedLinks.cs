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
        private const int LneField20 = 19;

        // FRCanonicalRequest.SignedField: the canonical-form version, a colon, the padded Base64 SHA-256 (ADR 0013).
        private static readonly Regex SignedRequestHashField = new Regex("^[0-9]+:[A-Za-z0-9+/]{43}=$", RegexOptions.CultureInvariant);

        private SignedLinks(long chainCounter, string flag, string? previousSignature, string? siret, string? certificateSerialNumber, string? requestHash, IssuerClaims? issuer = null, bool jws = false, string? field20 = null)
        {
            Field20 = string.IsNullOrEmpty(field20) ? null : field20;
            Jws = jws;
            Issuer = issuer ?? IssuerClaims.None;
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

        /// <summary>True when the signed content is a JWS (the InfoCert SCU), false for a data set.</summary>
        public bool Jws { get; }

        /// <summary>
        /// Field 20 of the LNE and self-certified data set, as signed (archive-v2 §5, market-fr#221): the archive boundaries,
        /// the migration event or the closed periods, by receipt case; null when empty or not a 25-field data set.
        /// </summary>
        public string? Field20 { get; }

        public long ChainCounter { get; }

        public bool StartOfChain { get; }

        public string? PreviousSignature { get; }

        /// <summary>Null when the signed content carries none (an InfoCert JET payload).</summary>
        public string? Siret { get; }

        public string? CertificateSerialNumber { get; }

        public string? RequestHash { get; }

        /// <summary>
        /// The issuer claims the SCU signed as configured (archive-v2 §1.1, fiskaltrust/middleware#827): LNE data-set fields 2 to 6,
        /// InfoCert payload keys <c>rcs</c>, <c>naf</c>, <c>legalform</c>, <c>vatoption</c>, <c>elecaddress</c>. Empty when not signed.
    /// The LNE data set's field 6 is reserved and empty since market-fr#221 (it was BT-34); entries signed
    /// before still read it here. The archive's <c>ElectronicAddress</c> column no longer uses it.
        /// </summary>
        public IssuerClaims Issuer { get; }

        /// <summary>Reads the signed content of a receipt QR code.</summary>
        /// <param name="queueItemId">The entry's queue item, checked against the one an LNE data set signs; null to skip the check.</param>
        /// <param name="receiptIdentification">The entry's <c>ftReceiptIdentification</c>: an R13/R12/R19 string signs it as its
        /// identification field, which gives the chain counter (market-fr#221).</param>
        /// <param name="receiptCase">The entry's <c>ftReceiptCase</c>: it chooses which R13/R12/R19 string a JWS payload is.</param>
        /// <exception cref="FormatException">The signed content lacks a field, or a field does not read.</exception>
        public static SignedLinks Parse(string qrCode, Guid? queueItemId, string? receiptIdentification = null, long? receiptCase = null)
        {
            // A compact JWS first: its payload may be an R13/R12 string, which holds '|' (the VAT breakdown) once decoded.
            var segments = qrCode.Split('.');
            if (segments.Length == 3)
            {
                return FromJws(segments[0], segments[1], queueItemId, receiptIdentification, receiptCase);
            }
            var dot = qrCode.LastIndexOf('.');
            var content = dot < 0 ? "" : qrCode.Substring(0, dot);
            return content.IndexOf('|') >= 0
                ? FromDataSet(content, queueItemId)
                : throw new FormatException("the signed content is neither a data set nor a JWS");
        }

        /// <summary>
        /// The signing certificate's serial number of a receipt QR code: a JWS header's <c>kid</c>, else what the signed
        /// content carries (a pre-<c>kid</c> InfoCert payload's <c>csn</c>, the data set's fourth-from-last field).
        /// </summary>
        public static string? CertificateSerialNumberOf(string qrCode)
        {
            var segments = qrCode.Split('.');
            if (segments.Length == 3 && KeyId(segments[0]) is { } kid)
            {
                return kid;
            }
            if (segments.Length == 3 && TryDecode(segments[1], out var payload) && IsJson(payload))
            {
                using var document = JsonDocument.Parse(payload);
                return document.RootElement.ValueKind == JsonValueKind.Object ? ArchiveEntry.String(document.RootElement, "csn") : null;
            }
            return segments.Length == 3 ? null : Parse(qrCode, null).CertificateSerialNumber;
        }

        private static SignedLinks FromDataSet(string dataSet, Guid? queueItemId, bool jws = false, string? keyId = null)
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
            var issuer = n == LneDataSetFieldCount ? new IssuerClaims(fields[1], fields[2], fields[3], fields[4], fields[5]) : null;
            return new SignedLinks(counter, fields[n - 2], fields[n - 1], fields[0], keyId ?? fields[n - 4], requestHash, issuer, jws, field20: n == LneDataSetFieldCount ? fields[LneField20] : null);
        }

        /// <summary>
        /// A compact JWS, whatever SCU signed it. Its payload is one of:
        /// <list type="bullet">
        /// <item>a JSON object (InfoCert entries signed before market-fr#221, kept readable): <c>chainCounter</c>,
        /// <c>previousSignatureCarriedOver</c>, <c>previousSignature</c>, <c>siret</c>, <c>csn</c>, the issuer claims;</item>
        /// <item>the 25-field data set, recognised by its 25 <c>|</c> fields with the entry's identification at field 11;</item>
        /// <item>an R13/R12/R19 string (InfoCert): <c>,</c>-separated, the flag and the previous signature last, the chain
        /// counter in the identification field, which is <c>ftReceiptIdentification</c>. No SIRET, serial number, request
        /// value or issuer claim is signed in it.</item>
        /// </list>
        /// The header's <c>kid</c>, when present, is the signing certificate's serial number.
        /// </summary>
        private static SignedLinks FromJws(string header, string payloadSegment, Guid? queueItemId, string? receiptIdentification, long? receiptCase)
        {
            var keyId = KeyId(header);
            if (!TryDecode(payloadSegment, out var payload))
            {
                throw new FormatException("the JWS payload is no Base64Url UTF-8");
            }
            if (IsJson(payload))
            {
                return FromJsonPayload(payload, keyId);
            }
            var fields = payload.Split('|');
            if (fields.Length == LneDataSetFieldCount && (receiptIdentification is null || fields[10] == receiptIdentification))
            {
                return FromDataSet(payload, queueItemId, jws: true, keyId: keyId);
            }
            return FromReferentialString(payload, keyId, receiptIdentification, receiptCase);
        }

        /// <summary>An R13/R12/R19 string (market-fr#108, #221); the shape per receipt case is checked, then the chain fields read.</summary>
        private static SignedLinks FromReferentialString(string text, string? keyId, string? receiptIdentification, long? receiptCase)
        {
            var fields = text.Split(',');
            var n = fields.Length;
            if (n < 3)
            {
                throw new FormatException($"the signed string has {n} fields, fewer than an identification, a flag and a previous signature");
            }
            var kind = receiptCase is { } rc ? KindOf(rc) : (ReferentialString?) null;
            var shapeOk = kind switch
            {
                ReferentialString.NoteTicket => n == 7,
                ReferentialString.PaymentProof => n == 6,
                ReferentialString.Invoice => n == 10,
                ReferentialString.Jet => n == 8,
                ReferentialString.Archive => n == 29,
                ReferentialString.Duplicate => n == 8 || n == 9,
                ReferentialString.GrandTotals => n >= 7 && (n - 2) % 5 == 0,
                _ => true,
            };
            if (!shapeOk)
            {
                throw new FormatException($"the {kind} string has {n} fields");
            }
            var identification = kind switch
            {
                ReferentialString.NoteTicket or ReferentialString.Invoice => fields[3],
                ReferentialString.GrandTotals => fields[4].Split(':')[0],
                ReferentialString.PaymentProof or ReferentialString.Jet or ReferentialString.Archive or ReferentialString.Duplicate => fields[0],
                _ => receiptIdentification ?? "",
            };
            if (receiptIdentification is not null && identification != receiptIdentification)
            {
                throw new FormatException($"the string is signed for '{identification}', not '{receiptIdentification}'");
            }
            if (!ArchiveEntry.TryReadChain(identification, out _, out var counter))
            {
                throw new FormatException($"the signed identification '{identification}' names no chain and counter");
            }
            return new SignedLinks(counter, fields[n - 2], fields[n - 1], null, keyId, null, null, jws: true);
        }

        /// <summary>The R13/R12/R19 string of a receipt case, as the Infocert SCU chooses it (training flag ignored).</summary>
        private enum ReferentialString { NoteTicket, PaymentProof, Invoice, Duplicate, GrandTotals, Archive, Jet, DataSet }

        private static ReferentialString KindOf(long receiptCase)
        {
            var training = (receiptCase & 0x0000_0000_0002_0000) != 0;
            switch (receiptCase & 0xFFFF)
            {
                case 0x0000: case 0x0001: case 0x0004: case 0x0003: case 0x0005: case 0x0006: case 0x3004: return ReferentialString.NoteTicket;
                case 0x0002: case 0x3005: return ReferentialString.PaymentProof;
                case 0x0007: case 0x1000: case 0x1001: case 0x1002: case 0x1003: return ReferentialString.Invoice;
                case 0x3010: return ReferentialString.Duplicate;
                case 0x2010: case 0x2011: case 0x2012: case 0x2013: return ReferentialString.GrandTotals;
                case 0x3011: return ReferentialString.Archive;
                case 0x3001: return training ? ReferentialString.DataSet : ReferentialString.Jet;
                default: return ReferentialString.DataSet;
            }
        }

        private static string? KeyId(string header)
        {
            if (!TryDecode(header, out var json) || !IsJson(json))
            {
                return null;
            }
            try
            {
                using var document = JsonDocument.Parse(json);
                return document.RootElement.ValueKind == JsonValueKind.Object && ArchiveEntry.String(document.RootElement, "kid") is { Length: > 0 } kid ? kid : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static bool TryDecode(string segment, out string text)
        {
            try
            {
                var padded = segment.Replace('-', '+').Replace('_', '/');
                padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
                text = new System.Text.UTF8Encoding(false, true).GetString(Convert.FromBase64String(padded));
                return true;
            }
            catch (Exception ex) when (ex is FormatException || ex is ArgumentException)
            {
                text = "";
                return false;
            }
        }

        private static bool IsJson(string text) => text.TrimStart().StartsWith("{", StringComparison.Ordinal);

        private static SignedLinks FromJsonPayload(string payload, string? keyId)
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(payload);
            }
            catch (JsonException ex)
            {
                throw new FormatException("the JWS payload is no JSON", ex);
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
                var issuer = new IssuerClaims(ArchiveEntry.String(root, "rcs"), ArchiveEntry.String(root, "naf"), ArchiveEntry.String(root, "legalform"),
                    ArchiveEntry.String(root, "vatoption"), ArchiveEntry.String(root, "elecaddress"));
                return new SignedLinks(counter, flag, ArchiveEntry.String(root, "previousSignature"), ArchiveEntry.String(root, "siret"), keyId ?? ArchiveEntry.String(root, "csn"), null, issuer, jws: true);
            }
        }
    }

    /// <summary>The issuer claims an FR SCU signs (archive-v2 §1.1); a claim not configured is null.</summary>
    public sealed class IssuerClaims
    {
        public static readonly IssuerClaims None = new IssuerClaims(null, null, null, null, null);

        public IssuerClaims(string? rcs, string? naf, string? legalForm, string? vatOption, string? electronicAddress)
        {
            Rcs = string.IsNullOrEmpty(rcs) ? null : rcs;
            Naf = string.IsNullOrEmpty(naf) ? null : naf;
            LegalForm = string.IsNullOrEmpty(legalForm) ? null : legalForm;
            VatOption = string.IsNullOrEmpty(vatOption) ? null : vatOption;
            ElectronicAddress = string.IsNullOrEmpty(electronicAddress) ? null : electronicAddress;
        }

        public string? Rcs { get; }

        public string? Naf { get; }

        public string? LegalForm { get; }

        public string? VatOption { get; }

        public string? ElectronicAddress { get; }
    }
}
