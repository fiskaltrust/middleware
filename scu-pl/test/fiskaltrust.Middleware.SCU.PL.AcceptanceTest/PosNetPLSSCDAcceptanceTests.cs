using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.SCU.PL.TestSupport.PosNetPrinter;
using fiskaltrust.Middleware.SCU.PL.TestSupport.Verification;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Models;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.AcceptanceTest;

/// <summary>
/// Acceptance tests in the shape of the Italian SCU acceptance suite — the SUT is built through
/// the ScuBootstrapper like the launcher would — but market-scoped: they run over the real
/// transport (TCP, or serial for the USB/COM interface) against whatever
/// <see cref="PosNetTestTarget"/> selects, so the whole stack (transport, framing, codec,
/// transaction flow) is exercised against a recorded printer in CI and against the device itself
/// by setting <c>SCU_PL_POSNET_DEVICE_URL</c>, without touching a test.
/// </summary>
public class PosNetPLSSCDAcceptanceTests
{
    /// <summary>
    /// The document number a receipt was printed under. Asserted relatively throughout: on a real
    /// printer the counter carries whatever history the device has.
    /// </summary>
    private static long DocumentNumberOf(ProcessResponse response)
    {
        var number = FiscalDocumentNumber.Of(response.ReceiptResponse);
        number.Should().NotBeNull("a printed receipt carries its fiscal document number");
        return number!.Value;
    }

    /// <summary>
    /// Checks that no transaction was left open on the device — only the emulator models that
    /// state, a real printer cannot be asked, so on a hardware run the command flow is the evidence.
    /// </summary>
    private static void NoTransactionShouldBeOpen(PosNetTestTarget target)
        => target.Emulator?.TransactionOpen.Should().BeFalse();

    [Fact]
    public async Task CashSale_RunsTheFullTransaction_AndReturnsTheFiscalDocumentNumber()
    {
        using var target = PosNetTestTarget.Open(TestProject.Cassettes);

        var result = await target.Sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());

