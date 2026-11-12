using System.Security.Cryptography.X509Certificates;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Journal.FR.Archive.Signing;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// Signs an archive the queue exports with the queue's SCU (ADR 0007 amendment,
/// fiskaltrust/market-fr#218). The cloud archive generation signs with the fiskaltrust archive
/// certificate instead; it has no SCU key.
/// </summary>
/// <remarks>
/// The SCU reports a failure in its result, never by throwing, because it may run behind REST. An
/// exception from the SCU call itself (the SCU unreachable) gets the same refusal: either way the
/// archive is not signed.
/// </remarks>
public sealed class ScuArchiveSigner : IArchiveSigner
{
    private const string CannotSign = "The queue's SCU cannot sign the archive export (ADR 0007)";

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
        catch (Exception ex)
        {
            throw new NotSupportedException($"{CannotSign}: {ex.Message}", ex);
        }

        if (signingCertificate.Failure is not null)
        {
            throw new NotSupportedException($"{CannotSign}: {signingCertificate.Failure}");
        }

        if (signingCertificate.SignatureAlgorithm != ArchiveSignatureAlgorithms.ES256)
        {
            throw new NotSupportedException($"The queue's SCU signs with {signingCertificate.SignatureAlgorithm}, which the archive package does not read.");
        }

        X509Certificate2 certificate;
        try
        {
            certificate = new X509Certificate2(Convert.FromBase64String(signingCertificate.CertificateBase64 ?? throw new FormatException("no certificate")));
        }
        catch (Exception ex) when (ex is FormatException || ex is System.Security.Cryptography.CryptographicException)
        {
            throw new NotSupportedException($"{CannotSign}: the SCU's certificate cannot be read ({ex.Message}).", ex);
        }
        return new ScuArchiveSigner(scu, certificate, signingCertificate.CertificateSerialNumber, signingCertificate.SignatureAlgorithm);
    }

    /// <exception cref="InvalidOperationException">The SCU did not sign the hash.</exception>
    public async Task<byte[]> SignHashAsync(byte[] sha256, CancellationToken cancellationToken)
    {
        FRHashSignature signature;
        try
        {
            signature = await _scu.SignHashAsync(sha256).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{CannotSign}: {ex.Message}", ex);
        }

        if (signature.Failure is not null)
        {
            throw new InvalidOperationException($"{CannotSign}: {signature.Failure}");
        }
        return signature.Signature is { Length: > 0 } bytes
            ? bytes
            : throw new InvalidOperationException($"{CannotSign}: the SCU returned no signature.");
    }
}
