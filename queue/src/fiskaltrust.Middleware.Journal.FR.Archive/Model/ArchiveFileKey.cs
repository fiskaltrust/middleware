using System;

namespace fiskaltrust.Middleware.Journal.FR.Archive.Model
{
    /// <summary>
    /// What an archive file holds one of (ADR 0019): the serial number of the SCU certificate that signed its entries,
    /// and its archive format version. A change of either ends the file; the chains never restart at a switch.
    /// </summary>
    public sealed class ArchiveFileKey : IEquatable<ArchiveFileKey>
    {
        public ArchiveFileKey(string certificateSerialNumber, ArchiveFormatVersion formatVersion)
        {
            if (string.IsNullOrWhiteSpace(certificateSerialNumber))
            {
                throw new ArgumentException("A certificate serial number must not be empty.", nameof(certificateSerialNumber));
            }
            CertificateSerialNumber = certificateSerialNumber;
            FormatVersion = formatVersion ?? throw new ArgumentNullException(nameof(formatVersion));
        }

        public string CertificateSerialNumber { get; }

        public ArchiveFormatVersion FormatVersion { get; }

        public bool Equals(ArchiveFileKey? other)
            => other is not null
               && string.Equals(CertificateSerialNumber, other.CertificateSerialNumber, StringComparison.OrdinalIgnoreCase)
               && FormatVersion.Equals(other.FormatVersion);

        public override bool Equals(object? obj) => Equals(obj as ArchiveFileKey);

        public override int GetHashCode()
            => unchecked((StringComparer.OrdinalIgnoreCase.GetHashCode(CertificateSerialNumber) * 397) ^ FormatVersion.GetHashCode());

        public override string ToString() => $"{CertificateSerialNumber}/v{FormatVersion}";
    }
}
