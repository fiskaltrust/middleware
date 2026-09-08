using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.PL.TestSupport.Verification;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.EndToEnd.AcceptanceTest;

/// <summary>
/// One test per committed business case under <c>BusinessCases/</c>: the JSON goes through the PL
/// queue into the PosNet SCU and onto the wire, and three things are held against it — the response
/// (state, signatures, identification), the commands that reached the register, and what the
/// register then reports having recorded. Without <c>SCU_PL_POSNET_DEVICE_URL</c> the register is the
/// emulator, replaying this test's cassette where one has been recorded and the device model
/// otherwise.
/// </summary>
public class PLBusinessCaseTests
{
    private const ulong ErrorStateMask = 0xFFFF_FFFF;
    private const ulong ErrorState = 0xEEEE_EEEE;

    private static void ShouldBeSigned(ReceiptResponse response)
    {
        using var _ = new AssertionScope();
        ((ulong)response.ftState & ErrorStateMask).Should().NotBe(ErrorState,
            string.Join(" | ", response.ftSignatures.Select(s => $"{s.Caption}: {s.Data}")));
        response.ftSignatures.Should().Contain(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.FiscalDocumentNumber);
        response.ftSignatures.Should().Contain(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.UniqueDeviceNumber);
        // The numer unikatowy of the register, as read off it before the sale.
        response.ftCashBoxIdentification.Should().NotBeNullOrWhiteSpace();
        response.ftReceiptIdentification.Should().EndWith(FiscalDocumentNumber.Of(response).ToString());
    }

    private static void ShouldBeStoredUnsigned(ReceiptResponse response)
        => ((ulong)response.ftState & ErrorStateMask).Should().Be(ErrorState,
            string.Join(" | ", response.ftSignatures.Select(s => $"{s.Caption}: {s.Data}")));

    private static KeyValuePair<string, string> Field(string key, string value) => new(key, value);

    [Fact]
    public async Task CashSaleReceipt_IsPrinted_AndLeavesItsFootprint()
    {
        using var harness = new PLEndToEndHarness();

        var receipt = await harness.SignAndVerifyAsync(TestProject.BusinessCase("SignRequestReceipt_CashSaleReceipt"));

        ShouldBeSigned(receipt.Response);
        harness.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trpayment", "trend", "scnt");
        harness.SentCommands.Single(c => c.CommandId == "trline").Parameters.Should().Contain(Field("pr", "369"));
        receipt.Discrepancies.Should().BeEmpty();

        var persisted = await harness.QueueItemsAsync();
        persisted.Should().ContainSingle().Which.response.Should().Contain("Numer dokumentu fiskalnego");
    }

    [Fact]
    public async Task CardSaleReceipt_PaysByCard()
    {
        using var harness = new PLEndToEndHarness();

        var receipt = await harness.SignAndVerifyAsync(TestProject.BusinessCase("SignRequestReceipt_CardSaleReceipt"));

        ShouldBeSigned(receipt.Response);
        harness.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trpayment", "trend", "scnt");
        // 2 × 8.00: the line states price, quantity and value, the card is payment type 2.
        harness.SentCommands.Single(c => c.CommandId == "trline").Parameters.Should().Contain(Field("il", "2.000")).And.Contain(Field("wa", "1600"));
        harness.SentCommands.Single(c => c.CommandId == "trpayment").Parameters.Should().Contain(Field("ty", "2"));
        receipt.Discrepancies.Should().BeEmpty();
    }

