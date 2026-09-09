using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using FluentAssertions;
using Xunit;
using static fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet.MappingFixture;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

/// <summary>
/// How a discount or extra position reaches a POSNET register: the register has no position for one,
/// so it becomes the rabat/narzut of the sale line the position follows, or a rabat od podsumy when
/// it follows none.
/// </summary>
public class PosNetDiscountMappingTests
{
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

    /// <summary>
    /// The case order alone cannot express: 1 Kawa, 2 Piwo, 1.1 Rabat — the last position belongs to
    /// the first. Position is read wherever the POS sets it, so the rabat rides the coffee's line
    /// even though another line stands between them.
    /// </summary>
    [Fact]
    public void AModifiersPositionNamesItsLine_EvenWhenAnotherLineStandsBetweenThem()
    {
        var commands = MapSale(
            [
                Position("Kawa", 10.00m, position: 1m),
                Position("Piwo", 8.00m, position: 2m),
                Modifier("Rabat na kawę", -2.00m, position: 1.1m),
            ],
            paidInCash: 16.00m);

        Render(commands).Should().Equal(
            "trinit bm0",
            "trline naKawa vt0 pr1000 wa1000 rd1 rnRabat na kawę rw200",
            "trline naPiwo vt0 pr800",
            "trpayment ty0 wa1600 naGotówka re0",
            "trend to1600 fp1600");
    }

    /// <summary>A positioned modifier may also name a line that has not been read yet.</summary>
    [Fact]
    public void AModifiersPositionNamesItsLine_EvenWhenTheLineComesAfterIt()
    {
        var commands = MapSale(
            [
                Modifier("Rabat na piwo", -1.00m, position: 2.1m),
                Position("Kawa", 10.00m, position: 1m),
                Position("Piwo", 8.00m, position: 2m),
            ],
            paidInCash: 17.00m);

        Render(commands)[2].Should().Be("trline naPiwo vt0 pr800 wa800 rd1 rnRabat na piwo rw100");
    }

    /// <summary>
    /// A fractional position the receipt does not carry is a mistake, not a receipt-level discount —
    /// the POS said which line it meant.
    /// </summary>
    [Fact]
    public void AModifierNamingAPositionTheReceiptDoesNotCarry_IsRejected()
    {
        var act = () => MapSale(
            [Position("Kawa", 10.00m, position: 1m), Modifier("Rabat", -2.00m, position: 3.1m)],
            paidInCash: 8.00m);

        act.Should().Throw<PLValidationException>()
            .WithMessage("The discount/extra 'Rabat'*sale position 3, which this receipt does not carry*apply to the subtotal*");
    }

    /// <summary>
    /// A POS that numbers every charge item in sequence gives the rabat a whole-number position of its
    /// own — 1 Kawa, 2 Piwo, 3 Rabat. That is no reference to a line, so the receipt reads by order:
    /// the rabat belongs to the beer in front of it, and one with nothing in front of it to the subtotal.
    /// </summary>
    [Fact]
    public void AModifierOnAWholeNumberPositionOfItsOwn_IsReadByOrder()
    {
        var onTheLine = MapSale(
            [
                Position("Kawa", 10.00m, position: 1m),
                Position("Piwo", 8.00m, position: 2m),
                Modifier("Rabat", -2.00m, position: 3m),
            ],
            paidInCash: 16.00m);
        var onTheSubtotal = MapSale(
            [
                Modifier("Rabat", -2.00m, position: 1m),
                Position("Kawa", 10.00m, position: 2m),
                Position("Piwo", 8.00m, position: 3m),
            ],
            paidInCash: 16.00m);

        Render(onTheLine)[2].Should().Be("trline naPiwo vt0 pr800 wa800 rd1 rnRabat rw200");
        Render(onTheSubtotal)[3].Should().Be("trdiscntsubtot naRabat rd1 rw200");
    }

    /// <summary>Positioned and unpositioned modifiers on one receipt: each is read its own way.</summary>
    [Fact]
    public void AnUnpositionedModifier_StillBelongsToTheLineInFrontOfIt()
    {
        var commands = MapSale(
            [
                Position("Kawa", 10.00m, position: 1m),
                Position("Piwo", 8.00m, position: 2m),
                Modifier("Rabat na piwo", -1.00m),
                Modifier("Rabat na kawę", -2.00m, position: 1.1m),
            ],
            paidInCash: 15.00m);

        Render(commands).Should().Equal(
            "trinit bm0",
            "trline naKawa vt0 pr1000 wa1000 rd1 rnRabat na kawę rw200",
            "trline naPiwo vt0 pr800 wa800 rd1 rnRabat na piwo rw100",
            "trpayment ty0 wa1500 naGotówka re0",
            "trend to1500 fp1500");
    }
}
