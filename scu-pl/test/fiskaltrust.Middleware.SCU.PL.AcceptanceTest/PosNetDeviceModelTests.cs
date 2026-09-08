using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.TestSupport.Emulator;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.AcceptanceTest;

/// <summary>
/// The device model is what the suite runs against wherever no cassette exists, so its arithmetic
/// is asserted on its own: it has to accept every receipt the recorded printer accepted, refuse what
/// the printer refused, and report the same numbers through the status commands.
/// </summary>
public class PosNetDeviceModelTests
{
    private const int SlotB = 1;

    /// <summary>Encodes the command like the SCU and decodes it like the emulator, so the model sees real wire fields.</summary>
    private static string Send(PosNetDeviceModel model, PosNetCommand command)
        => model.Answer(PosNetFrame.Decode(PosNetFrame.Encode(command)));

    private static void Sell(PosNetDeviceModel model, params PosNetCommand[] commands)
    {
        foreach (var command in commands)
        {
            Send(model, command).Should().Be($"{command.Mnemonic}\t", $"the register should accept {command.Mnemonic}");
        }
    }

    [Fact]
    public void CashSale_TotalizesTheLine_AndAdvancesTheCounters()
    {
        var model = new PosNetDeviceModel();
        var receiptsBefore = model.CompletedReceipts;

        Sell(model,
            PosNetCommands.Trinit(),
            PosNetCommands.Trline("Candies", SlotB, 999, 1m, 999),
            PosNetCommands.Trpayment(0, 999, isChange: false, "Gotówka"),
            PosNetCommands.Trend(999, 999, 0));

        model.TransactionOpen.Should().BeFalse();
        model.ReceiptTotalizersGrosze[SlotB].Should().Be(999);
        model.CompletedReceipts.Should().Be(receiptsBefore + 1);
        model.LastReceiptNumber.Should().Be(receiptsBefore + 1);
        Send(model, new PosNetCommand("strns")).Should().Be("strns\tto0\tts16\tva0\tvb999\tvc0\tvd0\tve0\tvf0\tvg0\tpp0\tpm0\tre0\tfp999\tfe0\t");
        Send(model, new PosNetCommand("scnt")).Should().Contain($"\tbt{receiptsBefore + 1}\t").And.Contain($"\tbn{receiptsBefore + 1}\t");
    }

    [Fact]
    public void CardSaleWithChange_SettlesLikeTheSpecExample()
    {
        var model = new PosNetDeviceModel();

        Sell(model,
            PosNetCommands.Trinit(),
            PosNetCommands.Trline("Apples", SlotB, 200, 1m, 200),
            PosNetCommands.Trpayment(2, 500, isChange: false, "Karta"),
            PosNetCommands.Trpayment(0, 300, isChange: true, "Reszta"),
            PosNetCommands.Trend(200, 500, 300));

        Send(model, new PosNetCommand("strns")).Should().Contain("\tre300\tfp500\t");
    }

    [Fact]
    public void LineRabat_AndSubtotalRabat_ReduceTheFiscalValue()
    {
        var model = new PosNetDeviceModel();

        Sell(model,
            PosNetCommands.Trinit(),
            PosNetCommands.Trline("Candies", SlotB, 1000, 1m, 1000, new PosNetModifier(isDiscount: true, 200, "Rabat")),
            PosNetCommands.Trdiscntsubtot(new PosNetModifier(isDiscount: true, 100, "Stały klient")),
            PosNetCommands.Trpayment(0, 700, isChange: false),
            PosNetCommands.Trend(700, 700, 0));

        model.ReceiptTotalizersGrosze[SlotB].Should().Be(700);
    }

    [Fact]
    public void LineNarzut_AndSubtotalNarzut_RaiseTheFiscalValue()
    {
        var model = new PosNetDeviceModel();

        Sell(model,
            PosNetCommands.Trinit(),
            PosNetCommands.Trline("Candies", SlotB, 1000, 1m, 1000, new PosNetModifier(isDiscount: false, 200)),
            PosNetCommands.Trdiscntsubtot(new PosNetModifier(isDiscount: false, 100)),
            PosNetCommands.Trpayment(0, 1300, isChange: false),
            PosNetCommands.Trend(1300, 1300, 0));

        model.ReceiptTotalizersGrosze[SlotB].Should().Be(1300);
    }

    [Fact]
    public void Storno_TakesThePositionOff_AndTheRestIsSettled()
    {
        var model = new PosNetDeviceModel();

        Sell(model,
            PosNetCommands.Trinit(),
            PosNetCommands.Trline("Kawa", SlotB, 1000, 1m, 1000),
            PosNetCommands.Trline("Piwo", SlotB, 800, 1m, 800),
            PosNetCommands.Trline("Kawa", SlotB, 1000, 1m, 1000, isReversal: true),
            PosNetCommands.Trpayment(0, 800, isChange: false),
            PosNetCommands.Trend(800, 800, 0));

        model.ReceiptTotalizersGrosze[SlotB].Should().Be(800);
    }

    [Fact]
    public void StornoOfDiscountedPosition_WithTheRabatRepeated_TakesTheDiscountedValueOff()
    {
        var model = new PosNetDeviceModel();
        var rabat = new PosNetModifier(isDiscount: true, 200, "Rabat");

        Sell(model,
            PosNetCommands.Trinit(),
            PosNetCommands.Trline("Kawa", SlotB, 1000, 1m, 1000, rabat),
            PosNetCommands.Trline("Piwo", SlotB, 800, 1m, 800),
            PosNetCommands.Trline("Kawa", SlotB, 1000, 1m, 1000, rabat, isReversal: true),
            PosNetCommands.Trpayment(0, 800, isChange: false),
            PosNetCommands.Trend(800, 800, 0));

        model.ReceiptTotalizersGrosze[SlotB].Should().Be(800);
    }

