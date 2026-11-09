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

    [Fact]
    public async Task ShiftClosing_PrintsThePerpetualTotalOnly()
    {
        await SignAsync(CashSale());
        var closing = await SignAsync(Request(ReceiptCase.ShiftClosing0x2010));

        ReadableTotal(SignatureOf(closing, SignatureTypeFR.PerpetualTotals)).Should().Be("12.00");
        closing.ftSignatures.Should().NotContain(x => ((long) x.ftSignatureType & 0xFFFF) == 0x0002 || x.ftSignatureType.IsType(SignatureTypeFR.DayTotals));
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
