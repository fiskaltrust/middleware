using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Validation;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// market-fr#221, ADR 0016. The negative-sign exemptions for a down-payment deduction and for money
/// leaving the drawer are French, applied by the FR validator over the shared NegativeAmounts rule; a
/// handwritten receipt (flag 0x0008) keeps the balance, VAT and description checks.
/// </summary>
public class FRNegativeAndHandwrittenTests
{
    private const long HandWritten = 0x0008_0000;

    private static readonly fiskaltrust.storage.V0.ftQueue Queue = new() { CountryCode = "FR" };

    private static ReceiptValidatorFR Validator() => new(
        new ReceiptReferenceProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult<IMiddlewareQueueItemRepository>(new InMemoryQueueItemRepository()))));

    private static async Task<List<string>> ErrorsAsync(ReceiptRequest request)
        => (await Validator().ValidateAsync(request, Queue)).Errors.Select(x => x.ErrorCode).ToList();

    private static ChargeItem Item(decimal amount, long chargeItemCase = 0x0013, decimal vatRate = 20m, decimal? vatAmount = null, string description = "Article", decimal quantity = 1m) => new()
    {
        Description = description,
        Amount = amount,
        Quantity = quantity,
        VATRate = vatRate,
        VATAmount = vatAmount ?? Math.Round(amount / (100 + vatRate) * vatRate, 2),
        Currency = Currency.EUR,
        ftChargeItemCase = (ChargeItemCase) (0x4652_2000_0000_0000 | chargeItemCase),
    };

    private static PayItem Cash(decimal amount) => new() { Amount = amount, Description = "Especes", Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") };

    private static ReceiptRequest Receipt(long receiptCase, List<ChargeItem> charge, List<PayItem> pay) => new()
    {
        ftReceiptCase = (ReceiptCase) (0x4652_2000_0000_0000 | receiptCase),
        Currency = Currency.EUR,
        cbReceiptReference = "R-1",
        cbPreviousReceiptReference = "receipt-0",
        cbChargeItems = charge,
        cbPayItems = pay,
    };

    // ADR 0016: the exemptions are French.

    [Fact]
    public async Task AFinalInvoice_DeductsTheDownPaymentAsANegativeLine()
    {
        var request = Receipt(0x1001, [Item(3000m), Item(-1200m, 0x0008_0013, quantity: -1m, description: "Acompte recu")], [Cash(1800m)]);

        (await ErrorsAsync(request)).Should().NotContain(["NegativeAmountNotAllowed", "NegativeQuantityNotAllowed"]);
    }

    [Theory]
    [InlineData(0x0002, 0x00A8)]
    [InlineData(0x0002, 0x0098)]
    [InlineData(0x0002, 0x0048)]
    [InlineData(0x3005, 0x00A8)]
    public async Task MoneyLeavingTheDrawer_IsANegativeDrawerMoveItem(long receiptCase, long chargeItemCase)
    {
        var request = Receipt(receiptCase, [Item(-500m, chargeItemCase, vatRate: 0m, vatAmount: 0m)], [Cash(-500m)]);

        (await ErrorsAsync(request)).Should().NotContain("NegativeAmountNotAllowed");
    }

    [Theory]
    [InlineData(0x0001, 0x00A8, "a cash transfer on a sale")]
    [InlineData(0x0001, 0x0048, "a voucher redemption on a sale is not decided here")]
    [InlineData(0x0001, 0x0013, "a negative article")]
    public async Task OtherNegativeItems_AreStillRefused(long receiptCase, long chargeItemCase, string because)
    {
        var request = Receipt(receiptCase, [Item(100m), Item(-50m, chargeItemCase, vatRate: 0m, vatAmount: 0m)], [Cash(50m)]);

        (await ErrorsAsync(request)).Should().Contain("NegativeAmountNotAllowed", because);
    }

    [Fact]
    public async Task ANegativeLine_IsReportedOnce()
    {
        var request = Receipt(0x0001, [Item(100m), Item(-50m)], [Cash(50m)]);

        (await ErrorsAsync(request)).Count(x => x == "NegativeAmountNotAllowed").Should().Be(1, "the shared instance's code is exempt, the FR instance reports it");
    }

    // ADR 0016: a handwritten receipt keeps the checks.

    [Fact]
    public async Task AnUnbalancedHandwrittenReceipt_IsRefused()
        => (await ErrorsAsync(Receipt(HandWritten | 0x0001, [Item(12m)], [Cash(10m)]))).Should().Contain("ReceiptNotBalanced");

    [Fact]
    public async Task AHandwrittenReceiptWithAWrongVatAmount_IsRefused()
        => (await ErrorsAsync(Receipt(HandWritten | 0x0001, [Item(12m, vatAmount: 5m)], [Cash(12m)]))).Should().Contain("VatAmountMismatch");

    [Fact]
    public async Task AHandwrittenReceiptWithAnEmptyDescription_IsRefused()
    {
        var result = await Validator().ValidateAsync(Receipt(HandWritten | 0x0001, [Item(12m, description: " ")], [Cash(12m)]), Queue);

        result.Errors.Should().Contain(x => x.PropertyName == "cbChargeItems[0].Description");
    }

    [Fact]
    public async Task AHandwrittenZeroRateItemWithoutNatureOfVat_IsRefused()
        => (await ErrorsAsync(Receipt(HandWritten | 0x0001, [Item(12m, 0x0011, vatRate: 0m, vatAmount: 0m)], [Cash(12m)]))).Should().Contain("ZeroVatRateMissingNature");

    [Fact]
    public async Task ACorrectHandwrittenReceipt_IsTaken()
        => (await ErrorsAsync(Receipt(HandWritten | 0x0001, [Item(12m)], [Cash(12m)]))).Should().BeEmpty();

    [Fact]
    public async Task AHandwrittenTableCheck_FollowsTheSharedBalanceExemption()
        => (await ErrorsAsync(Receipt(HandWritten | 0x0006, [Item(12m)], []))).Should().NotContain("ReceiptNotBalanced");

    [Fact]
    public async Task AnUnbalancedTypedReceipt_IsReportedOnce()
        => (await ErrorsAsync(Receipt(0x0001, [Item(12m)], [Cash(10m)]))).Count(x => x == "ReceiptNotBalanced").Should().Be(1);
}