    [Fact]
    public async Task CardSaleWithChange_SettlesLikeTheSpecExample()
    {
        using var harness = new PLEndToEndHarness();

        var receipt = await harness.SignAndVerifyAsync(TestProject.BusinessCase("SignRequestReceipt_CardSaleWithChange"));

        ShouldBeSigned(receipt.Response);
        harness.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trpayment", "trpayment", "trend", "scnt");
        var trend = harness.SentCommands.Single(c => c.CommandId == "trend");
        trend.Parameters.Should().Contain(Field("to", "200")).And.Contain(Field("re", "300")).And.Contain(Field("fp", "500"));
        receipt.Expected.ChangeGrosze.Should().Be(300);
        receipt.Discrepancies.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscountReceipt_GrantsTheLineRabat_AndTheSubtotalRabat()
    {
        using var harness = new PLEndToEndHarness();

        var receipt = await harness.SignAndVerifyAsync(TestProject.BusinessCase("SignRequestReceipt_DiscountReceipt"));

        ShouldBeSigned(receipt.Response);
        harness.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trdiscntsubtot", "trpayment", "trend", "scnt");
        harness.SentCommands.Single(c => c.CommandId == "trline").Parameters.Should().Contain(Field("rd", "1")).And.Contain(Field("rw", "2300"));
        harness.SentCommands.Single(c => c.CommandId == "trdiscntsubtot").Parameters.Should().Contain(Field("rd", "1")).And.Contain(Field("rw", "100"));
        // 100.00 less 23.00 on the line less 1.00 off the subtotal.
        receipt.Expected.TotalGrosze.Should().Be(7600);
        receipt.Discrepancies.Should().BeEmpty();
    }

    [Fact]
    public async Task MarkupReceipt_GrantsTheLineNarzut_AndTheSubtotalNarzut()
    {
        using var harness = new PLEndToEndHarness();

        var receipt = await harness.SignAndVerifyAsync(TestProject.BusinessCase("SignRequestReceipt_MarkupReceipt"));

        ShouldBeSigned(receipt.Response);
        harness.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trdiscntsubtot", "trpayment", "trend", "scnt");
        harness.SentCommands.Single(c => c.CommandId == "trline").Parameters.Should().Contain(Field("rd", "0")).And.Contain(Field("rw", "2300"));
        harness.SentCommands.Single(c => c.CommandId == "trdiscntsubtot").Parameters.Should().Contain(Field("rd", "0")).And.Contain(Field("rw", "100"));
        receipt.Expected.TotalGrosze.Should().Be(12400);
        receipt.Discrepancies.Should().BeEmpty();
    }

    [Fact]
    public async Task StornoReceipt_ReversesThePositionItNames()
    {
        using var harness = new PLEndToEndHarness();

        var receipt = await harness.SignAndVerifyAsync(TestProject.BusinessCase("SignRequestReceipt_StornoReceipt"));

        ShouldBeSigned(receipt.Response);
        harness.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trline", "trline", "trpayment", "trend", "scnt");
        var storno = harness.SentCommands.Where(c => c.CommandId == "trline").Last();
        storno.Parameters.Should().Contain(Field("na", "Kawa")).And.Contain(Field("st", "1")).And.Contain(Field("wa", "1000"));
        receipt.Expected.TotalGrosze.Should().Be(800);
        receipt.Discrepancies.Should().BeEmpty();
    }

    [Fact]
    public async Task StornoOfDiscountedReceipt_CarriesTheRabatOnTheReversal()
    {
        using var harness = new PLEndToEndHarness();

        var receipt = await harness.SignAndVerifyAsync(TestProject.BusinessCase("SignRequestReceipt_StornoOfDiscountedReceipt"));

        ShouldBeSigned(receipt.Response);
        harness.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trline", "trline", "trpayment", "trend", "scnt");
        var storno = harness.SentCommands.Where(c => c.CommandId == "trline").Last();
        storno.Parameters.Should().Contain(Field("st", "1")).And.Contain(Field("rd", "1")).And.Contain(Field("rw", "200"));
        receipt.Expected.TotalGrosze.Should().Be(800);
        receipt.Discrepancies.Should().BeEmpty();
    }

    [Fact]
    public async Task NipReceipt_PrintsTheBuyersNip()
    {
        using var harness = new PLEndToEndHarness();

        var receipt = await harness.SignAndVerifyAsync(TestProject.BusinessCase("SignRequestReceipt_NipReceipt"));

        ShouldBeSigned(receipt.Response);
        harness.SentMnemonics.Should().Equal("scomm", "trinit", "trnipset", "trline", "trpayment", "trend", "scnt");
        // "PL5260250274" in cbCustomer travels as digits only.
        harness.SentCommands.Single(c => c.CommandId == "trnipset").Parameters.Should().Contain(Field("ni", "5260250274"));
        receipt.Discrepancies.Should().BeEmpty();
    }

    [Fact]
    public async Task ZeroReceipt_OnlyReadsTheRegisterStatus()
    {
        using var harness = new PLEndToEndHarness();

        var response = await harness.SignAsync(TestProject.BusinessCase("SignRequestReceipt_ZeroReceipt"));

        ((ulong)response.ftState & ErrorStateMask).Should().NotBe(ErrorState);
        harness.SentMnemonics.Should().Equal("scomm");
    }

    [Fact]
    public async Task InitialOperation_ActivatesTheQueue_AgainstTheRegister()
    {
        using var harness = new PLEndToEndHarness(startActive: false);

        var activation = await harness.SignAsync(TestProject.BusinessCase("SignRequestLifecycle_InitialOperation"));
        var sale = await harness.SignAndVerifyAsync(TestProject.BusinessCase("SignRequestReceipt_CashSaleReceipt"));

        ((ulong)activation.ftState & ErrorStateMask).Should().NotBe(ErrorState);
        activation.ftSignatures.Should().Contain(s => ((ulong)s.ftSignatureType & 0xFFFF) == 0x1001, "the initial operation receipt carries its own signature");
        // The activation reads the register state; the first sale reads the identity once more,
        // because the SCU caches it per instance and GetInfo does not fill that cache.
        harness.SentMnemonics.Should().Equal("scomm", "scomm", "trinit", "trline", "trpayment", "trend", "scnt");
        ShouldBeSigned(sale.Response);
        sale.Discrepancies.Should().BeEmpty();
    }

    [Fact]
    public async Task ConsecutiveSales_NumberTheDocumentsSequentially()
    {
        using var harness = new PLEndToEndHarness();

        var first = await harness.SignAndVerifyAsync(TestProject.BusinessCase("SignRequestReceipt_CashSaleReceipt"));
        var second = await harness.SignAndVerifyAsync(TestProject.BusinessCase("SignRequestReceipt_CardSaleReceipt"));

        ShouldBeSigned(first.Response);
        ShouldBeSigned(second.Response);
        first.Discrepancies.Should().BeEmpty();
        second.Discrepancies.Should().BeEmpty();
        FiscalDocumentNumber.Of(second.Response).Should().Be(FiscalDocumentNumber.Of(first.Response) + 1);
        // The register identity is read once per SCU instance.
        harness.SentMnemonics.Count(m => m == "scomm").Should().Be(1);
        (await harness.QueueItemsAsync()).Should().HaveCount(2);
    }

    /// <summary>
    /// Returns are a document of their own on a Polish register and not implemented in the PosNet
    /// SCU yet (PosNetReceiptMapper). Until they are, this pins the current behaviour: the queue
    /// stores the receipt unsigned and nothing reaches the register.
    /// </summary>
    [Fact]
    public async Task ReturnReceipt_IsStoredUnsigned_UntilTheScuSupportsReturns()
    {
        using var harness = new PLEndToEndHarness();
        await harness.SignAsync(TestProject.BusinessCase("SignRequestReceipt_CashSaleReceipt"));
        var commandsAfterSale = harness.SentMnemonics.Count();

        var response = await harness.SignAsync(TestProject.BusinessCase("SignRequestReceipt_ReturnReceipt"));

        ShouldBeStoredUnsigned(response);
        harness.SentMnemonics.Should().HaveCount(commandsAfterSale, "a refused return sends nothing to the register");
        (await harness.QueueItemsAsync()).Should().HaveCount(2, "the queue keeps the refused request too");
    }

    /// <summary>
    /// Daily and periodic reports are not implemented in the PosNet SCU yet (PosNetPLSSCD). Until
    /// they are, this pins the current behaviour: the closing is stored unsigned and no report is
    /// requested from the register.
    /// </summary>
    [Fact]
    public async Task DailyClosing_IsStoredUnsigned_UntilTheScuSupportsReports()
    {
        using var harness = new PLEndToEndHarness();

        var response = await harness.SignAsync(TestProject.BusinessCase("SignRequestDailyOperations_DailyClosing"));

        ShouldBeStoredUnsigned(response);
        harness.SentMnemonics.Should().BeEmpty();
    }
}
