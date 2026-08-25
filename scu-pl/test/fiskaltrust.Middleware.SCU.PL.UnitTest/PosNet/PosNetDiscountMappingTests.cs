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
/// How a discount or extra position reaches a POSNET register: the register has no position for one,
/// so it becomes the rabat/narzut of the sale line the position follows, or a rabat od podsumy when
/// it follows none.
/// </summary>
public class PosNetDiscountMappingTests
{
    private const long NormalRate = 0x0003;      // 23%, PTU slot A of the default table
    private const long ReducedRate = 0x0001;     // 8%, PTU slot B
    private const long NoRate = 0x0000;          // UnknownService — the POS sent no VAT case at all
    private const long ExtraOrDiscount = 0x0004_0000;

    [Fact]
    public void ADiscountFollowingAPosition_BecomesTheRabatOfThatLine()
    {
        var commands = MapSale(
            [Position("Candies", 100.00m), Modifier("Pos-Rabatt auf Candies", -23.00m)],
            paidInCash: 77.00m);

        Render(commands).Should().Equal(
            "trinit bm0",
            // The line keeps its own value (wa) — that is what the register prints and what it
            // measures the rabat against; the receipt is settled with the difference.
            "trline naCandies vt0 pr10000 wa10000 rd1 rnPos-Rabatt auf Candies rw2300",
            "trpayment ty0 wa7700 naGotówka re0",
            "trend to7700 fp7700");
    }

    [Fact]
    public void AnExtraFollowingAPosition_BecomesTheNarzutOfThatLine()
    {
        var commands = MapSale(
            [Position("Candies", 100.00m), Modifier("Serviceaufschlag", 5.00m)],
            paidInCash: 105.00m);

        Render(commands).Should().Equal(
            "trinit bm0",
            "trline naCandies vt0 pr10000 wa10000 rd0 rnServiceaufschlag rw500",
            "trpayment ty0 wa10500 naGotówka re0",
            "trend to10500 fp10500");
    }

    /// <summary>POT-I-DEV-05 p.226: Candies 10.00 and Cookies 25.00, 5.25 off the subtotal.</summary>
    [Fact]
    public void ADiscountFollowingNoPosition_BecomesARabatOdPodsumy()
    {
        var commands = MapSale(
            [Modifier("Stały klient", -5.25m), Position("Candies", 10.00m), Position("Cookies", 25.00m)],
            paidInCash: 29.75m);

        // The subtotal discount is held back until every line has been sent: it applies to what the
        // receipt sold, and the register distributes it over the PTU rates itself.
        Render(commands).Should().Equal(
            "trinit bm0",
            "trline naCandies vt0 pr1000",
            "trline naCookies vt0 pr2500",
            "trdiscntsubtot naStały klient rd1 rw525",
            "trpayment ty0 wa2975 naGotówka re0",
            "trend to2975 fp2975");
    }

    [Fact]
    public void ADiscountOnAPositionWithAQuantity_KeepsTheLineInvariant()
    {
        var commands = MapSale(
            [Position("Woda", 30.00m, quantity: 3m), Modifier("Rabat", -3.00m)],
            paidInCash: 27.00m);

        Render(commands)[1].Should().Be("trline naWoda vt0 pr1000 il3.000 wa3000 rd1 rnRabat rw300");
    }

    [Fact]
    public void ADiscountBookedOnAnotherVatRateThanItsPosition_IsRejected()
    {
        var act = () => MapSale(
            [Position("Candies", 100.00m), Modifier("Rabat", -23.00m, vatCase: ReducedRate)],
            paidInCash: 77.00m);

        // The rabat is granted at the line's rate, so the tax on paper would not be the tax the POS
        // booked — that is refused rather than printed.
        act.Should().Throw<PLValidationException>().WithMessage("*DiscountedVatRate1*NormalVatRate*");
    }

