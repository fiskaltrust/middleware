using AutoFixture;
using fiskaltrust.ifPOS.v2.Cases;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.Models.Cases.Tests;

public class PayItemCaseTests
{
    private readonly IFixture _fixture;

    public PayItemCaseTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void FuzzTest_WithFlag()
    {
        for (var i = 0; i < 1000; i++)
        {
            var payItemCase = _fixture.Create<PayItemCase>();
            var payItemCaseFlag = _fixture.Create<PayItemCaseFlags>();

            var result = payItemCase.WithFlag(payItemCaseFlag);

            result.IsFlag(payItemCaseFlag).Should().BeTrue();
        }
    }

    [Fact]
    public void FuzzTest_WithCase()
    {
        for (var i = 0; i < 1000; i++)
        {
            var payItemCase = _fixture.Create<PayItemCase>();
            var payItemCaseCase = _fixture.Create<PayItemCase>();

            var result = payItemCase.WithCase(payItemCaseCase);

            result.IsCase(payItemCaseCase).Should().BeTrue();
            result.Case().Should().Be(payItemCaseCase);
        }
    }

    [Fact]
    public void FuzzTest_Reset()
    {
        for (var i = 0; i < 1000; i++)
        {
            var payItemCase = _fixture.Create<PayItemCase>();

            var result = payItemCase.Reset();

            result.Should().Be((PayItemCase) (0xFFFF_F000_0000_0000 & (ulong) payItemCase));
        }
    }

    [Fact]
    public void FuzzTest_WithVersion()
    {
        for (var i = 0; i < 1000; i++)
        {
            var payItemCase = _fixture.Create<PayItemCase>();
            var version = (byte) (_fixture.Create<byte>() >> 4);

            var result = payItemCase.WithVersion(version);

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
            var payItemCase = _fixture.Create<PayItemCase>();

            var resultCode = payItemCase.WithCountry(code);
            resultCode.CountryCode().Should().Be(code);

            var resultString = payItemCase.WithCountry(country);
            resultString.Country().Should().Be(country);
        }
    }
}
