using AutoFixture;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.Models.Cases.Tests;

public class SignatureFormatTests
{
    private readonly IFixture _fixture;

    public SignatureFormatTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void FuzzTest_WithFormat()
    {
        for (var i = 0; i < 1000; i++)
        {
            var signatureFormat = _fixture.Create<SignatureFormat>();
            var signatureFormatCase = _fixture.Create<SignatureFormat>();

            var result = signatureFormat.WithFormat(signatureFormatCase);

            result.Format().Should().Be(signatureFormatCase);
            result.IsFormat(signatureFormatCase).Should().BeTrue();
        }
    }

    [Fact]
    public void FuzzTest_WithFlag()
    {
        for (var i = 0; i < 1000; i++)
        {
            var signatureFormat = _fixture.Create<SignatureFormat>();
            var signatureFormatFlag = _fixture.Create<SignatureFormatPosition>();

            var result = signatureFormat.WithPosition(signatureFormatFlag);

            result.IsPosition(signatureFormatFlag).Should().BeTrue();
        }
    }

    [Fact]
    public void FuzzTest_Reset()
    {
        for (var i = 0; i < 1000; i++)
        {
            var signatureFormat = _fixture.Create<SignatureFormat>();

            var result = signatureFormat.Reset();

            result.Should().Be((SignatureFormat) (0xFFFF_F000_0000_0000 & (ulong) signatureFormat));
        }
    }

    [Fact]
    public void FuzzTest_WithVersion()
    {
        for (var i = 0; i < 1000; i++)
        {
            var signatureFormat = _fixture.Create<SignatureFormat>();
            var version = (byte) (_fixture.Create<byte>() >> 4);

            var result = signatureFormat.WithVersion(version);

            result.Version().Should().Be(version);
        }
    }
}
