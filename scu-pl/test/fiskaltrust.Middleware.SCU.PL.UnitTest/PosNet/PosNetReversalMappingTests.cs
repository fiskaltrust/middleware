using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

/// <summary>
/// How a voided position reaches a POSNET register: as a trline of its own with the reversal flag
/// (st1), repeating the goods the register printed and taking the stated quantity and value off
/// them. Which position it reverses is read the way a discount's is — Position where the POS sets
/// one, otherwise the position in front of it.
/// </summary>
public class PosNetReversalMappingTests
{
    private const long NormalRate = 0x0003;       // 23%, PTU slot A of the default table
    private const long VoidFlag = 0x0001_0000;
    private const long RefundFlag = 0x0002_0000;
    private const long ExtraOrDiscountFlag = 0x0004_0000;

    /// <summary>1 Kawa, 2 Piwo, 1.1 Storno: the storno reverses the coffee, not the beer.</summary>
    [Fact]
    public void AStornosPositionNamesTheLineItReverses()
    {
        var commands = MapSale(
            [
                Position("Kawa", 10.00m, position: 1m),
                Position("Piwo", 8.00m, position: 2m),
                Voided("Storno", -10.00m, position: 1.1m),
            ],
            paidInCash: 8.00m);

        // The reversal repeats the goods name the register printed — the device matches a storno
        // against what it sold — so the storno position's own description does not travel.
        Render(commands).Should().Equal(
            "trinit bm0",
            "trline naKawa vt0 pr1000",
            "trline naPiwo vt0 pr800",
            "trline naKawa vt0 pr1000 st1 wa1000",
            "trpayment ty0 wa800 naGotówka re0",
            "trend to800 fp800");
    }

    [Fact]
    public void AStornoWithoutAPosition_ReversesTheLineInFrontOfIt()
    {
        var commands = MapSale(
            [Position("Kawa", 10.00m), Position("Piwo", 8.00m), Voided("Storno", -8.00m)],
            paidInCash: 10.00m);

        Render(commands)[3].Should().Be("trline naPiwo vt0 pr800 st1 wa800");
    }

    /// <summary>A quantity of its own makes it a partial storno: one of three waters goes back.</summary>
    [Fact]
    public void AStornoOfPartOfAPosition_ReversesThatMuch()
    {
        var commands = MapSale(
            [Position("Woda", 30.00m, quantity: 3m), Voided("Storno", -10.00m, quantity: 1m)],
            paidInCash: 20.00m);

        Render(commands).Should().Equal(
            "trinit bm0",
            "trline naWoda vt0 pr1000 il3.000 wa3000",
            "trline naWoda vt0 pr1000 st1 wa1000",
            "trpayment ty0 wa2000 naGotówka re0",
            "trend to2000 fp2000");
    }

    /// <summary>
    /// The direction is in the st flag, not in the sign — the same reading the other SCUs give a
    /// voided position — so a POS that sends the storno with a positive amount is understood.
    /// </summary>
    [Fact]
    public void AStornoSentWithAPositiveAmount_ReversesJustTheSame()
    {
        var commands = MapSale(
            [Position("Kawa", 10.00m), Position("Piwo", 8.00m), Voided("Storno", 8.00m)],
            paidInCash: 10.00m);

        Render(commands)[3].Should().Be("trline naPiwo vt0 pr800 st1 wa800");
    }

    [Fact]
    public void AStornoOfMoreThanThePositionCarries_IsRejected()
    {
        var act = () => MapSale(
            [Position("Kawa", 10.00m), Voided("Storno", -12.00m)],
            paidInCash: 0m);

        act.Should().Throw<PLValidationException>().WithMessage("*more than the 10.00 still standing on it*");
    }

    [Fact]
    public void ASecondStornoBeyondWhatIsLeftOfThePosition_IsRejected()
    {
        var act = () => MapSale(
            [
                Position("Woda", 30.00m, quantity: 3m),
                Voided("Storno", -20.00m, quantity: 2m),
                Voided("Storno", -20.00m, quantity: 2m),
            ],
            paidInCash: 0m);

        act.Should().Throw<PLValidationException>().WithMessage("*more than the 10.00 still standing on it*");
    }

    [Fact]
    public void AStornoThatDoesNotFitThePositionsUnitPrice_IsRejected()
    {
        var act = () => MapSale(
            [Position("Woda", 30.00m, quantity: 3m), Voided("Storno", -7.00m, quantity: 1m)],
            paidInCash: 23.00m);

        act.Should().Throw<PLValidationException>().WithMessage("*does not match the unit price*");
    }

