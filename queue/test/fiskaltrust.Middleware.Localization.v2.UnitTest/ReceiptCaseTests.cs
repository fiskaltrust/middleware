using AutoFixture;
using fiskaltrust.ifPOS.v2.Cases;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.Models.Cases.Tests;

public class ReceiptCaseTests
{
    private readonly IFixture _fixture;

    public ReceiptCaseTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void FuzzTest_WithCase()
    {
        for (var i = 0; i < 1000; i++)
        {
            var receiptCase = _fixture.Create<ReceiptCase>();
            var receiptCaseCase = _fixture.Create<ReceiptCase>();

            var result = receiptCase.WithCase(receiptCaseCase);

            result.Case().Should().Be(receiptCaseCase);
            result.IsCase(receiptCaseCase).Should().BeTrue();
        }
    }

    [Fact]
    public void FuzzTest_WithType()
    {
        for (var i = 0; i < 1000; i++)
        {
            var receiptCase = _fixture.Create<ReceiptCase>();
            var receiptCaseType = _fixture.Create<ReceiptCaseType>();

            var result = receiptCase.WithType(receiptCaseType);

            result.Type().Should().Be(receiptCaseType);
            result.IsType(receiptCaseType).Should().BeTrue();
        }
    }

    [Fact]
    public void FuzzTest_WithFlag()
    {
        for (var i = 0; i < 1000; i++)
        {
            var receiptCase = _fixture.Create<ReceiptCase>();
            var receiptCaseFlag = _fixture.Create<ReceiptCaseFlags>();

            var result = receiptCase.WithFlag(receiptCaseFlag);

            result.IsFlag(receiptCaseFlag).Should().BeTrue();
        }
    }

    [Fact]
    public void FuzzTest_Reset()
    {
        for (var i = 0; i < 1000; i++)
        {
            var receiptCase = _fixture.Create<ReceiptCase>();

            var result = receiptCase.Reset();

            result.Should().Be((ReceiptCase) (0xFFFF_F000_0000_0000 & (ulong) receiptCase));
        }
    }

    [Fact]
    public void FuzzTest_WithVersion()
    {
        for (var i = 0; i < 1000; i++)
        {
            var receiptCase = _fixture.Create<ReceiptCase>();
            var version = (byte) (_fixture.Create<byte>() >> 4);

            var result = receiptCase.WithVersion(version);

            result.Version().Should().Be(version);
        }
    }

    [Fact]
    public void FuzzTest_WithCountry()
    {
        foreach (var (country, code) in new List<(string, ulong)> {
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
            var receiptCase = _fixture.Create<ReceiptCase>();

            var resultCode = receiptCase.WithCountry(code);
            resultCode.CountryCode().Should().Be(code);

            var resultString = receiptCase.WithCountry(country);
            resultString.Country().Should().Be(country);
        }
    }
}