    /// <summary>
    /// A discount that carries no VAT case is the POS leaving the rate to the position — which is
    /// what the register does with a line rabat anyway, so it is accepted rather than run through
    /// the PTU rate table (where an unmapped case would fail as an unresolvable rate).
    /// </summary>
    [Fact]
    public void ADiscountWithoutAVatCase_TakesTheRateOfItsPosition()
    {
        var commands = MapSale(
            [Position("Candies", 100.00m), Modifier("Rabat", -23.00m, vatCase: NoRate)],
            paidInCash: 77.00m);

        Render(commands)[1].Should().Be("trline naCandies vt0 pr10000 wa10000 rd1 rnRabat rw2300");
    }

    [Fact]
    public void ASecondDiscountOnTheSamePosition_IsRejected()
    {
        var act = () => MapSale(
            [Position("Candies", 100.00m), Modifier("Rabat", -10.00m), Modifier("Treuerabatt", -5.00m)],
            paidInCash: 85.00m);

        act.Should().Throw<PLValidationException>().WithMessage("*more than one discount/extra*");
    }

    [Fact]
    public void ADiscountThatExceedsItsPosition_IsRejected()
    {
        var act = () => MapSale(
            [Position("Candies", 10.00m), Modifier("Rabat", -12.00m)],
            paidInCash: 0m);

        act.Should().Throw<PLValidationException>().WithMessage("*exceeds the position's value*");
    }

    [Fact]
    public void ASubtotalDiscountThatSwallowsTheReceipt_IsRejected()
    {
        var act = () => MapSale(
            [Modifier("Rabat", -10.00m), Position("Candies", 10.00m)],
            paidInCash: 0m);

        act.Should().Throw<PLValidationException>().WithMessage("*not less than the subtotal*");
    }

    [Fact]
    public void ADiscountWithoutAnAmount_IsRejected()
    {
        var act = () => MapSale(
            [Position("Candies", 10.00m), Modifier("Rabat", 0m)],
            paidInCash: 10.00m);

        act.Should().Throw<PLValidationException>().WithMessage("*carries no amount*");
    }

    /// <summary>The rn field holds 25 characters, so a longer description is cut, not refused.</summary>
    [Fact]
    public void ALongDiscountName_IsCutToTheFieldLength()
    {
        var commands = MapSale(
            [Position("Candies", 100.00m), Modifier("Rabat für treue Stammkunden im Sommer", -23.00m)],
            paidInCash: 77.00m);

        Render(commands)[1].Should().Be("trline naCandies vt0 pr10000 wa10000 rd1 rnRabat für treue Stammkund rw2300");
    }

    private static IReadOnlyList<PosNetCommand> MapSale(List<ChargeItem> chargeItems, decimal paidInCash)
        => PosNetReceiptMapper.MapSale(
            new ReceiptRequest
            {
                ftReceiptCase = (ReceiptCase)0x504C_2000_0000_0001,
                Currency = Currency.PLN,
                cbChargeItems = chargeItems,
                cbPayItems =
                [
                    new PayItem { Description = "Gotówka", Amount = paidInCash, ftPayItemCase = (PayItemCase)0x504C_2000_0000_0001, Currency = Currency.PLN },
                ],
            },
            new PtuSlotResolver(PosNetConfiguration.DefaultVatRateTable()));

    private static ChargeItem Position(string description, decimal amount, decimal quantity = 1m, long vatCase = NormalRate) => new()
    {
        Description = description,
        Amount = amount,
        Quantity = quantity,
        ftChargeItemCase = (ChargeItemCase)(0x504C_2000_0000_0010 | vatCase),
        Currency = Currency.PLN,
    };

    private static ChargeItem Modifier(string description, decimal amount, long vatCase = NormalRate) => new()
    {
        Description = description,
        Amount = amount,
        Quantity = 1m,
        ftChargeItemCase = (ChargeItemCase)(0x504C_2000_0000_0010 | ExtraOrDiscount | vatCase),
        Currency = Currency.PLN,
    };

    private static List<string> Render(IReadOnlyList<PosNetCommand> commands)
        => commands.Select(c => string.Join(' ', new[] { c.Mnemonic }.Concat(c.Parameters.Select(p => $"{p.Key}{p.Value}")))).ToList();
}