    [Fact]
    public void StornoOfDiscountedPosition_WithoutTheRabat_LeavesLess_AndTheTrendIsRefused()
    {
        // Measured on the printer: the storno without rw200 took 10.00 off instead of 8.00, so the
        // 8.00 the positions still standing add up to was refused with 2805 (PosNetReceiptMapper).
        var model = new PosNetDeviceModel();

        Sell(model,
            PosNetCommands.Trinit(),
            PosNetCommands.Trline("Kawa", SlotB, 1000, 1m, 1000, new PosNetModifier(isDiscount: true, 200, "Rabat")),
            PosNetCommands.Trline("Piwo", SlotB, 800, 1m, 800),
            PosNetCommands.Trline("Kawa", SlotB, 1000, 1m, 1000, isReversal: true),
            PosNetCommands.Trpayment(0, 800, isChange: false));

        Send(model, PosNetCommands.Trend(800, 800, 0)).Should().Be($"trend\t?{PosNetErrors.EndTotalVerify}\t");
        Send(model, new PosNetCommand("strns")).Should().Contain("\tto1\t").And.Contain("\tvb600\t");
    }

    [Fact]
    public void Trend_WithAFiscalValueTheReceiptDoesNotHave_IsRefusedWith2805()
    {
        var model = new PosNetDeviceModel();
        Sell(model,
            PosNetCommands.Trinit(),
            PosNetCommands.Trline("Candies", SlotB, 999, 1m, 999),
            PosNetCommands.Trpayment(0, 1000, isChange: false));

        Send(model, PosNetCommands.Trend(1000, 1000, 0)).Should().Be($"trend\t?{PosNetErrors.EndTotalVerify}\t");
        model.TransactionOpen.Should().BeTrue("a refused trend leaves the transaction open, like on the device");
    }

    [Fact]
    public void SubtotalRabat_IsSplitOverTheRatesInProportion()
    {
        // 10.00 in A and 25.00 in B, 5.25 off the subtotal: 1.50 and 3.75.
        var model = new PosNetDeviceModel();

        Sell(model,
            PosNetCommands.Trinit(),
            PosNetCommands.Trline("Candies", 0, 1000, 1m, 1000),
            PosNetCommands.Trline("Cookies", SlotB, 2500, 1m, 2500),
            PosNetCommands.Trdiscntsubtot(new PosNetModifier(isDiscount: true, 525, "Stały klient")),
            PosNetCommands.Trpayment(0, 2975, isChange: false),
            PosNetCommands.Trend(2975, 2975, 0));

        model.ReceiptTotalizersGrosze[0].Should().Be(850);
        model.ReceiptTotalizersGrosze[SlotB].Should().Be(2125);
    }

    [Fact]
    public void Storno_OfMoreThanWasSold_IsRefused()
    {
        var model = new PosNetDeviceModel();
        Sell(model,
            PosNetCommands.Trinit(),
            PosNetCommands.Trline("Kawa", SlotB, 1000, 1m, 1000));

        Send(model, PosNetCommands.Trline("Kawa", SlotB, 1000, 2m, 2000, isReversal: true)).Should().Be($"trline\t?{PosNetErrors.StornoQuantity}\t");
        Send(model, PosNetCommands.Trline("Herbata", SlotB, 1000, 1m, 1000, isReversal: true)).Should().Be($"trline\t?{PosNetErrors.StornoAmount}\t");
    }

    [Fact]
    public void Commands_OutOfOrder_AreRefusedLikeTheDevice()
    {
        var model = new PosNetDeviceModel();

        Send(model, PosNetCommands.Trline("Candies", SlotB, 999, 1m, 999)).Should().Be($"trline\t?{PosNetErrors.NoTransactionMode}\t");
        Send(model, PosNetCommands.Prncancel()).Should().Be($"prncancel\t?{PosNetErrors.NothingToCancel}\t");
        Send(model, PosNetCommands.Trinit()).Should().Be("trinit\t");
        Send(model, PosNetCommands.Trinit()).Should().Be($"trinit\t?{PosNetErrors.TransactionMode}\t");
        Send(model, PosNetCommands.Trline("Candies", 4, 999, 1m, 999)).Should().Be($"trline\t?{PosNetErrors.VatField}\t", "slot E is inactive in the default rate table");
    }

    [Fact]
    public void Prncancel_CountsTheCanceledReceipt_AndNumbersIt()
    {
        var model = new PosNetDeviceModel();
        var lastNumber = model.LastReceiptNumber;
        Sell(model,
            PosNetCommands.Trinit(),
            PosNetCommands.Trline("Candies", SlotB, 999, 1m, 999),
            PosNetCommands.Prncancel());

        model.TransactionOpen.Should().BeFalse();
        model.CanceledReceipts.Should().Be(1);
        model.CanceledTotalGrosze.Should().Be(999);
        model.LastReceiptNumber.Should().Be(lastNumber + 1, "a canceled receipt uses up a number too (recorded: bn58 bc2 bt60)");
        model.ReceiptTotalizersGrosze[SlotB].Should().Be(0);
    }

    [Fact]
    public void NonFiscalRegister_ReportsItself_LikeTheTestPrinterDid()
    {
        var model = new PosNetDeviceModel().NonFiscal();

        Send(model, PosNetCommands.Scomm()).Should().Be("scomm\tfsN\ttzN\tts0\thrT\tnuZBF 2101002392\ttdN\t");
    }
}
