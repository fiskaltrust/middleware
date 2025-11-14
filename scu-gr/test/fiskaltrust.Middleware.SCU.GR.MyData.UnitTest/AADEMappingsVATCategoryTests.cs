using System;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest.SCU.MyData;

public class AADEMappingsVATCategoryTests
{
    [Theory]
    [InlineData(24, ChargeItemCase.NormalVatRate, MyDataVatCategory.VatRate24_Category1)]
    [InlineData(13, ChargeItemCase.DiscountedVatRate1, MyDataVatCategory.VatRate13_Category2)]
    [InlineData(13, ChargeItemCase.DiscountedVatRate2, MyDataVatCategory.VatRate13_Category2)]
    [InlineData(6, ChargeItemCase.DiscountedVatRate1, MyDataVatCategory.VatRate6_Category3)]
    [InlineData(6, ChargeItemCase.DiscountedVatRate2, MyDataVatCategory.VatRate6_Category3)]
    [InlineData(17, ChargeItemCase.DiscountedVatRate1, MyDataVatCategory.VatRate17_Category4)]
    [InlineData(17, ChargeItemCase.DiscountedVatRate2, MyDataVatCategory.VatRate17_Category4)]
    [InlineData(9, ChargeItemCase.DiscountedVatRate1, MyDataVatCategory.VatRate9_Category5)]
    [InlineData(9, ChargeItemCase.DiscountedVatRate2, MyDataVatCategory.VatRate9_Category5)]
    [InlineData(4, ChargeItemCase.SuperReducedVatRate1, MyDataVatCategory.VatRate4_Category6)]
    [InlineData(4, ChargeItemCase.SuperReducedVatRate2, MyDataVatCategory.VatRate4_Category6)]
    public void GetVATCategory_ReturnsCorrectVATCategory(decimal vatRate, ChargeItemCase state, int expected)
    {
        var result = AADEMappings.GetVATCategory(new ifPOS.v2.ChargeItem
        {
            VATRate = vatRate,
            ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(state)
        });
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(24, MyDataVatCategory.VatRate24_Category1)]
    [InlineData(13, MyDataVatCategory.VatRate13_Category2)]
    [InlineData(6, MyDataVatCategory.VatRate6_Category3)]
    [InlineData(17, MyDataVatCategory.VatRate17_Category4)]
    [InlineData(9, MyDataVatCategory.VatRate9_Category5)]
    [InlineData(4, MyDataVatCategory.VatRate4_Category6)]
    [InlineData(0, MyDataVatCategory.VatRate0_ExcludingVat_Category7)]
    [InlineData(3, MyDataVatCategory.VatRate3_Category9)]
    public void GetVATCategory_WithUnknown_ReturnsCorrectVATCategory(decimal vatRate, int expected)
    {
        var result = AADEMappings.GetVATCategory(new ifPOS.v2.ChargeItem
        {
            VATRate = vatRate,
            ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.UnknownService)
        });
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(6, ChargeItemCase.NormalVatRate)]
    [InlineData(3, ChargeItemCase.DiscountedVatRate1)]
    [InlineData(3, ChargeItemCase.DiscountedVatRate2)]
    [InlineData(3, ChargeItemCase.SuperReducedVatRate1)]
    [InlineData(3, ChargeItemCase.SuperReducedVatRate2)]
    [InlineData(3, ChargeItemCase.ZeroVatRate)]
    [InlineData(3, ChargeItemCase.NotTaxable)]
    public void GetVatCategory_ShouldThrowException_InCaseWrongSetup(decimal vatRate, ChargeItemCase state)
    {
        Action action = () => _ = AADEMappings.GetVATCategory(new ifPOS.v2.ChargeItem
        {
            VATRate = vatRate,
            ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(state)
        });
        action.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData(ChargeItemCase.ParkingVatRate)]
    [InlineData((ChargeItemCase) 9)]
    public void GetVatCategory_ParkingVatRateShouldThrowException(ChargeItemCase state)
    {
        Action action = () => _ = AADEMappings.GetVATCategory(new ifPOS.v2.ChargeItem
        {
            ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(state)
        });
        action.Should().Throw<Exception>();
    }
}