    [Fact]
    public void AStornoOfAPositionThatCarriesARabat_IsRejected()
    {
        var act = () => MapSale(
            [Position("Kawa", 10.00m), Modifier("Rabat", -2.00m), Voided("Storno", -8.00m)],
            paidInCash: 0m);

        act.Should().Throw<PLValidationException>().WithMessage("*carries a rabat/narzut and cannot be reversed*");
    }

    [Fact]
    public void AStornoOfAPositionSoldAfterIt_IsRejected()
    {
        var act = () => MapSale(
            [
                Position("Kawa", 10.00m, position: 1m),
                Voided("Storno", -8.00m, position: 2.1m),
                Position("Piwo", 8.00m, position: 2m),
            ],
            paidInCash: 10.00m);

        act.Should().Throw<PLValidationException>().WithMessage("*can only be reversed once it has been sold*");
    }

    [Fact]
    public void AStornoWithNothingInFrontOfIt_IsRejected()
    {
        var act = () => MapSale(
            [Voided("Storno", -10.00m), Position("Kawa", 10.00m)],
            paidInCash: 10.00m);

        act.Should().Throw<PLValidationException>().WithMessage("*no sale position to reverse*");
    }

    [Fact]
    public void AVoidedDiscountPosition_IsRejected()
    {
        var act = () => MapSale(
            [Position("Kawa", 10.00m), VoidedModifier("Storno rabatu", 2.00m)],
            paidInCash: 10.00m);

        act.Should().Throw<PLValidationException>().WithMessage("*cannot be reversed on a POSNET register*");
    }

    /// <summary>A return is a document of its own on a Polish register, not a line of this receipt.</summary>
    [Fact]
    public void ARefundedPosition_IsStillRejected()
    {
        var act = () => MapSale(
            [Position("Kawa", 10.00m), Refunded("Zwrot", -10.00m)],
            paidInCash: 0m);

        act.Should().Throw<PLValidationException>().WithMessage("*Refunded positions are not supported*");
    }

    [Fact]
    public void AReceiptStornoedDownToNothing_IsRejected()
    {
        var act = () => MapSale(
            [Position("Kawa", 10.00m), Voided("Storno", -10.00m)],
            paidInCash: 10.00m);

        act.Should().Throw<PLValidationException>().WithMessage("*needs a positive total*");
    }

    private static IReadOnlyList<PosNetCommand> MapSale(List<ChargeItem> chargeItems, decimal paidInCash)
        => PosNetReceiptMapper.MapSale(
            new ReceiptRequest
            {
                ftReceiptCase = (ReceiptCase)0x504C_2000_0000_0001,
                Currency = Currency.PLN,
                cbChargeItems = chargeItems,
                cbPayItems = paidInCash == 0m
                    ? []
                    : [new PayItem { Description = "Gotówka", Amount = paidInCash, ftPayItemCase = (PayItemCase)0x504C_2000_0000_0001, Currency = Currency.PLN }],
            },
            new PtuSlotResolver(PosNetConfiguration.DefaultVatRateTable()));

    private static ChargeItem Position(string description, decimal amount, decimal quantity = 1m, decimal position = 0m)
        => Item(description, amount, quantity, position, flags: 0);

    private static ChargeItem Voided(string description, decimal amount, decimal quantity = 0m, decimal position = 0m)
        => Item(description, amount, quantity, position, flags: VoidFlag);

    private static ChargeItem VoidedModifier(string description, decimal amount)
        => Item(description, amount, quantity: 1m, position: 0m, flags: VoidFlag | ExtraOrDiscountFlag);

    private static ChargeItem Modifier(string description, decimal amount)
        => Item(description, amount, quantity: 1m, position: 0m, flags: ExtraOrDiscountFlag);

    private static ChargeItem Refunded(string description, decimal amount)
        => Item(description, amount, quantity: 1m, position: 0m, flags: RefundFlag);

    private static ChargeItem Item(string description, decimal amount, decimal quantity, decimal position, long flags) => new()
    {
        Description = description,
        Amount = amount,
        Quantity = quantity,
        Position = position,
        ftChargeItemCase = (ChargeItemCase)(0x504C_2000_0000_0010 | flags | NormalRate),
        Currency = Currency.PLN,
    };

    private static List<string> Render(IReadOnlyList<PosNetCommand> commands)
        => commands.Select(c => string.Join(' ', new[] { c.Mnemonic }.Concat(c.Parameters.Select(p => $"{p.Key}{p.Value}")))).ToList();
}
