using AutoFixture;
using fiskaltrust.Middleware.Localization.QueueES.Models.Cases;
using fiskaltrust.ifPOS.v2.Cases;
using FluentAssertions;
using Xunit;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueGR.Models;

namespace fiskaltrust.Middleware.Localization.v2.Models.Cases.Tests;

public class ChargeItemCaseTests
{
    private readonly IFixture _fixture;

    public ChargeItemCaseTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void FuzzTest_WithVat()
    {
        for (var i = 0; i < 1000; i++)
        {
            var chargeItemCase = _fixture.Create<ChargeItemCase>();
            var vatCase = _fixture.Create<ChargeItemCase>();

            var result = chargeItemCase.WithVat(vatCase);

            result.Vat().Should().Be(vatCase);
            result.IsVat(vatCase).Should().BeTrue();
        }
    }

    [Fact]
    public void FuzzTest_Reset()
    {
        for (var i = 0; i < 1000; i++)
        {
            var chargeItemCase = _fixture.Create<ChargeItemCase>();

            var result = chargeItemCase.Reset();

            result.Should().Be((ChargeItemCase) (0xFFFF_F000_0000_0000 & (ulong) chargeItemCase));
        }
    }

    [Fact]
    public void FuzzTest_WithVersion()
    {
        for (var i = 0; i < 1000; i++)
        {
            var chargeItemCase = _fixture.Create<ChargeItemCase>();
            var version = (byte) (_fixture.Create<byte>() >> 4);

            var result = chargeItemCase.WithVersion(version);

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
            var chargeItemCase = _fixture.Create<ChargeItemCase>();

            var resultCode = chargeItemCase.WithCountry(code);
            resultCode.CountryCode().Should().Be(code);

            var resultString = chargeItemCase.WithCountry(country);
            resultString.Country().Should().Be(country);
        }
    }

    [Fact]
    public void FuzzTest_TypeOfService()
    {
        for (var i = 0; i < 1000; i++)
        {
            var chargeItemCase = _fixture.Create<ChargeItemCase>();
            var typeOfService = _fixture.Create<ChargeItemCaseTypeOfService>();

            var result = chargeItemCase.WithTypeOfService(typeOfService);

            result.TypeOfService().Should().Be(typeOfService);
            result.IsTypeOfService(typeOfService).Should().BeTrue();
        }
    }

    [Fact]
    public void FuzzTest_NatureOfVatES()
    {
        for (var i = 0; i < 1000; i++)
        {
            var chargeItemCase = _fixture.Create<ChargeItemCase>();
            var natureOfVat = _fixture.Create<ChargeItemCaseNatureOfVatES>();

            var result = chargeItemCase.WithNatureOfVat(natureOfVat);

            ChargeItemCaseNatureOfVatESExt.NatureOfVat(result).Should().Be(natureOfVat);
            result.IsNatureOfVat(natureOfVat).Should().BeTrue();
        }
    }

    [Fact]
    public void FuzzTest_NatureOfVatGR()
    {
        for (var i = 0; i < 1000; i++)
        {
            var chargeItemCase = _fixture.Create<ChargeItemCase>();
            var natureOfVat = _fixture.Create<ChargeItemCaseNatureOfVatGR>();

            var result = chargeItemCase.WithNatureOfVat(natureOfVat);

            ChargeItemCaseNatureOfVatGRExt.NatureOfVat(result).Should().Be(natureOfVat);
            result.IsNatureOfVat(natureOfVat).Should().BeTrue();
        }
    }

    [Fact]
    public void FuzzTest_NatureOfVatPT()
    {
        for (var i = 0; i < 1000; i++)
        {
            var chargeItemCase = _fixture.Create<ChargeItemCase>();
            var natureOfVat = _fixture.Create<ChargeItemCaseNatureOfVatPT>();

            var result = chargeItemCase.WithNatureOfVat(natureOfVat);

            ChargeItemCaseNatureOfVatPTExt.NatureOfVat(result).Should().Be(natureOfVat);
            result.IsNatureOfVat(natureOfVat).Should().BeTrue();
        }
    }
}
