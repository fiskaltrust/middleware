using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Scenarios;

/// <summary>
/// Drives a real QueueFR.v2 through its sign processor against the in-memory storage and an SCU,
/// so the chaining, numbering and signature wiring is exercised end to end. Each SCU the queue
/// supports runs the same scenarios through a subclass.
/// </summary>
public abstract class FRScenarioTests
{
    private readonly Func<string, Task<string>> _signProcessor;
    private readonly Guid _cashBoxId;

    protected abstract IFRSSCD CreateScu();

    protected FRScenarioTests()
    {
        (_signProcessor, _cashBoxId) = FRTestQueue.Create(CreateScu());
    }

    [Fact]
    public async Task Copies_AreNumberedPerCopiedReceipt()
    {
        var sale = Request(ReceiptCase.PointOfSaleReceipt0x0001);
        await SignAsync(sale);
        ReceiptRequest CopyOfTheSale()
        {
            var copy = Request(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010);
            copy.cbPreviousReceiptReference = sale.cbReceiptReference;
            return copy;
        }

        var first = await SignAsync(CopyOfTheSale());
        var second = await SignAsync(CopyOfTheSale());
        var withoutReference = await SignAsync(Request(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010));

        first.ftSignatures.Should().Contain(x => x.Caption == "Duplicata" && x.Data == $"1. Duplicata de {sale.cbReceiptReference}");
        second.ftSignatures.Should().Contain(x => x.Caption == "Duplicata" && x.Data == $"2. Duplicata de {sale.cbReceiptReference}");
        second.ftReceiptIdentification.Should().EndWith("D2");
        withoutReference.ftState.IsState(State.Error).Should().BeTrue("market-fr#235: a copy has to name the receipt it copies");
    }

    [Fact]
    public async Task CashSale_IsSignedAndNumberedInTheTicketChain()
    {
        var response = await SignAsync(CashSale());

        response.ftState.IsState(State.Error).Should().BeFalse();
        response.ftReceiptIdentification.Should().EndWith("T1");
        SignatureOf(response, SignatureTypeFR.ReceiptSignature).Should().NotBeNullOrEmpty();
        var qr = SignatureOf(response, SignatureTypeFR.ReceiptSignature)!;
        qr[(qr.LastIndexOf('.') + 1)..].Should().MatchRegex("^[A-Za-z0-9_-]+$", "the QR code ends with the chained signature, in Base64Url (ADR 0001)");
        SignatureOf(response, SignatureTypeFR.ChainHash).Should().BeNull("an entry chains on the previous signature, not on a separate hash");
        response.ftSignatures.Should().ContainSingle(x => x.Caption == "Nombre de lignes").Which.Data.Should().Be("1");
    }

    [Fact]
    public async Task ConsecutiveSales_ContinueTheSameChain()
    {
        var first = await SignAsync(CashSale());
        var second = await SignAsync(CashSale());

        first.ftReceiptIdentification.Should().EndWith("T1");
        second.ftReceiptIdentification.Should().EndWith("T2");
        SignatureOf(second, SignatureTypeFR.ReceiptSignature).Should().NotBe(SignatureOf(first, SignatureTypeFR.ReceiptSignature));
    }

    [Fact]
    public async Task InvoiceAndTicket_UseSeparateChains()
    {
        await SignAsync(CashSale());
        var invoice = await SignAsync(Request(ReceiptCase.InvoiceB2B0x1002));

        invoice.ftState.IsState(State.Error).Should().BeFalse();
        invoice.ftReceiptIdentification.Should().EndWith("I1");
    }

    [Fact]
    public async Task DailyClosing_IsSignedIntoTheDailyClosingChain()
    {
        var response = await SignAsync(Request(ReceiptCase.DailyClosing0x2011));

        response.ftState.IsState(State.Error).Should().BeFalse();
        response.ftReceiptIdentification.Should().EndWith("Z1");
    }

