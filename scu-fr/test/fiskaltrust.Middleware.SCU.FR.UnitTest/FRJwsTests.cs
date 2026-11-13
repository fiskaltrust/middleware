using System;
using System.Text;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest;

/// <summary>The compact JWS every FR SCU's QR code is (market-fr#221): header with kid, the signed string as payload.</summary>
public class FRJwsTests
{
    [Fact]
    public void Header_NamesTheCertificateSerialAsKid()
    {
        Encoding.UTF8.GetString(FRJws.FromBase64Url(FRJws.Header(FRJws.ES256, "CERT-4711"))).Should().Be("{\"alg\":\"ES256\",\"typ\":\"JWT\",\"kid\":\"CERT-4711\"}");
        Encoding.UTF8.GetString(FRJws.FromBase64Url(FRJws.Header(FRJws.ES256, null))).Should().Be("{\"alg\":\"ES256\",\"typ\":\"JWT\"}");
    }

    [Fact]
    public void SignedJws_DecodesBackToTheSignedString_AndVerifies()
    {
        var (_, key) = FRTestData.CreateKey();
        using var _ = key;
        const string signed = "2000:1200|0550:550,1750,20260301100000,T1,VENTE,N,";

        var (jws, signature) = FRJws.SignES256(key, signed, "CERT-4711");
        var parts = FRJws.Decode(jws);

        parts.SignedString.Should().Be(signed);
        parts.KeyId.Should().Be("CERT-4711");
        parts.Algorithm.Should().Be(FRJws.ES256);
        parts.Signature.Should().Be(signature).And.Be(FRChaining.ChainedSignatureOf(jws));
        FRJws.VerifyES256(jws, key).Should().BeTrue();
    }

    [Fact]
    public void ChangedPayload_FailsVerification()
    {
        var (_, key) = FRTestData.CreateKey();
        using var _ = key;
        var (jws, _) = FRJws.SignES256(key, "a,b,N,", null);
        var segments = jws.Split('.');
        var forged = $"{segments[0]}.{FRChaining.ToBase64Url(Encoding.UTF8.GetBytes("a,c,N,"))}.{segments[2]}";

        FRJws.VerifyES256(forged, key).Should().BeFalse();
    }

    [Theory]
    [InlineData("a.b")]
    [InlineData("a.b.c.d")]
    public void Decode_RefusesWhatIsNoCompactJws(string value)
    {
        FRJws.IsJws(value).Should().BeFalse();
        var act = () => FRJws.Decode(value);
        act.Should().Throw<FormatException>();
    }
}
