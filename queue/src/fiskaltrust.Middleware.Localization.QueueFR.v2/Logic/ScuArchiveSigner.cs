using System.Security.Cryptography.X509Certificates;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Signing;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// Signs an archive the queue exports with the queue's SCU (ADR 0007 amendment,
/// fiskaltrust/market-fr#218). The cloud archive generation signs with the fiskaltrust archive
/// certificate instead; it has no SCU key.
/// </summary>
public sealed class ScuArchiveSigner : IArchiveSigner
{
    private readonly IFRSSCD _scu;

    private ScuArchiveSigner(IFRSSCD scu, X509Certificate2 certificate, string certificateSerialNumber, string signatureAlgorithm)
    {
        _scu = scu;
        Certificate = certificate;
        CertificateSerialNumber = certificateSerialNumber;
        SignatureAlgorithm = signatureAlgorithm;
    }

    public X509Certificate2 Certificate { get; }

    /// <summary>The serial the SCU prints on its entries, so the sub-archive of those entries can embed the certificate.</summary>
    public string CertificateSerialNumber { get; }

    public string SignatureAlgorithm { get; }

    /// <exception cref="NotSupportedException">The SCU does not sign archives, or has no usable certificate.</exception>
    public static async Task<ScuArchiveSigner> CreateAsync(IFRSSCD scu)
    {
        FRSigningCertificate signingCertificate;
        try
        {
            signingCertificate = await scu.GetSigningCertificateAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is NotSupportedException or FRSSCDException)
        {
            throw new NotSupportedException($"The queue's SCU cannot sign the archive export (ADR 0007): {ex.Message}", ex);
        }

        if (signingCertificate.SignatureAlgorithm != ArchiveSignatureAlgorithms.ES256)
        {
            throw new NotSupportedException($"The queue's SCU signs with {signingCertificate.SignatureAlgorithm}, which the archive package does not read.");
        }

        return new ScuArchiveSigner(scu, new X509Certificate2(Convert.FromBase64String(signingCertificate.CertificateBase64)), signingCertificate.CertificateSerialNumber, signingCertificate.SignatureAlgorithm);
    }

    public Task<byte[]> SignHashAsync(byte[] sha256, CancellationToken cancellationToken) => _scu.SignHashAsync(sha256);
}