    [Fact]
    public async Task DailyClosing_ReportsTheTurnoverAccumulatedSinceTheLastClosing()
    {
        await SignAsync(CashSale());
        await SignAsync(CashSale());
        var closing = await SignAsync(Request(ReceiptCase.DailyClosing0x2011));

        // Two 12.00 EUR sales - a closing attests the period, not its own (empty) item list.
        TotalsOf(closing).Should().Be(("24.00", "24.00"));

        await SignAsync(CashSale());
        var secondClosing = await SignAsync(Request(ReceiptCase.DailyClosing0x2011));

        TotalsOf(secondClosing).Should().Be(("12.00", "36.00"), "the first closing reset the day, the perpetual total is never reset");
    }

    /// <summary>
    /// Every FR SCU prints the closing's totals as readable items, as v1.2 did: the period totals
    /// (low word 0x0003 day, 0x0004 month, 0x0005 year) and the perpetual total (0x0007), each
    /// starting with the grand total tax included (market-fr#221, decision 5).
    /// </summary>
    [Theory]
    [InlineData(ReceiptCase.DailyClosing0x2011, SignatureTypeFR.DayTotals)]
    [InlineData(ReceiptCase.MonthlyClosing0x2012, SignatureTypeFR.MonthTotals)]
    [InlineData(ReceiptCase.YearlyClosing0x2013, SignatureTypeFR.YearTotals)]
    public async Task Closing_PrintsItsTotalsAsReadableItems(ReceiptCase closingCase, SignatureTypeFR periodType)
    {
        await SignAsync(CashSale());
        var closing = await SignAsync(Request(closingCase));

        ReadableTotal(SignatureOf(closing, periodType)).Should().Be("12.00");
        ReadableTotal(SignatureOf(closing, SignatureTypeFR.PerpetualTotals)).Should().Be("12.00");
        closing.ftSignatures.Should().NotContain(x => ((long) x.ftSignatureType & 0xFFFF) == 0x0002, "low word 0x0002 stays reserved");
    }

