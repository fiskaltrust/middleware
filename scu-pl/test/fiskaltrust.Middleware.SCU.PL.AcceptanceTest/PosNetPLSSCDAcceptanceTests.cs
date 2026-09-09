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
        // A scripted register has no pinned table, so the SCU reads it first.
        target.SentMnemonics.Should().Equal("sfsk", "scomm", "trinit", "trline", "prncancel");
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
        target.SentMnemonics.Should().Equal("sfsk", "scomm", "trinit", "trline", "trpayment");
    }

    /// <summary>
    /// The e-paragon flow (middleware#764): the IDZ from cbCustomer is bound with eparagonidznext
    /// strictly before trinit, and the response carries the eDokument id plus the best-effort
    /// delivery state. Emulator-only: eDokument needs a fiscalized, e-paragon-configured device.
    /// </summary>
    [EmulatorOnlyFact]
    public async Task EReceiptSale_BindsTheIdzBeforeTheTransaction_AndReturnsTheEDocumentId()
    {
        using var target = PosNetTestTarget.Scripted(emulator => emulator.NextEDocumentId = 7777);

        var result = await target.Sut.ProcessReceiptAsync(PLReceiptExamples.EReceiptSale("KID0123456789ABC"));

        target.SentMnemonics.Should().Equal("sfsk", "scomm", "eparagonidznext", "trinit", "trline", "trpayment", "trend", "scnt", "eparagonbufferget");
        var binding = target.SentCommands.Single(c => c.CommandId == "eparagonidznext");
        binding.Parameters.Should().Contain(new KeyValuePair<string, string>("id", "KID0123456789ABC"));
        var readback = target.SentCommands.Single(c => c.CommandId == "eparagonbufferget");
        readback.Parameters.Should().Contain(new KeyValuePair<string, string>("hd", "7777"));
        NoTransactionShouldBeOpen(target);
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Identyfikator eDokumentu" && s.Data == "7777");
        // The emulator's buffer record is prN st1 — an electronic document, no paper produced.
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Status eDokumentu" && s.Data == "electronic (st1)");
    }

    [EmulatorOnlyFact]
    public async Task EReceiptSale_OnANonFiscalizedDevice_FailsBeforeAnythingIsPrinted()
    {
        using var target = PosNetTestTarget.Scripted(emulator => emulator.ErrorOn("eparagonidznext", 2034));

        var act = () => target.Sut.ProcessReceiptAsync(PLReceiptExamples.EReceiptSale());

        (await act.Should().ThrowAsync<PLDeviceErrorException>()).Which.ErrorCode.Should().Be(2034);
        // The rejected binding is the last frame on the wire: no trinit, no line, no cancel —
        // nothing was sent to the device for this receipt after the failed bind.
        target.SentMnemonics.Should().Equal("sfsk", "scomm", "eparagonidznext");
        NoTransactionShouldBeOpen(target);
    }

    /// <summary>
    /// A confirmed binding without the promised ha is armed on the device but untrackable —
    /// the SCU must clear it (eparagonidzcancel) before failing, or the next plain sale would
    /// inherit this customer's IDZ and deliver their e-receipt to the wrong recipient.
    /// </summary>
    [EmulatorOnlyFact]
    public async Task EReceiptSale_WhenTheBindingConfirmsWithoutTheHandle_CancelsTheBindingBeforeFailing()
    {
        using var target = PosNetTestTarget.Scripted(emulator => emulator.OmittingEDocumentIdOnBind());

        var act = () => target.Sut.ProcessReceiptAsync(PLReceiptExamples.EReceiptSale());

        await act.Should().ThrowAsync<PLSSCDException>();
        // The armed binding is cleared and nothing is printed: no trinit ever goes out.
        target.SentMnemonics.Should().Equal("sfsk", "scomm", "eparagonidznext", "eparagonidzcancel");
        NoTransactionShouldBeOpen(target);
    }

    /// <summary>
    /// The SCU is a singleton and the client lock only makes single commands atomic — the device
    /// lock must serialize whole sequences, or a concurrent plain sale could slip its trinit
    /// between another sale's eparagonidznext and trinit and consume that customer's binding
    /// (middleware#766 review).
    /// </summary>
    [EmulatorOnlyFact]
    public async Task ConcurrentSales_NeverInterleaveOnTheWire_SoTheBindingStaysWithItsSale()
    {
        using var target = PosNetTestTarget.Scripted();

        await Task.WhenAll(
            target.Sut.ProcessReceiptAsync(PLReceiptExamples.EReceiptSale("KIDCONCURRENT01")),
            target.Sut.ProcessReceiptAsync(PLReceiptExamples.CashSale()));

        var mnemonics = target.SentMnemonics.ToList();
        var bind = mnemonics.IndexOf("eparagonidznext");
        bind.Should().BeGreaterThanOrEqualTo(0);
        // The bound sale's transaction opens immediately after its binding …
        mnemonics[bind + 1].Should().Be("trinit");
        // … and exactly one transaction runs between the binding and its trend: the plain sale's
        // trinit never slips into the bound sequence.
        var trendAfterBind = mnemonics.IndexOf("trend", bind);
        mnemonics.Skip(bind).Take(trendAfterBind - bind).Count(m => m == "trinit").Should().Be(1);
        NoTransactionShouldBeOpen(target);
    }

    [Fact]
    public async Task SaleWithoutEReceiptCustomerId_NeverTouchesTheEParagonCommands()
    {
        using var target = PosNetTestTarget.Scripted();

        var result = await target.Sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());

        target.SentMnemonics.Should().Equal("sfsk", "scomm", "trinit", "trline", "trpayment", "trend", "scnt");
        result.ReceiptResponse.ftSignatures.Should().NotContain(s => s.Caption == "Identyfikator eDokumentu" || s.Caption == "Status eDokumentu");
    }

    [EmulatorOnlyFact]
    public async Task UnreachablePrinter_FailsAsDeviceUnreachable()
    {
        using var target = PosNetTestTarget.Scripted(unreachable: true);

        var act = () => target.Sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());

        await act.Should().ThrowAsync<PLDeviceUnreachableException>();
        target.SentMnemonics.Should().BeEmpty();
    }

    /// <summary>
    /// Without a configured table the SCU reads the register's own before the first sale. Scripted,
    /// because the committed cassettes were recorded with the table pinned and hold no sfsk.
    /// </summary>
    [EmulatorOnlyFact]
    public async Task Sale_WithoutAConfiguredRateTable_ReadsThePtuTableOffTheRegisterFirst()
    {
        using var target = PosNetTestTarget.Scripted();

        var result = await target.Sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());
        var info = PLDeviceInfo.FromPLSSCDInfo(await target.Sut.GetInfoAsync());

        target.SentMnemonics.Should().Equal("sfsk", "scomm", "trinit", "trline", "trpayment", "trend", "scnt", "scomm");
        // Candies at 8% land in slot B of the table the register reports — the same table GetInfo hands to the queue.
        target.SentCommands.Single(c => c.CommandId == "trline").Parameters.Should().Contain(new KeyValuePair<string, string>("vt", "1"));
        info!.VatRateTable.Select(e => e.PtuSlot).Should().Equal("A", "B", "C", "D", "G");
        DocumentNumberOf(result).Should().BePositive();
    }

    [Fact]
    public async Task Return_PrintsTheGoodsReturn_AndReportsNoFiscalDocumentNumber()
    {
        using var target = PosNetTestTarget.Open(TestProject.Cassettes);

        var result = await target.Sut.ProcessReceiptAsync(PLReceiptExamples.Return());

        target.SentMnemonics.Should().Equal("scomm", "stocash");
        target.SentCommands.Single(c => c.CommandId == "stocash").Parameters.Should().Contain(new KeyValuePair<string, string>("kw", "999"));
        FiscalDocumentNumber.Of(result.ReceiptResponse).Should().BeNull("a goods return is a non-fiscal printout");
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Zwrot towaru (wydruk niefiskalny)").Which.Data.Should().Be("9.99");
        NoTransactionShouldBeOpen(target);
    }

    [Fact]
    public async Task DailyClosing_AfterASale_PrintsTheDailyReport_AndReportsItsNumber()
    {
        using var target = PosNetTestTarget.Open(TestProject.Cassettes);
        await target.Sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());
        var before = await target.Probe.SnapshotAsync();

        var result = await target.Sut.ProcessReceiptAsync(PLReceiptExamples.DailyClosing());
        var after = await target.Probe.SnapshotAsync();

        target.SentMnemonics.Should().EndWith(["dailyrep", "scnt"]);
        target.SentCommands.Single(c => c.CommandId == "dailyrep").Parameters.Should().ContainKey("da");
        var reportNumber = result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer raportu dobowego").Subject.Data;
        // The report the register just printed is the one it announced as next, and the day starts over.
        reportNumber.Should().Be($"{before.NextDailyReportNumber}");
        after.NextDailyReportNumber.Should().Be(before.NextDailyReportNumber + 1);
        after.ReceiptTotalizersGrosze.Should().AllSatisfy(v => v.Should().Be(0));
    }
}
