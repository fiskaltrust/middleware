using System;
using System.Security.Cryptography;
using System.Text;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.FR.InfoCert.Signing;

/// <summary>
/// Produces the Infocert receipt signature: an ES256 JWS in compact serialization over the json
/// receipt payload. The chain hash returned alongside is the SHA-256 of the JWS signing input,
/// which is what the next entry of the same chain covers.
/// </summary>
internal sealed class InfoCertJwsSigner : IDisposable
{
    /// <summary>Base64url of <c>{"alg":"ES256","typ":"JWT"}</c>.</summary>
    private const string Es256JwsHeader = "eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCJ9";

    private readonly ECDsa _key;

    public InfoCertJwsSigner(string privateKeyBase64)
    {
        _key = ImportKey(privateKeyBase64);
    }

    public (string jws, string chainHash) Sign(string payloadJson)
    {
        var signingInput = $"{Es256JwsHeader}.{ToBase64Url(Encoding.UTF8.GetBytes(payloadJson))}";
        var signingInputBytes = Encoding.UTF8.GetBytes(signingInput);

        // JWS mandates the raw R||S form (RFC 7518 §3.4), not the DER sequence.
        var signature = _key.SignData(signingInputBytes, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        var chainHash = SHA256.HashData(signingInputBytes);

        return ($"{signingInput}.{ToBase64Url(signature)}", Convert.ToBase64String(chainHash));
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
            "(BEGIN PRIVATE KEY) or SEC1 (BEGIN EC PRIVATE KEY) encoded key — both carry the public point the " +
            "signature verification needs. A bare private scalar, as stored in ftSignaturCreationUnitFR by the v1 " +
            "QueueFR localization, has to be converted first.");
    }

    private static void ImportPkcs8(ECDsa key, byte[] keyMaterial) => key.ImportPkcs8PrivateKey(keyMaterial, out _);

    private static void ImportSec1(ECDsa key, byte[] keyMaterial) => key.ImportECPrivateKey(keyMaterial, out _);

    private static string ToBase64Url(byte[] data) => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