    /// <summary>
    /// A shift closing prints its shift totals, as v1.2 did, under the v2 type 0x000D (low word 0x0002
    /// stays reserved), and the perpetual total. It prints no day totals (market-fr#221, G5).
    /// </summary>
    [Fact]
    public async Task ShiftClosing_PrintsItsShiftTotalsAndThePerpetualTotal()
    {
        await SignAsync(CashSale());
        var firstShift = await SignAsync(Request(ReceiptCase.ShiftClosing0x2010));

        firstShift.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ShiftPeriodTotals)).Caption.Should().Be("Shift Totals");
        ReadableTotal(SignatureOf(firstShift, SignatureTypeFR.ShiftPeriodTotals)).Should().Be("12.00");
        ReadableTotal(SignatureOf(firstShift, SignatureTypeFR.PerpetualTotals)).Should().Be("12.00");
        firstShift.ftSignatures.Should().NotContain(x => ((long) x.ftSignatureType & 0xFFFF) == 0x0002 || x.ftSignatureType.IsType(SignatureTypeFR.DayTotals));

        await SignAsync(CashSale());
        var secondShift = await SignAsync(Request(ReceiptCase.ShiftClosing0x2010));

        ReadableTotal(SignatureOf(secondShift, SignatureTypeFR.ShiftPeriodTotals)).Should().Be("12.00", "the first shift closing reset the shift");
        ReadableTotal(SignatureOf(secondShift, SignatureTypeFR.PerpetualTotals)).Should().Be("24.00");
    }

    [Theory]
    [InlineData(ReceiptCase.DailyClosing0x2011)]
    [InlineData(ReceiptCase.MonthlyClosing0x2012)]
    [InlineData(ReceiptCase.YearlyClosing0x2013)]
    public async Task OtherClosings_PrintNoShiftTotals(ReceiptCase closingCase)
    {
        await SignAsync(CashSale());
        var closing = await SignAsync(Request(closingCase));

        SignatureOf(closing, SignatureTypeFR.ShiftPeriodTotals).Should().BeNull();
    }

    /// <summary>
    /// The archive receipt (0x3011) is signed into chain A and prints the sales since the previous
    /// archive receipt as "Archive Totals" (low word 0x0006, as v1.2), then the perpetual total
    /// (market-fr#221).
    /// </summary>
    [Fact]
    public async Task ArchiveReceipt_IsSignedIntoTheArchiveChainAndPrintsTheArchiveTotals()
    {
        await SignAsync(CashSale());
        var first = await SignAsync(Request((ReceiptCase) 0x3011));

        first.ftState.IsState(State.Error).Should().BeFalse();
        first.ftReceiptIdentification.Should().EndWith("A1");
        first.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ArchiveTotals)).Caption.Should().Be("Archive Totals");
        ReadableTotal(SignatureOf(first, SignatureTypeFR.ArchiveTotals)).Should().Be("12.00");
        first.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.DayTotals)).Caption.Should().Be("Day Totals", "as v1.2, the archive receipt closes the day and prints its totals");
        ReadableTotal(SignatureOf(first, SignatureTypeFR.DayTotals)).Should().Be("12.00");
        ReadableTotal(SignatureOf(first, SignatureTypeFR.PerpetualTotals)).Should().Be("12.00");

        await SignAsync(CashSale());
        var second = await SignAsync(Request((ReceiptCase) 0x3011));

        second.ftReceiptIdentification.Should().EndWith("A2");
        ReadableTotal(SignatureOf(second, SignatureTypeFR.ArchiveTotals)).Should().Be("12.00", "the first archive receipt reset the archive period");
        ReadableTotal(SignatureOf(second, SignatureTypeFR.PerpetualTotals)).Should().Be("24.00");
    }

    [Fact]
    public async Task Sale_PrintsNoTotalsItems()
    {
        var sale = await SignAsync(CashSale());

        SignatureOf(sale, SignatureTypeFR.PerpetualTotals).Should().BeNull();
    }

    /// <summary>The grand total on the first line of a readable totals item ("Total TTC: 12.00").</summary>
    protected static string? ReadableTotal(string? item) => item?.Split('\n')[0].Split(": ")[1];

    [Fact]
    public async Task NonEuroReceipt_IsRejected()
    {
        var request = CashSale();
        request.Currency = Currency.CHF;

        var response = await SignAsync(request);

        response.ftState.IsState(State.Error).Should().BeTrue("a French queue totalizes in EUR only");
    }

    // ---- Corrections, translated from the v1.2 scenarios (market-fr#221 gap G1) ----
    // v1.2 corrected a paid receipt with the reverse flag 0x40000. In v2 that business case is a
    // refund (IsRefund 0x0100_0000): the void (0x4_0000) is only for a receipt whose goods and
    // money never changed hands, and a voided receipt can no longer be referenced
    // (docs: general/cash-register-integration/refunds-and-voids).

    [Fact]
    public async Task RefundThenCorrectedSale_ReferencingTheOriginal_IsSignedInTheTicketChain()
    {
        // v1.2 CheckDataUnalterablity_ChangeInvoiceTicket_Quantity, IsPaymentDataRecorded_Test1/2/4,
        // CheckDataUnalterablity_Change_Payment (LNE conditions 5 and 6).
        var sale = Sale(ReceiptCase.PointOfSaleReceipt0x0001, quantity: 5, amount: 200m);
        var original = await SignAsync(sale);

        var refund = FullRefundOf(sale);
        var refunded = await SignAsync(refund);

        var corrected = Sale(ReceiptCase.PointOfSaleReceipt0x0001, quantity: 3, amount: 120m);
        corrected.cbPreviousReceiptReference = sale.cbReceiptReference;
        var correction = await SignAsync(corrected);

        foreach (var response in new[] { original, refunded, correction })
        {
            response.ftState.IsState(State.Error).Should().BeFalse("a refund leaves the original referenceable, unlike a void");
            SignatureOf(response, SignatureTypeFR.ReceiptSignature).Should().NotBeNullOrEmpty();
        }
        original.ftReceiptIdentification.Should().EndWith("T1");
        refunded.ftReceiptIdentification.Should().EndWith("T2", "the refund is signed in the chain of the receipt it refunds");
        correction.ftReceiptIdentification.Should().EndWith("T3");

        var closing = await SignAsync(Request(ReceiptCase.DailyClosing0x2011));
        TotalsOf(closing).Day.Should().Be("120.00", "200 sold, 200 refunded, 120 sold again");
    }

    [Fact]
    public async Task InvoiceRefund_IsSignedInTheInvoiceChainAndCancelsTheTurnover()
    {
        // v1.2 CheckDataUnalterablity_CancelInvoice (LNE condition 6).
        var invoice = Sale(ReceiptCase.InvoiceB2C0x1001, quantity: 5, amount: 200m);
        await SignAsync(invoice);

        var refunded = await SignAsync(FullRefundOf(invoice));

        refunded.ftState.IsState(State.Error).Should().BeFalse();
        refunded.ftReceiptIdentification.Should().EndWith("I2");
        var closing = await SignAsync(Request(ReceiptCase.DailyClosing0x2011));
        TotalsOf(closing).Day.Should().Be("0.00");
    }

    [Fact]
    public async Task TechnicalVoid_IsSigned_AndTheVoidedReceiptCanNoLongerBeReferenced()
    {
        // v2-only rule (shared Localization.v2 PreviousReceiptMustNotBeVoided): pins the departure
        // from v1.2, where a corrected receipt could reference the reversed one.
        var sale = Sale(ReceiptCase.PointOfSaleReceipt0x0001, quantity: 2, amount: 6m);
        await SignAsync(sale);

        var voided = await SignAsync(VoidOf(sale));
        var corrected = Sale(ReceiptCase.PointOfSaleReceipt0x0001, quantity: 2, amount: 6m);
        corrected.cbPreviousReceiptReference = sale.cbReceiptReference;
        var refused = await SignAsync(corrected);

        voided.ftState.IsState(State.Error).Should().BeFalse();
        voided.ftReceiptIdentification.Should().EndWith("T2");
        refused.ftState.IsState(State.Error).Should().BeTrue("a voided receipt can no longer be referenced");
        refused.ftReceiptIdentification.Should().NotMatchRegex("T[0-9]+$", "a refused receipt takes no number");

        var closing = await SignAsync(Request(ReceiptCase.DailyClosing0x2011));
        TotalsOf(closing).Day.Should().Be("0.00");
    }

    [Fact]
    public async Task DiscountLine_ReducesTheTurnover()
    {
        // v1.2 IsPaymentDataRecorded_Test5 (100 - 20 = 80) and _Test6 (gap G6). In v2 a negative
        // line has to carry ExtraOrDiscount, the shared NegativeAmounts rule refuses it otherwise.
        var sale = Request(ReceiptCase.PointOfSaleReceipt0x0001);
        sale.cbChargeItems =
        [
            new ChargeItem { Amount = 100m, VATRate = 20m, Quantity = 1, Description = "Chaussures", Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") },
            new ChargeItem { Amount = -20m, VATRate = 20m, Quantity = 1, Description = "Remise", Currency = Currency.EUR, ftChargeItemCase = WithFlag(ChargeItemCase.NormalVatRate.WithCountry("FR"), ChargeItemCaseFlags.ExtraOrDiscount) },
        ];
        sale.cbPayItems = [new PayItem { Amount = 80m, Description = "Especes", Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") }];

        var response = await SignAsync(sale);
        var closing = await SignAsync(Request(ReceiptCase.DailyClosing0x2011));

        response.ftState.IsState(State.Error).Should().BeFalse();
        response.ftSignatures.Should().ContainSingle(x => x.Caption == "Nombre de lignes").Which.Data.Should().Be("1", "a discount is not a sales line");
        TotalsOf(closing).Day.Should().Be("80.00");
    }

    private ReceiptRequest Sale(ReceiptCase receiptCase, decimal quantity, decimal amount)
    {
        var request = Request(receiptCase);
        request.cbChargeItems =
        [
            new ChargeItem { Amount = amount, VATRate = 5.5m, Quantity = quantity, Description = "Chaussures", Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.DiscountedVatRate1.WithCountry("FR") },
        ];
        request.cbPayItems = [new PayItem { Amount = amount, Quantity = 1, Description = "Especes", Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") }];
        return request;
    }

    /// <summary>A full refund: the receipt flag IsRefund, every item inverted, the original referenced.</summary>
    private ReceiptRequest FullRefundOf(ReceiptRequest original) => Inverted(original, (long) ReceiptCaseFlags.Refund, chargeItemFlag: 0, payItemFlag: 0);

    /// <summary>A void: the receipt flag IsVoid and IsVoid on every inverted item, the original referenced.</summary>
    private ReceiptRequest VoidOf(ReceiptRequest original) => Inverted(original, (long) ReceiptCaseFlags.Void, (long) ChargeItemCaseFlags.Void, (long) PayItemCaseFlags.Void);

    private ReceiptRequest Inverted(ReceiptRequest original, long receiptFlag, long chargeItemFlag, long payItemFlag)
    {
        var request = Request((ReceiptCase) ((long) original.ftReceiptCase & 0xFFFF));
        request.ftReceiptCase = (ReceiptCase) ((long) original.ftReceiptCase | receiptFlag);
        request.cbPreviousReceiptReference = original.cbReceiptReference;
        request.cbChargeItems = original.cbChargeItems.Select(x => new ChargeItem
        {
            Amount = -x.Amount, VATRate = x.VATRate, Quantity = -x.Quantity, Description = x.Description, Currency = x.Currency,
            ftChargeItemCase = (ChargeItemCase) ((long) x.ftChargeItemCase | chargeItemFlag),
        }).ToList();
        request.cbPayItems = original.cbPayItems.Select(x => new PayItem
        {
            Amount = -x.Amount, Quantity = -x.Quantity, Description = x.Description, Currency = x.Currency,
            ftPayItemCase = (PayItemCase) ((long) x.ftPayItemCase | payItemFlag),
        }).ToList();
        return request;
    }

    private static ChargeItemCase WithFlag(ChargeItemCase chargeItemCase, ChargeItemCaseFlags flag) => (ChargeItemCase) ((long) chargeItemCase | (long) flag);

    private async Task<ReceiptResponse> SignAsync(ReceiptRequest request)
    {
        var responseJson = await _signProcessor(JsonSerializer.Serialize(request));
        var response = JsonSerializer.Deserialize<ReceiptResponse>(responseJson);
        response.Should().NotBeNull();
        return response!;
    }

    /// <summary>
    /// The day and perpetual totalizers a closing attests. By default read off the printed total
    /// items; an SCU that only carries them inside its signed data set overrides this.
    /// </summary>
    protected virtual (string? Day, string? Perpetual) TotalsOf(ReceiptResponse closing)
        => (ReadableTotal(SignatureOf(closing, SignatureTypeFR.DayTotals)), ReadableTotal(SignatureOf(closing, SignatureTypeFR.PerpetualTotals)));

    /// <summary>Reads a signature off the response the way a POS would, by its FR signature type.</summary>
    protected static string? SignatureOf(ReceiptResponse response, SignatureTypeFR type)
        => response.ftSignatures.FirstOrDefault(x => x.ftSignatureType.IsType(type))?.Data;

    private ReceiptRequest Request(ReceiptCase receiptCase) => new()
    {
        ftCashBoxID = _cashBoxId,
        cbTerminalID = "1",
        cbReceiptReference = Guid.NewGuid().ToString("N")[..8],
        cbReceiptMoment = DateTime.UtcNow,
        Currency = Currency.EUR,
        ftReceiptCase = receiptCase.WithCountry("FR"),
        cbChargeItems = [],
        cbPayItems = [],
    };

    private ReceiptRequest CashSale()
    {
        var request = Request(ReceiptCase.PointOfSaleReceipt0x0001);
        request.cbChargeItems =
        [
            new ChargeItem { Amount = 12.00m, VATAmount = 2.00m, VATRate = 20m, Quantity = 1, Description = "Cafe", Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") },
        ];
        request.cbPayItems =
        [
            new PayItem { Amount = 12.00m, Description = "Especes", Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") },
        ];
        return request;
    }
}
