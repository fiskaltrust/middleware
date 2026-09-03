using System.Globalization;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.SCU.PL.AcceptanceTest.PosNetPrinter;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Models;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.AcceptanceTest;

/// <summary>
/// Acceptance tests in the shape of the Italian SCU acceptance suite — the SUT is built through
/// the ScuBootstrapper like the launcher would — but market-scoped: they run over real TCP against
/// whatever <see cref="PosNetTestTarget"/> selects, so the whole stack (transport, framing, codec,
/// transaction flow) is exercised against a recorded printer in CI and against the device itself
/// by setting <c>SCU_PL_POSNET_DEVICE_URL</c>, without touching a test.
/// </summary>
public class PosNetPLSSCDAcceptanceTests
{
    private const string FiscalDocumentNumber = "Numer dokumentu fiskalnego";

    /// <summary>
    /// The document number a receipt was printed under. Asserted relatively throughout: on a real
    /// printer the counter carries whatever history the device has.
    /// </summary>
    private static int DocumentNumberOf(ProcessResponse response)
    {
        var signature = response.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == FiscalDocumentNumber).Subject;
        return int.Parse(signature.Data, NumberStyles.None, CultureInfo.InvariantCulture);
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
        using var target = PosNetTestTarget.Open();

        var result = await target.Sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());

        target.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trpayment", "trend", "scnt");
        NoTransactionShouldBeOpen(target);
        DocumentNumberOf(result).Should().BePositive();
    }

    [Fact]
    public async Task CardSaleWithChange_SettlesLikeTheSpecExample()
    {
        using var target = PosNetTestTarget.Open();

        await target.Sut.ProcessReceiptAsync(PLReceiptExamples.CardSaleWithChange());

        target.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trpayment", "trpayment", "trend", "scnt");
        var trend = target.SentCommands.Single(c => c.CommandId == "trend");
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("to", "200"));
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("re", "300"));
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("fp", "500"));
    }

    [Fact]
    public async Task NipReceipt_PrintsTheBuyersNip()
    {
        using var target = PosNetTestTarget.Open();

        await target.Sut.ProcessReceiptAsync(PLReceiptExamples.NipReceipt());

        target.SentMnemonics.Should().Equal("scomm", "trinit", "trnipset", "trline", "trpayment", "trend", "scnt");
        var trnipset = target.SentCommands.Single(c => c.CommandId == "trnipset");
        trnipset.Parameters.Should().Contain(new KeyValuePair<string, string>("ni", "1234563218"));
    }

    [Fact]
    public async Task ConsecutiveSales_ReuseTheConnection_AndNumberDocumentsSequentially()
    {
        using var target = PosNetTestTarget.Open();

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
        using var target = PosNetTestTarget.Open();

        await target.Sut.ProcessReceiptAsync(PLReceiptExamples.ZeroReceipt());

        target.SentMnemonics.Should().Equal("scomm");
    }

    [Fact]
    public async Task GetInfo_ReadsTheRegisterStateWithASingleStatusCommand()
    {
        using var target = PosNetTestTarget.Open();

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

        target.SentMnemonics.Should().Equal("scomm", "eparagonidznext", "trinit", "trline", "trpayment", "trend", "scnt", "eparagonbufferget");
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
        target.SentMnemonics.Should().Equal("scomm", "eparagonidznext");
        NoTransactionShouldBeOpen(target);
    }

    [Fact]
    public async Task SaleWithoutEReceiptCustomerId_NeverTouchesTheEParagonCommands()
    {
        using var target = PosNetTestTarget.Open();

        var result = await target.Sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());

        target.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trpayment", "trend", "scnt");
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
}
