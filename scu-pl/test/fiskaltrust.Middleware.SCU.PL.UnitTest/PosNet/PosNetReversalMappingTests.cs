using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using FluentAssertions;
using Xunit;
using static fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet.MappingFixture;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

/// <summary>
/// How a voided position reaches a POSNET register: as a trline of its own with the reversal flag
/// (st1), repeating the goods the register printed and taking the stated quantity and value off
/// them. Which position it reverses is read the way a discount's is — Position where the POS sets
/// one, otherwise the position in front of it.
/// </summary>
public class PosNetReversalMappingTests
{
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

    /// <summary>
    /// A POS that numbers every charge item in sequence gives the storno a whole-number position of
    /// its own. That is no reference to a line, so the receipt reads by order like one without positions.
    /// </summary>
    [Fact]
    public void AStornoOnAWholeNumberPositionOfItsOwn_ReversesTheLineInFrontOfIt()
    {
        var commands = MapSale(
            [
                Position("Kawa", 10.00m, position: 1m),
                Position("Piwo", 8.00m, position: 2m),
                Voided("Storno", -8.00m, position: 3m),
            ],
            paidInCash: 10.00m);

        Render(commands)[3].Should().Be("trline naPiwo vt0 pr800 st1 wa800");
    }

    /// <summary>A fractional position names a line, and one the receipt does not carry is a mistake.</summary>
    [Fact]
    public void AStornoNamingAPositionTheReceiptDoesNotCarry_IsRejected()
    {
        var act = () => MapSale(
            [Position("Kawa", 10.00m, position: 1m), Voided("Storno", -10.00m, position: 3.1m)],
            paidInCash: 10.00m);

        act.Should().Throw<PLValidationException>()
            .WithMessage("The voided position 'Storno'*reverses sale position 3, which this receipt does not carry*reverse the position in front of it*");
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

    /// <summary>
    /// The quantity travels with three decimal places. One with more would satisfy price x quantity
    /// here and fail on the register, with the transaction already open — whether the POS stated it
    /// or it follows from an amount that is no multiple of the unit price to three places.
    /// </summary>
    [Fact]
    public void AStornoWhoseQuantityHasMoreDecimalsThanTheWireCarries_IsRejected()
    {
        var stated = () => MapSale(
            [Position("Woda", 40.00m, quantity: 2m), Voided("Storno", -24.69m, quantity: 1.2345m)],
            paidInCash: 15.31m);
        var derived = () => MapSale(
            [Position("Woda", 16.00m), Voided("Storno", -1.00m)],
            paidInCash: 15.00m);

        stated.Should().Throw<PLValidationException>().WithMessage("The storno of 'Storno'*more than 3 decimal places*");
        derived.Should().Throw<PLValidationException>().WithMessage("The storno of 'Storno'*quantity 0.0625*more than 3 decimal places*");
    }

    /// <summary>
    /// A storno travels at the PTU slot of the position it reverses. Booked at another rate it would
    /// silently move turnover between rates — refused, the way a rabat at another rate is.
    /// </summary>
    [Fact]
    public void AStornoBookedOnAnotherVatRateThanItsPosition_IsRejected()
    {
        var act = () => MapSale(
            [Position("Kawa", 10.00m), Position("Piwo", 8.00m), Voided("Storno", -8.00m, vatCase: ReducedRate)],
            paidInCash: 10.00m);

        act.Should().Throw<PLValidationException>().WithMessage("The voided position 'Storno'*DiscountedVatRate1*reverses ('Piwo')*NormalVatRate*");
    }

    /// <summary>A storno without a VAT case leaves the rate to the position, as a rabat without one does.</summary>
    [Fact]
    public void AStornoWithoutAVatCase_TakesTheRateOfItsPosition()
    {
        var commands = MapSale(
            [Position("Kawa", 10.00m), Position("Piwo", 8.00m), Voided("Storno", -8.00m, vatCase: NoRate)],
            paidInCash: 10.00m);

        Render(commands)[3].Should().Be("trline naPiwo vt0 pr800 st1 wa800");
    }

    /// <summary>
    /// A position sold with a rabat is reversed by a storno carrying the same rabat — measured on the
    /// device: with the rabat repeated it takes the discounted 8.00 off, without it the 10.00 before
    /// the rabat, which would leave the receipt totalling less than the positions still on it.
    /// </summary>
    [Fact]
    public void AStornoOfAPositionSoldWithARabat_RepeatsThatRabat()
    {
        var commands = MapSale(
            [
                Position("Kawa", 10.00m, position: 1m),
                Modifier("Rabat", -2.00m, position: 1.1m),
                Position("Piwo", 8.00m, position: 2m),
                Voided("Storno", -8.00m, position: 1.2m),
            ],
            paidInCash: 8.00m);

        Render(commands).Should().Equal(
            "trinit bm0",
            "trline naKawa vt0 pr1000 wa1000 rd1 rnRabat rw200",
            "trline naPiwo vt0 pr800",
            "trline naKawa vt0 pr1000 st1 wa1000 rd1 rnRabat rw200",
            "trpayment ty0 wa800 naGotówka re0",
            "trend to800 fp800");
    }

    /// <summary>The whole position may be stated either before or after its rabat.</summary>
    [Fact]
    public void AStornoOfAPositionSoldWithARabat_MayStateTheValueBeforeTheRabat()
    {
        var commands = MapSale(
            [
                Position("Kawa", 10.00m, position: 1m),
                Modifier("Rabat", -2.00m, position: 1.1m),
                Position("Piwo", 8.00m, position: 2m),
                Voided("Storno", -10.00m, position: 1.2m),
            ],
            paidInCash: 8.00m);

        Render(commands)[3].Should().Be("trline naKawa vt0 pr1000 st1 wa1000 rd1 rnRabat rw200");
    }

    /// <summary>
    /// The rabat is one amount for the whole line, and how the register would split it over part of
    /// one is not documented — so a discounted position is reversed as a whole or not at all.
    /// </summary>
    [Fact]
    public void APartialStornoOfAPositionSoldWithARabat_IsRejected()
    {
        var act = () => MapSale(
            [
                Position("Woda", 30.00m, quantity: 3m, position: 1m),
                Modifier("Rabat", -3.00m, position: 1.1m),
                Voided("Storno", -9.00m, quantity: 1m, position: 1.2m),
            ],
            paidInCash: 18.00m);

        act.Should().Throw<PLValidationException>()
            .WithMessage("*can only be reversed as a whole — state 30.00 before it or 27.00 after it*");
    }

    /// <summary>
    /// A rabat sent right after a storno has nothing to belong to: the storno carries no rabat of its
    /// own, and the position in front of the storno has just been reversed.
    /// </summary>
    [Fact]
    public void ARabatFollowingAStorno_IsRejected()
    {
        var act = () => MapSale(
            [Position("Kawa", 10.00m), Position("Piwo", 8.00m), Voided("Storno", -8.00m), Modifier("Rabat", -2.00m)],
            paidInCash: 8.00m);

        act.Should().Throw<PLValidationException>().WithMessage("The discount/extra 'Rabat' follows a storno*");
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
}