        target.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trpayment", "trend", "scnt");
        NoTransactionShouldBeOpen(target);
        DocumentNumberOf(result).Should().BePositive();
    }

    [Fact]
    public async Task CardSaleWithChange_SettlesLikeTheSpecExample()
    {
        using var target = PosNetTestTarget.Open(TestProject.Cassettes);

        await target.Sut.ProcessReceiptAsync(PLReceiptExamples.CardSaleWithChange());

        target.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trpayment", "trpayment", "trend", "scnt");
        var trend = target.SentCommands.Single(c => c.CommandId == "trend");
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("to", "200"));
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("re", "300"));
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("fp", "500"));
    }

    [Fact]
    public async Task DiscountSale_GrantsTheLineRabatOnTheLine_AndTheSubtotalRabatAfterIt()
    {
        using var target = PosNetTestTarget.Open(TestProject.Cassettes);

        await target.Sut.ProcessReceiptAsync(PLReceiptExamples.DiscountSale());

        target.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trdiscntsubtot", "trpayment", "trend", "scnt");
        var trline = target.SentCommands.Single(c => c.CommandId == "trline");
        // The line value stays the value before the rabat — the register prints both and totalizes
        // the difference.
        trline.Parameters.Should().Contain(new KeyValuePair<string, string>("wa", "1000"));
        trline.Parameters.Should().Contain(new KeyValuePair<string, string>("rd", "1"));
        trline.Parameters.Should().Contain(new KeyValuePair<string, string>("rw", "200"));
        var subtotal = target.SentCommands.Single(c => c.CommandId == "trdiscntsubtot");
        subtotal.Parameters.Should().Contain(new KeyValuePair<string, string>("rd", "1"));
        subtotal.Parameters.Should().Contain(new KeyValuePair<string, string>("rw", "100"));
        // 10.00 minus 2.00 on the line minus 1.00 off the subtotal is what the receipt is settled with.
        var trend = target.SentCommands.Single(c => c.CommandId == "trend");
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("to", "700"));
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("fp", "700"));
        NoTransactionShouldBeOpen(target);
    }

    [Fact]
    public async Task MarkupSale_GrantsTheNarzutOnTheLine_AndOnTheSubtotal()
    {
        using var target = PosNetTestTarget.Open(TestProject.Cassettes);

        await target.Sut.ProcessReceiptAsync(PLReceiptExamples.MarkupSale());

        target.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trdiscntsubtot", "trpayment", "trend", "scnt");
        // Same fields as a rabat, with rd0: the register adds the value instead of subtracting it.
        var trline = target.SentCommands.Single(c => c.CommandId == "trline");
        trline.Parameters.Should().Contain(new KeyValuePair<string, string>("wa", "1000"));
        trline.Parameters.Should().Contain(new KeyValuePair<string, string>("rd", "0"));
        trline.Parameters.Should().Contain(new KeyValuePair<string, string>("rw", "200"));
        var subtotal = target.SentCommands.Single(c => c.CommandId == "trdiscntsubtot");
        subtotal.Parameters.Should().Contain(new KeyValuePair<string, string>("rd", "0"));
        subtotal.Parameters.Should().Contain(new KeyValuePair<string, string>("rw", "100"));
        var trend = target.SentCommands.Single(c => c.CommandId == "trend");
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("to", "1300"));
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("fp", "1300"));
        NoTransactionShouldBeOpen(target);
    }

    [Fact]
    public async Task StornoSale_ReversesThePositionItNames_AndSettlesWithWhatIsLeft()
    {
        using var target = PosNetTestTarget.Open(TestProject.Cassettes);

        await target.Sut.ProcessReceiptAsync(PLReceiptExamples.StornoSale());

        target.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trline", "trline", "trpayment", "trend", "scnt");
        var lines = target.SentCommands.Where(c => c.CommandId == "trline").ToList();
        // The storno repeats the goods of the position it reverses — the coffee, not the beer that
        // was sold between them — and states the value the device verifies against what it printed.
        lines[2].Parameters.Should().Contain(new KeyValuePair<string, string>("na", "Kawa"));
        lines[2].Parameters.Should().Contain(new KeyValuePair<string, string>("st", "1"));
        lines[2].Parameters.Should().Contain(new KeyValuePair<string, string>("wa", "1000"));
        // A trend the device accepts is the evidence that the arithmetic matches its own: it verifies
        // the fiscal value it was sent against the receipt it printed (2805 ERR_ENDTOT_VERIFY).
        var trend = target.SentCommands.Single(c => c.CommandId == "trend");
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("to", "800"));
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("fp", "800"));
        NoTransactionShouldBeOpen(target);
    }

    [Fact]
    public async Task StornoOfDiscountedSale_CarriesTheRabatOnTheReversal()
    {
        using var target = PosNetTestTarget.Open(TestProject.Cassettes);

        await target.Sut.ProcessReceiptAsync(PLReceiptExamples.StornoOfDiscountedSale());

        target.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trline", "trline", "trpayment", "trend", "scnt");
        var lines = target.SentCommands.Where(c => c.CommandId == "trline").ToList();
        // The reversal repeats the rabat the position was sold with. Without it the register takes
        // the value before the rabat off the receipt: the same three commands with rw200 missing were
        // answered by the device with a fiscal value of 6.00 instead of 8.00, and it refused the
        // 8.00 that the positions still standing add up to (2805 ERR_ENDTOT_VERIFY).
        lines[2].Parameters.Should().Contain(new KeyValuePair<string, string>("st", "1"));
        lines[2].Parameters.Should().Contain(new KeyValuePair<string, string>("wa", "1000"));
        lines[2].Parameters.Should().Contain(new KeyValuePair<string, string>("rd", "1"));
        lines[2].Parameters.Should().Contain(new KeyValuePair<string, string>("rw", "200"));
        var trend = target.SentCommands.Single(c => c.CommandId == "trend");
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("to", "800"));
        NoTransactionShouldBeOpen(target);
    }

    [Fact]
    public async Task NipReceipt_PrintsTheBuyersNip()
    {
        using var target = PosNetTestTarget.Open(TestProject.Cassettes);

        await target.Sut.ProcessReceiptAsync(PLReceiptExamples.NipReceipt());

        target.SentMnemonics.Should().Equal("scomm", "trinit", "trnipset", "trline", "trpayment", "trend", "scnt");
        var trnipset = target.SentCommands.Single(c => c.CommandId == "trnipset");
        trnipset.Parameters.Should().Contain(new KeyValuePair<string, string>("ni", "1234563218"));
    }

    [Fact]
    public async Task ConsecutiveSales_ReuseTheConnection_AndNumberDocumentsSequentially()
    {
        using var target = PosNetTestTarget.Open(TestProject.Cassettes);

        var first = await target.Sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());
        var second = await target.Sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());

        // The register identity is read once, so only the first sale carries the leading scomm.
        target.SentMnemonics.Should().HaveCount(11);
        NoTransactionShouldBeOpen(target);
        DocumentNumberOf(second).Should().Be(DocumentNumberOf(first) + 1);
    }

    [Fact]
    public async Task ZeroReceipt_ReadsTheDeviceStatus()
    {
        using var target = PosNetTestTarget.Open(TestProject.Cassettes);

        await target.Sut.ProcessReceiptAsync(PLReceiptExamples.ZeroReceipt());

        target.SentMnemonics.Should().Equal("scomm");
    }

    [Fact]
    public async Task GetInfo_ReadsTheRegisterStateWithASingleStatusCommand()
    {
        using var target = PosNetTestTarget.Open(TestProject.Cassettes);

        var info = await target.Sut.GetInfoAsync();

        target.SentMnemonics.Should().Equal("scomm");
        var deviceInfo = PLDeviceInfo.FromPLSSCDInfo(info);
        deviceInfo.Should().NotBeNull();
        // Which state a real register reports depends on the device in front of you, but the status
        // has to be understood: Unknown means the fs flag was missing or in a shape this SCU does
        // not read — which would keep a fiscalized register from ever activating a PL queue.
        deviceInfo!.FiscalizationState.Should().BeOneOf(
            PLFiscalizationState.NonFiscal, PLFiscalizationState.Fiscalized, PLFiscalizationState.ReadOnly);
        // Every POSNET register carries a numer unikatowy, and it is printed on every fiscal document.
        deviceInfo.UniqueDeviceNumber.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>The fiscalized state is scripted into the emulator; a test device is typically non-fiscal.</summary>
    [EmulatorOnlyFact]
    public async Task GetInfo_ReportsTheFiscalizedRegister()
    {
        using var target = PosNetTestTarget.Scripted();

        var info = await target.Sut.GetInfoAsync();

        var deviceInfo = PLDeviceInfo.FromPLSSCDInfo(info);
        deviceInfo!.FiscalizationState.Should().Be(PLFiscalizationState.Fiscalized);
    }

    [EmulatorOnlyFact]
    public async Task RejectedLine_CancelsTheOpenTransactionOnTheDevice()
    {
        using var target = PosNetTestTarget.Scripted(emulator => emulator.ErrorOn("trline", 2005));

        var act = () => target.Sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());

        (await act.Should().ThrowAsync<PLDeviceErrorException>()).Which.ErrorCode.Should().Be(2005);
        target.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "prncancel");
        NoTransactionShouldBeOpen(target);
    }

    [EmulatorOnlyFact]
    public async Task SilentPrinter_IsAmbiguous_NothingElseIsSent()
    {
        using var target = PosNetTestTarget.Scripted(emulator => emulator.SwallowOn("trpayment"));

        var act = () => target.Sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());

        await act.Should().ThrowAsync<PosNetAmbiguousResponseException>();
        // Exactly one trpayment and no cleanup afterwards: the device may have printed — the
        // operator must verify before anything is sent again (triple-print protection).
        target.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trpayment");
    }

    [EmulatorOnlyFact]
    public async Task UnreachablePrinter_FailsAsDeviceUnreachable()
    {
        using var target = PosNetTestTarget.Scripted(unreachable: true);

        var act = () => target.Sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());

        await act.Should().ThrowAsync<PLDeviceUnreachableException>();
        target.SentMnemonics.Should().BeEmpty();
    }
}
