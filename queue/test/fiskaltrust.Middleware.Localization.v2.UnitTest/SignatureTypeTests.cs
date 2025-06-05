using AutoFixture;
using fiskaltrust.Middleware.Localization.QueueES.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.ifPOS.v2.Cases;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.Models.Cases.Tests;

public class SignatureTypeTests
{
    private readonly IFixture _fixture;

    public SignatureTypeTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void FuzzTest_WithTypeES()
    {
        for (var i = 0; i < 1000; i++)
        {
            var signatureType = _fixture.Create<SignatureType>();
            var signatureTypeCase = _fixture.Create<SignatureTypeES>();

            var result = signatureType.WithType(signatureTypeCase);

            SignatureTypeESExt.Type(result).Should().Be((SignatureTypeES) ((long) signatureTypeCase & 0xFFFF));
            result.IsType(signatureTypeCase).Should().BeTrue();
        }
    }

    [Fact]
    public void FuzzTest_WithTypeGR()
    {
        for (var i = 0; i < 1000; i++)
        {
            var signatureType = _fixture.Create<SignatureType>();
            var signatureTypeCase = _fixture.Create<SignatureTypeGR>();

            var result = signatureType.WithType(signatureTypeCase);

            SignatureTypeGRExt.Type(result).Should().Be((SignatureTypeGR) ((long) signatureTypeCase & 0xFFFF));
            result.IsType(signatureTypeCase).Should().BeTrue();
        }
    }

    [Fact]
    public void FuzzTest_WithTypePT()
    {
        for (var i = 0; i < 1000; i++)
        {
            var signatureType = _fixture.Create<SignatureType>();
            var signatureTypeCase = _fixture.Create<SignatureTypePT>();

            var result = signatureType.WithType(signatureTypeCase);

            SignatureTypePTExt.Type(result).Should().Be((SignatureTypePT) ((long) signatureTypeCase & 0xFFFF));
            result.IsType(signatureTypeCase).Should().BeTrue();
        }
    }

    [Fact]
    public void FuzzTest_WithFlag()
    {
        for (var i = 0; i < 1000; i++)
        {
            var signatureType = _fixture.Create<SignatureType>();
            var signatureTypeFlag = _fixture.Create<SignatureTypeFlags>();

            var result = signatureType.WithFlag(signatureTypeFlag);

            result.IsFlag(signatureTypeFlag).Should().BeTrue();
        }
    }

    [Fact]
    public void FuzzTest_Reset()
    {
        for (var i = 0; i < 1000; i++)
        {
            var signatureType = _fixture.Create<SignatureType>();

            var result = signatureType.Reset();

            result.Should().Be((SignatureType) (0xFFFF_F000_0000_0000 & (ulong) signatureType));
        }
    }

    [Fact]
    public void FuzzTest_WithVersion()
    {
        for (var i = 0; i < 1000; i++)
        {
            var signatureType = _fixture.Create<SignatureType>();
            var version = (byte) (_fixture.Create<byte>() >> 4);

            var result = signatureType.WithVersion(version);

            result.Version().Should().Be(version);
        }
    }

    [Fact]
    public void FuzzTest_WithCountry()
    {
        foreach (var (country, code) in new List<(string, long)> {
            ("AT", 0x4154),
            ("DE", 0x4445),
            ("FR", 0x4652),
            ("IT", 0x4954),
            ("ME", 0x4D45),
            ("ES", 0x4752),
            ("GR", 0x4752),
            ("PT", 0x5054),
            })
        {
            var signatureType = _fixture.Create<SignatureType>();

            var resultCode = signatureType.WithCountry(code);
            resultCode.CountryCode().Should().Be(code);

            var resultString = signatureType.WithCountry(country);
            resultString.Country().Should().Be(country);
        }
    }
}
