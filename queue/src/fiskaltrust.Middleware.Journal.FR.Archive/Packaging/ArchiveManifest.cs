using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.Journal.FR.Archive.Packaging
{
    /// <summary>
    /// <c>manifest.json</c> of an archive file, the one manifest kind (contract section 10.2, ADR 0019): which entries
    /// the file holds and why it ends, the SCU certificate that signed them, and each file with its hash and signature.
    /// The manifest itself is signed in <c>manifest.sig</c> (base64 signature over the SHA-256 of the manifest.json bytes).
    /// camelCase keys, absent keys omitted.
    /// </summary>
    public sealed class ArchiveManifest
    {
        /// <summary>A string, as TaxAuditorTool reads it; the contract writes it <c>1</c>.</summary>
        public string ManifestVersion { get; set; } = "1";

        /// <summary>
        /// The column schema of the receipts, <c>InfoCert</c> or <c>LNE</c> (self-certified SCUs use <c>LNE</c>): it also tells a
        /// verifier how the receipt signatures are encoded when the certificate does not say (market-fr#262, #263).
        /// </summary>
        public string? Schema { get; set; }

        /// <summary>The serial number of the SCU certificate that signed the file's entries and the file.</summary>
        public string? CertificateSerialNumber { get; set; }

        public string? FormatVersion { get; set; }

        public string Siret { get; set; } = string.Empty;

        public string? DocumentationUrl { get; set; }

        public DateTime GeneratedAt { get; set; }

        /// <summary>Inclusive start of the file's slice, UTC; absent on a fixed file, which is bounded by records.</summary>
        public DateTime? PeriodStart { get; set; }

        /// <summary>Exclusive end of the file's slice, UTC, never more than a year after <see cref="PeriodStart"/>.</summary>
        public DateTime? PeriodEnd { get; set; }

        /// <summary>The whole requested period's start; absent on a fixed file.</summary>
        public DateTime? RequestStart { get; set; }

        public DateTime? RequestEnd { get; set; }

        /// <summary>The file's place among the files the request was split into, from 1; absent on a fixed file.</summary>
        public int? Part { get; set; }

        public int? PartCount { get; set; }

        /// <summary>The file's first entry in queue order.</summary>
        public ManifestRecord? FirstRecord { get; set; }

        /// <summary>The file's last entry in queue order.</summary>
        public ManifestRecord? LastRecord { get; set; }

        public int RecordCount { get; set; }

        /// <summary>Why the file ends: <c>periodEnd</c>, <c>lastSignedEntry</c>, <c>formatChange</c>, <c>yearLimit</c> or <c>archiveReceipt</c>.</summary>
        public string? ClosedBy { get; set; }

        /// <summary>On a fixed file at the one-year limit: the <c>ForcedArchive</c> action journal entry's id (ADR 0018).</summary>
        public string? ForcedArchive { get; set; }

        /// <summary>When <see cref="ClosedBy"/> is <c>archiveReceipt</c>: that receipt.</summary>
        public ManifestRecord? ArchiveReceipt { get; set; }

        public ManifestCertificate SigningCertificate { get; set; } = new ManifestCertificate();

        public string HashAlgorithm { get; set; } = "SHA-256";

        public string SignatureAlgorithm { get; set; } = string.Empty;

        public List<ManifestFile> Files { get; set; } = new List<ManifestFile>();

        /// <summary>
        /// Per chain present in the file: its first entry and the previous signature that entry covers
        /// (market-fr#262, #121), so consecutive files can be checked to chain to each other.
        /// </summary>
        public List<ManifestChainAnchor>? ChainAnchors { get; set; }
    }

    /// <summary>An entry as the manifest names it: <c>{ftReceiptIdentification, ftQueueItemID, ftReceiptMoment}</c>.</summary>
    public sealed class ManifestRecord
    {
        [JsonPropertyName("ftReceiptIdentification")]
        public string ReceiptIdentification { get; set; } = string.Empty;

        [JsonPropertyName("ftQueueItemID")]
        public Guid QueueItemId { get; set; }

        [JsonPropertyName("ftReceiptMoment")]
        public DateTime ReceiptMoment { get; set; }
    }

    public sealed class ManifestChainAnchor
    {
        /// <summary>The chain letter, as written into <c>ftReceiptIdentification</c>.</summary>
        public string Chain { get; set; } = string.Empty;

        /// <summary>The <c>ftReceiptIdentification</c> of the chain's first entry in this file.</summary>
        public string FirstReceiptIdentification { get; set; } = string.Empty;

        /// <summary>The signature that entry covers, verbatim; empty when the chain starts in this file.</summary>
        public string PreviousSignature { get; set; } = string.Empty;
    }

    public sealed class ManifestCertificate
    {
        /// <summary>Name of the DER certificate file in the same archive.</summary>
        public string FileName { get; set; } = string.Empty;

        public string SerialNumber { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Issuer { get; set; } = string.Empty;

        /// <summary>Base64 SHA-256 of the DER certificate.</summary>
        public string Sha256Thumbprint { get; set; } = string.Empty;
    }

    public sealed class ManifestFile
    {
        public string Name { get; set; } = string.Empty;

        public long Size { get; set; }

        /// <summary>Base64 SHA-256 of the file bytes.</summary>
        public string Hash { get; set; } = string.Empty;

        /// <summary>Base64 signature over <see cref="Hash"/>.</summary>
        public string Signature { get; set; } = string.Empty;
    }

    internal static class ManifestSerializer
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        public static byte[] Serialize(ArchiveManifest manifest) => JsonSerializer.SerializeToUtf8Bytes(manifest, Options);

        public static ArchiveManifest Deserialize(byte[] json)
            => JsonSerializer.Deserialize<ArchiveManifest>(json, Options) ?? throw new JsonException("Empty manifest.");
    }
}
