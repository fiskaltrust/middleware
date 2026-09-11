using System;
using System.Security.Cryptography;
using System.Text;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.FR.LNE.Signing;

/// <summary>
/// Signs the LNE data set with SHA-256withECDSA over secp256r1 and links it into the chain.
/// </summary>
/// <remarks>
/// Two deliberate differences to the Infocert SCU, both dictated by how the respective referential
/// describes the archive format: the signature is emitted as a plain base64 DER sequence rather than
/// wrapped in a JWS, and the chain hash covers the data set <em>including</em> its signature, so an
/// altered signature breaks the chain of every following entry.
/// </remarks>
internal sealed class LneSignatureCreator : IDisposable
{
    /// <summary>OID of secp256r1 / NIST P-256, the curve the French signature is defined over.</summary>
    private const string NistP256Oid = "1.2.840.10045.3.1.7";

    private readonly ECDsa _key;

    public LneSignatureCreator(string privateKeyBase64)
    {
        _key = ImportKey(privateKeyBase64);
    }

    public (string signature, string chainHash) Sign(string dataSet)
    {
        var dataSetBytes = LneDataSetBuilder.ToBytes(dataSet);
        var signature = _key.SignData(dataSetBytes, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        var signatureBase64 = Convert.ToBase64String(signature);

        var chainInput = Encoding.UTF8.GetBytes($"{dataSet}{LneDataSetBuilder.FieldSeparator}{signatureBase64}");
        return (signatureBase64, Convert.ToBase64String(SHA256.HashData(chainInput)));
    }

    public void Dispose() => _key.Dispose();

    private static ECDsa ImportKey(string privateKeyBase64)
    {
        byte[] keyMaterial;
        try
        {
            keyMaterial = Convert.FromBase64String(privateKeyBase64);
        }
        catch (FormatException ex)
        {
            throw new FRCertificateException("The configured PrivateKey is not valid base64.", ex);
        }

        var key = ECDsa.Create();
        foreach (var import in new Action<ECDsa, byte[]>[] { ImportPkcs8, ImportSec1 })
        {
            try
            {
                import(key, keyMaterial);
                EnsureNistP256(key);
                return key;
            }
            catch (FRCertificateException)
            {
                key.Dispose();
                throw;
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException)
            {
                // Try the next encoding.
            }
        }

        key.Dispose();
        throw new FRCertificateException(
            "The configured PrivateKey could not be read as a secp256r1 key. Provide it as base64 of a PKCS#8 " +
            "(BEGIN PRIVATE KEY) or SEC1 (BEGIN EC PRIVATE KEY) encoded key. A bare private scalar, as stored in " +
            "ftSignaturCreationUnitFR by the v1 QueueFR localization, has to be converted first.");
    }

    private static void ImportPkcs8(ECDsa key, byte[] keyMaterial) => key.ImportPkcs8PrivateKey(keyMaterial, out _);

    private static void ImportSec1(ECDsa key, byte[] keyMaterial) => key.ImportECPrivateKey(keyMaterial, out _);

    /// <summary>
    /// The French signature is SHA-256withECDSA over secp256r1. A key on another curve would import
    /// happily and then produce a signature an auditor cannot verify against the declared algorithm,
    /// so refuse it up front.
    /// </summary>
    private static void EnsureNistP256(ECDsa key)
    {
        var curve = key.ExportParameters(false).Curve;
        var isNistP256 = key.KeySize == 256
            && (!curve.IsNamed || curve.Oid.Value == NistP256Oid || curve.Oid.FriendlyName == "nistP256" || curve.Oid.FriendlyName == "ECDSA_P256");

        if (!isNistP256)
        {
            throw new FRCertificateException($"The configured PrivateKey is not a secp256r1 (NIST P-256) key: {curve.Oid.FriendlyName ?? curve.Oid.Value} with a key size of {key.KeySize} bits. NF525 signatures are created as SHA-256withECDSA over P-256.");
        }
    }
}
