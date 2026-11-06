using System;
using System.Formats.Asn1;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.InfoCert;
using fiskaltrust.Middleware.SCU.FR.LNE;
using fiskaltrust.Middleware.SCU.FR.SelfCertified;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest;

/// <summary>
/// The encoding of the receipt signature, the part of the QR code after its last <c>.</c>, per SCU
/// (decided on fiskaltrust/market-fr#192, 2026-11-05): LNE and self-certified sign in strict DER, as
/// v1.2 did, and InfoCert's JWS carries ES256 in R‖S form (RFC 7518 §3.4).
/// </summary>
public class SignatureEncodingTests
{
    private static async Task<byte[]> ReceiptSignatureOf(IFRSSCD scu)
    {
        var (response, _) = await scu.ProcessReceiptAsync(FRTestData.Request(), null);
        var qrCode = response.ReceiptResponse.ftSignatures!
            .Single(x => ((long) x.ftSignatureType & 0xFFFF) == ((long) SignatureTypeFR.ReceiptSignature & 0xFFFF)).Data;
        return FRTestData.FromBase64Url(qrCode.Substring(qrCode.LastIndexOf('.') + 1));
    }

    /// <summary>Parses an ECDSA signature as strict DER: SEQUENCE { INTEGER r, INTEGER s } and nothing after it.</summary>
    private static void ShouldBeStrictDer(byte[] signature)
    {
        var reader = new AsnReader(signature, AsnEncodingRules.DER);
        var sequence = reader.ReadSequence();
        sequence.ReadInteger();
        sequence.ReadInteger();
        sequence.ThrowIfNotEmpty();
        reader.ThrowIfNotEmpty();
    }

    [Fact]
    public async Task Lne_SignsTheReceiptInStrictDer()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(new LneConfiguration { Siret = FRTestData.Siret, PrivateKey = privateKey, CertificateSerialNumber = "CERT-4711" });

        var signature = await ReceiptSignatureOf(sut);

        var parse = () => ShouldBeStrictDer(signature);
        parse.Should().NotThrow("LNE v2 keeps v1.2's DER encoding (market-fr#192)");
        signature.Length.Should().NotBe(64);
    }

    [Fact]
    public async Task SelfCertified_SignsTheReceiptInStrictDer()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new SelfCertifiedFRSSCD(new SelfCertifiedConfiguration { Siret = FRTestData.Siret, PrivateKey = privateKey, CertificateSerialNumber = "CERT-4711" });

        var signature = await ReceiptSignatureOf(sut);

        var parse = () => ShouldBeStrictDer(signature);
        parse.Should().NotThrow("the self-certified SCU signs like LNE (market-fr#192)");
    }

    [Fact]
    public async Task InfoCert_SignsTheJwsInRAndSForm()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(new InfoCertConfiguration { Siret = FRTestData.Siret, PrivateKey = privateKey, CertificateSerialNumber = "CERT-4711" });

        var signature = await ReceiptSignatureOf(sut);

        signature.Should().HaveCount(64, "ES256 in a JWS is R‖S, two 32-byte integers (RFC 7518 §3.4)");
    }

    [Fact]
    public void StrictDer_RejectsABerEncodingOfTheSameSignature()
    {
        // SEQUENCE { INTEGER 1, INTEGER 1 } in DER, and the same value with a long-form length, which BER allows and DER forbids.
        var der = new byte[] { 0x30, 0x06, 0x02, 0x01, 0x01, 0x02, 0x01, 0x01 };
        var ber = new byte[] { 0x30, 0x81, 0x06, 0x02, 0x01, 0x01, 0x02, 0x01, 0x01 };

        ((Action) (() => ShouldBeStrictDer(der))).Should().NotThrow();
        ((Action) (() => ShouldBeStrictDer(ber))).Should().Throw<AsnContentException>();
    }
}
