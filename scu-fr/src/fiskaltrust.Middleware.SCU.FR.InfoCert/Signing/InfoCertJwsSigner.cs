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

    /// <summary>OID of secp256r1 / NIST P-256, the only curve ES256 is defined over.</summary>
    private const string NistP256Oid = "1.2.840.10045.3.1.7";

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
            "(BEGIN PRIVATE KEY) or SEC1 (BEGIN EC PRIVATE KEY) encoded key — both carry the public point the " +
            "signature verification needs. A bare private scalar, as stored in ftSignaturCreationUnitFR by the v1 " +
            "QueueFR localization, has to be converted first.");
    }

    private static void ImportPkcs8(ECDsa key, byte[] keyMaterial) => key.ImportPkcs8PrivateKey(keyMaterial, out _);

    private static void ImportSec1(ECDsa key, byte[] keyMaterial) => key.ImportECPrivateKey(keyMaterial, out _);

    /// <summary>
    /// The JWS header unconditionally declares ES256, which is defined over P-256 only (RFC 7518
    /// section 3.4). A key on another curve would import happily and then produce a signature of the
    /// wrong size, which every standards-compliant verifier rejects - so refuse it up front.
    /// </summary>
    private static void EnsureNistP256(ECDsa key)
    {
        var curve = key.ExportParameters(false).Curve;
        var isNistP256 = key.KeySize == 256
            && (!curve.IsNamed || curve.Oid.Value == NistP256Oid || curve.Oid.FriendlyName == "nistP256" || curve.Oid.FriendlyName == "ECDSA_P256");

        if (!isNistP256)
        {
            throw new FRCertificateException($"The configured PrivateKey is not a secp256r1 (NIST P-256) key: {curve.Oid.FriendlyName ?? curve.Oid.Value} with a key size of {key.KeySize} bits. NF525 signatures are created as ES256, which is defined over P-256 only.");
        }
    }

    private static string ToBase64Url(byte[] data) => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
