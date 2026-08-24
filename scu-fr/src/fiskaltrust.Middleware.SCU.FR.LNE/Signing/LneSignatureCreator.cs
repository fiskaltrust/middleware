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
                return key;
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
}
