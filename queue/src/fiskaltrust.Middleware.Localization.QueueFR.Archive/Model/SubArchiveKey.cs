using System;

namespace fiskaltrust.Middleware.Localization.QueueFR.Archive.Model
{
    /// <summary>
    /// What a sub-archive is cut on: the serial number of the SCU certificate that signed its entries,
    /// and its archive format version (ADR 0005). The chains never restart at a switch; the archive splits.
    /// </summary>
    public sealed class SubArchiveKey : IEquatable<SubArchiveKey>
    {
        public SubArchiveKey(string certificateSerialNumber, ArchiveFormatVersion formatVersion)
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

        public bool Equals(SubArchiveKey? other)
            => other is not null
               && string.Equals(CertificateSerialNumber, other.CertificateSerialNumber, StringComparison.OrdinalIgnoreCase)
               && FormatVersion.Equals(other.FormatVersion);

        public override bool Equals(object? obj) => Equals(obj as SubArchiveKey);

        public override int GetHashCode()
            => unchecked((StringComparer.OrdinalIgnoreCase.GetHashCode(CertificateSerialNumber) * 397) ^ FormatVersion.GetHashCode());

        public override string ToString() => $"{CertificateSerialNumber}/v{FormatVersion}";
    }
}
