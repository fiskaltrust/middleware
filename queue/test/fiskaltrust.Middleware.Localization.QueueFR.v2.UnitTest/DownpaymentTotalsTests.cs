using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// A down payment is not turnover until the sale is invoiced, so it must move no total: neither
/// when the down-payment invoice records it nor when the final invoice settles it
/// (fiskaltrust/market-fr#135).
/// </summary>
public class DownpaymentTotalsTests
{
    private static ChargeItemCase Charge(ChargeItemCase vat, bool downpayment = false)
    {
        var value = (long) vat.WithCountry("FR");
        return (ChargeItemCase) (downpayment ? value | (long) ChargeItemCaseFlags.Downpayment : value);
    }

    private static PayItemCase Pay(PayItemCase pay, bool downpayment = false)
    {
        var value = (long) pay.WithCountry("FR");
        return (PayItemCase) (downpayment ? value | (long) PayItemCaseFlags.Downpayment : value);
    }

    private static ReceiptRequest Request(List<ChargeItem> chargeItems, List<PayItem> payItems) => new()
    {
        ftCashBoxID = Guid.NewGuid(),
        ftReceiptCase = ReceiptCase.InvoiceB2C0x1001.WithCountry("FR"),
        Currency = Currency.EUR,
        cbChargeItems = chargeItems,
        cbPayItems = payItems,
    };

    [Fact]
    public void DownpaymentInvoice_MovesNoTotal()
    {
        var request = Request(
            [new ChargeItem { Amount = 300m, Currency = Currency.EUR, ftChargeItemCase = Charge(ChargeItemCase.NormalVatRate, downpayment: true) }],
            [new PayItem { Amount = 300m, Currency = Currency.EUR, ftPayItemCase = Pay(PayItemCase.CreditCardPayment, downpayment: true) }]);

        var totals = FRTotalsCalculator.From(request);

        totals.Should().BeEquivalentTo(new FRTotalsSnapshot());
    }

    [Fact]
    public void FinalInvoice_CountsOnlyTheSaleNotTheSettledDownpayment()
    {
        var request = Request(
            [
                new ChargeItem { Amount = 1000m, Currency = Currency.EUR, ftChargeItemCase = Charge(ChargeItemCase.NormalVatRate) },
                new ChargeItem { Amount = -300m, Currency = Currency.EUR, ftChargeItemCase = Charge(ChargeItemCase.NormalVatRate, downpayment: true) },
            ],
            [
                new PayItem { Amount = 700m, Currency = Currency.EUR, ftPayItemCase = Pay(PayItemCase.CashPayment) },
                new PayItem { Amount = 300m, Currency = Currency.EUR, ftPayItemCase = Pay(PayItemCase.CreditCardPayment, downpayment: true) },
            ]);

        var totals = FRTotalsCalculator.From(request);

        totals.Totalizer.Should().Be(1000m);
        totals.CINormal.Should().Be(1000m);
        totals.CIUnknown.Should().Be(0m);
        totals.PICash.Should().Be(700m);
        totals.PINonCash.Should().Be(0m);
        totals.PIUnknown.Should().Be(0m);
    }

    [Fact]
    public void DownpaymentLifecycle_AddsUpToTheSaleOnly()
    {
        var downpaymentInvoice = Request(
            [new ChargeItem { Amount = 300m, Currency = Currency.EUR, ftChargeItemCase = Charge(ChargeItemCase.DiscountedVatRate1, downpayment: true) }],
            [new PayItem { Amount = 300m, Currency = Currency.EUR, ftPayItemCase = Pay(PayItemCase.CashPayment, downpayment: true) }]);
        var finalInvoice = Request(
            [
                new ChargeItem { Amount = 1000m, Currency = Currency.EUR, ftChargeItemCase = Charge(ChargeItemCase.DiscountedVatRate1) },
                new ChargeItem { Amount = -300m, Currency = Currency.EUR, ftChargeItemCase = Charge(ChargeItemCase.DiscountedVatRate1, downpayment: true) },
            ],
            [
                new PayItem { Amount = 700m, Currency = Currency.EUR, ftPayItemCase = Pay(PayItemCase.CashPayment) },
                new PayItem { Amount = 300m, Currency = Currency.EUR, ftPayItemCase = Pay(PayItemCase.CashPayment, downpayment: true) },
            ]);

        var totals = FRTotalsCalculator.From(downpaymentInvoice);
        totals.Add(FRTotalsCalculator.From(finalInvoice));

        totals.Totalizer.Should().Be(1000m);
        totals.CIReduced1.Should().Be(1000m);
        // Charge and pay sides stay balanced: only the final invoice's own settlement counts.
        totals.PICash.Should().Be(700m);
    }

    [Fact]
    public void ItemsWithoutTheFlag_AreUnchanged()
    {
        var request = Request(
            [
                new ChargeItem { Amount = 120m, Currency = Currency.EUR, ftChargeItemCase = Charge(ChargeItemCase.NormalVatRate) },
                new ChargeItem { Amount = 11m, Currency = Currency.EUR, ftChargeItemCase = Charge(ChargeItemCase.DiscountedVatRate2) },
            ],
            [new PayItem { Amount = 131m, Currency = Currency.EUR, ftPayItemCase = Pay(PayItemCase.DebitCardPayment) }]);

        var totals = FRTotalsCalculator.From(request);

        totals.Totalizer.Should().Be(131m);
        totals.CINormal.Should().Be(120m);
        totals.CIReduced2.Should().Be(11m);
        totals.PINonCash.Should().Be(131m);
    }

    /// <summary>All-zero expectation for <see cref="FRTotalsCalculator.From"/>.</summary>
    private sealed class FRTotalsSnapshot
    {
        public decimal Totalizer { get; init; }
        public decimal CINormal { get; init; }
        public decimal CIReduced1 { get; init; }
        public decimal CIReduced2 { get; init; }
        public decimal CIReducedS { get; init; }
        public decimal CIZero { get; init; }
        public decimal CIUnknown { get; init; }
        public decimal PICash { get; init; }
        public decimal PINonCash { get; init; }
        public decimal PIInternal { get; init; }
        public decimal PIUnknown { get; init; }
    }
}
