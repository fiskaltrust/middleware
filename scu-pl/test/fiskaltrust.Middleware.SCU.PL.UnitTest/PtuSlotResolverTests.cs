using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest;

public class PtuSlotResolverTests
{
    private static List<PLVatRateTableEntry> DefaultTable() => new()
    {
        new() { PtuSlot = "A", VatRatePercent = 23m },
        new() { PtuSlot = "B", VatRatePercent = 8m },
        new() { PtuSlot = "C", VatRatePercent = 5m },
        new() { PtuSlot = "D", VatRatePercent = 0m },
        new() { PtuSlot = "G", IsExempt = true },
    };

    [Theory]
    [InlineData(ChargeItemCase.NormalVatRate, "A")]
    [InlineData(ChargeItemCase.DiscountedVatRate1, "B")]
    [InlineData(ChargeItemCase.DiscountedVatRate2, "C")]
    [InlineData(ChargeItemCase.ZeroVatRate, "D")]
    [InlineData(ChargeItemCase.NotTaxable, "G")]
    public void Resolve_ShouldMapVatCase_ToDefaultSlots(ChargeItemCase vatCase, string expectedSlot)
    {
        var resolver = new PtuSlotResolver(DefaultTable());
        var chargeItemCase = ((ChargeItemCase)0x504C_2000_0000_0000).WithVat(vatCase);

        resolver.Resolve(chargeItemCase).PtuSlot.Should().Be(expectedSlot);
    }

    [Fact]
    public void Resolve_ShouldFollowTheDeviceTable_NotHardcodedLetters()
    {
        // A register may have 23% programmed on a different slot — the device owns the assignment.
        var resolver = new PtuSlotResolver(new List<PLVatRateTableEntry>
        {
            new() { PtuSlot = "B", VatRatePercent = 23m },
            new() { PtuSlot = "E", IsExempt = true },
        });

        resolver.Resolve(((ChargeItemCase)0x504C_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate)).PtuSlot.Should().Be("B");
        resolver.Resolve(((ChargeItemCase)0x504C_2000_0000_0000).WithVat(ChargeItemCase.NotTaxable)).PtuSlot.Should().Be("E");
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenRateNotProgrammedOnDevice()
    {
        var resolver = new PtuSlotResolver(new List<PLVatRateTableEntry> { new() { PtuSlot = "A", VatRatePercent = 23m } });

        var act = () => resolver.Resolve(((ChargeItemCase)0x504C_2000_0000_0000).WithVat(ChargeItemCase.DiscountedVatRate1));

        act.Should().Throw<PLValidationException>().WithMessage("*8%*");
    }

    [Fact]
    public void Resolve_ShouldThrow_ForVatCasesWithoutPolishMapping()
    {
        var resolver = new PtuSlotResolver(DefaultTable());

        var act = () => resolver.Resolve(((ChargeItemCase)0x504C_2000_0000_0000).WithVat(ChargeItemCase.ParkingVatRate));

        act.Should().Throw<PLValidationException>();
    }

    [Fact]
    public void ResolveExempt_ShouldThrow_WhenNoExemptSlotExists()
    {
        var resolver = new PtuSlotResolver(new List<PLVatRateTableEntry> { new() { PtuSlot = "A", VatRatePercent = 23m } });

        var act = () => resolver.ResolveExempt();

        act.Should().Throw<PLValidationException>().WithMessage("*zw.*");
    }
}
