using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.Journal.FR.Archive.Packaging
{
    /// <summary>
    /// manifest.json of a main archive or a sub-archive (market-fr#133): the period, the generation date,
    /// the SIRET, the signing certificate, and each file with its hash and signature. The manifest itself
    /// is signed in manifest.sig (base64 signature over the SHA-256 of the manifest.json bytes).
    /// Provisional layout, pending the archive specification.
    /// </summary>
    public sealed class ArchiveManifest
    {
        public const string MainKind = "main";
        public const string SubKind = "sub";

        public string ManifestVersion { get; set; } = "1";

        /// <summary><see cref="MainKind"/> or <see cref="SubKind"/>.</summary>
        public string Kind { get; set; } = MainKind;

        /// <summary>For a sub-archive: the SCU certificate serial number it is cut on.</summary>
        public string? CertificateSerialNumber { get; set; }

        /// <summary>For a sub-archive: its archive format version.</summary>
        public string? FormatVersion { get; set; }

        /// <summary>For a sub-archive: which contiguous run of its SCU certificate and format version it is, from 1 (market-fr#262).</summary>
        public int? Run { get; set; }

        /// <summary>
        /// The column schema of the receipts, <c>InfoCert</c> or <c>LNE</c> (self-certified SCUs use <c>LNE</c>): it also tells a
        /// verifier how the receipt signatures are encoded when the certificate does not say (market-fr#262, #263).
        /// For a main archive, the schema of its newest sub-archive.
        /// </summary>
        public string? Schema { get; set; }

        public string Siret { get; set; } = string.Empty;

        public string? DocumentationUrl { get; set; }

        /// <summary>Inclusive start of the archived period, UTC.</summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>Exclusive end of the archived period, UTC.</summary>
        public DateTime PeriodEnd { get; set; }

        /// <summary>Moment of the first archived record, null for an empty archive (market-fr#146).</summary>
        public DateTime? FirstRecordMoment { get; set; }

        /// <summary>Moment of the last archived record, null for an empty archive (market-fr#146).</summary>
        public DateTime? LastRecordMoment { get; set; }

        public int RecordCount { get; set; }

        public DateTime GeneratedAt { get; set; }

        public ManifestCertificate SigningCertificate { get; set; } = new ManifestCertificate();

        public string HashAlgorithm { get; set; } = "SHA-256";

        public string SignatureAlgorithm { get; set; } = string.Empty;

        public List<ManifestFile> Files { get; set; } = new List<ManifestFile>();

        /// <summary>
        /// For a main archive: the signed index of its sub-archives (each also listed in <see cref="Files"/>),
        /// signed with the key of the newest sub-archive's SCU (market-fr#262).
        /// </summary>
        public List<ManifestSubArchive>? SubArchives { get; set; }

        /// <summary>
        /// For a sub-archive: per chain, its first entry and the previous signature that entry covers
        /// (market-fr#262, #121), so consecutive sub-archives can be checked to chain to each other.
        /// </summary>
        public List<ManifestChainAnchor>? ChainAnchors { get; set; }
    }

    public sealed class ManifestChainAnchor
    {
        /// <summary>The chain letter, as written into <c>ftReceiptIdentification</c>.</summary>
        public string Chain { get; set; } = string.Empty;

        /// <summary>The <c>ftReceiptIdentification</c> of the chain's first entry in this sub-archive.</summary>
        public string FirstReceiptIdentification { get; set; } = string.Empty;

        /// <summary>The signature that entry covers, verbatim; empty when the chain starts in this sub-archive.</summary>
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

    public sealed class ManifestSubArchive
    {
        public string FileName { get; set; } = string.Empty;

        public string CertificateSerialNumber { get; set; } = string.Empty;

        public string FormatVersion { get; set; } = string.Empty;

        /// <summary>The sub-archive's <see cref="ArchiveManifest.Run"/>.</summary>
        public int Run { get; set; }

        /// <summary>The sub-archive's <see cref="ArchiveManifest.Schema"/>.</summary>
        public string? Schema { get; set; }

        public int RecordCount { get; set; }

        /// <summary>Inclusive start of the sub-archive's period, UTC.</summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>Exclusive end of the sub-archive's period, UTC.</summary>
        public DateTime PeriodEnd { get; set; }

        public DateTime? FirstRecordMoment { get; set; }

        public DateTime? LastRecordMoment { get; set; }

        /// <summary>Base64 SHA-256 of the sub-archive zip, the same value as its <see cref="ArchiveManifest.Files"/> entry.</summary>
        public string Hash { get; set; } = string.Empty;
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
