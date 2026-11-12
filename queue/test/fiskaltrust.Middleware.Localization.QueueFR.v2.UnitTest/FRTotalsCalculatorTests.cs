using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// The bucket of every v2 VAT class and pay case, with QueueFR v1.2 (reached through the PosSystem
/// API v2→v0 translation) as the oracle, except card, crossed cheque, voucher and Ticket Restaurant, which are
/// non-cash by decision (market-fr ADR). The InfoCert SCU runs the same table
/// (InfoCertTotalsTests), so the period totals and the signed receipt totals cannot drift.
/// </summary>
public class FRTotalsCalculatorTests
{
    public static TheoryData<long, decimal, string> VatCases => new()
    {
        // VAT class 0: v1.2 derives the class from the rate.
        { 0x4652_2000_0000_0000, 5.5m, nameof(FRTotals.CIReduced1) },
        { 0x4652_2000_0000_0000, 10m, nameof(FRTotals.CIReduced2) },
        { 0x4652_2000_0000_0000, 20m, nameof(FRTotals.CINormal) },
        { 0x4652_2000_0000_0000, 0m, nameof(FRTotals.CIZero) },
        { 0x4652_2000_0000_0000, 2.1m, nameof(FRTotals.CIReducedS) },
        { 0x4652_2000_0000_0001, 5.5m, nameof(FRTotals.CIReduced1) },
        { 0x4652_2000_0000_0002, 10m, nameof(FRTotals.CIReduced2) },
        { 0x4652_2000_0000_0003, 20m, nameof(FRTotals.CINormal) },
        { 0x4652_2000_0000_0004, 2.1m, nameof(FRTotals.CIReducedS) },
        // Super-reduced 2, parking and not taxable reach v0 "zero" in v1.2.
        { 0x4652_2000_0000_0005, 0m, nameof(FRTotals.CIZero) },
        { 0x4652_2000_0000_0006, 0m, nameof(FRTotals.CIZero) },
        { 0x4652_2000_0000_0007, 0m, nameof(FRTotals.CIZero) },
        { 0x4652_2000_0000_0008, 0m, nameof(FRTotals.CIZero) },
        // The rate of a known class never moves it (no v1.2 reverse-charge quirk).
        { 0x4652_2000_0000_0007, 20m, nameof(FRTotals.CIZero) },
        // A single-purpose voucher (type of service 4) is taxed when sold: in its VAT bucket and the Totalizer.
        { 0x4652_2000_0000_0043, 20m, nameof(FRTotals.CINormal) },
        // No such VAT class.
        { 0x4652_2000_0000_000F, 0m, nameof(FRTotals.CIUnknown) },
    };

    public static TheoryData<long, string> PayCases => new()
    {
        { 0x4652_2000_0000_0000, nameof(FRTotals.PICash) },
        { 0x4652_2000_0000_0001, nameof(FRTotals.PICash) },
        { 0x4652_2000_0000_0002, nameof(FRTotals.PINonCash) },
        { 0x4652_2000_0000_0003, nameof(FRTotals.PINonCash) },
        { 0x4652_2000_0000_0004, nameof(FRTotals.PINonCash) },
        { 0x4652_2000_0000_0005, nameof(FRTotals.PINonCash) },
        { 0x4652_2000_0000_0006, nameof(FRTotals.PINonCash) },
        { 0x4652_2000_0000_0007, nameof(FRTotals.PINonCash) },
        { 0x4652_2000_0000_0008, nameof(FRTotals.PINonCash) },
        { 0x4652_2000_0000_0009, nameof(FRTotals.PIInternal) },
        { 0x4652_2000_0000_000A, nameof(FRTotals.PINonCash) },
        { 0x4652_2000_0000_000B, nameof(FRTotals.PINonCash) },
        { 0x4652_2000_0000_000C, nameof(FRTotals.PIInternal) },
        { 0x4652_2000_0000_000D, nameof(FRTotals.PIInternal) },
        { 0x4652_2000_0000_000E, nameof(FRTotals.PIInternal) },
        { 0x4652_2000_0000_000F, nameof(FRTotals.PINonCash) },
        // Tip: v1.2 compares the whole case, so a tipped item matches no bucket.
        { 0x4652_2000_0040_0001, nameof(FRTotals.PIUnknown) },
        { 0x4652_2000_0000_00FF, nameof(FRTotals.PIUnknown) },
    };

    // NF525 V2.3 §6.18.1.3, market-fr#221: a multipurpose voucher sold (S=4, not taxable) is a treasury operation.
    [Fact]
    public void AMultipurposeVoucherSold_StaysOutOfTheTurnover()
    {
        var totals = FRTotalsCalculator.From(new ReceiptRequest
        {
            cbChargeItems =
            [
                new ChargeItem { Amount = 50m, VATRate = 0m, ftChargeItemCase = (ChargeItemCase) 0x4652_2000_0000_0048 },
                new ChargeItem { Amount = 10m, VATRate = 20m, ftChargeItemCase = (ChargeItemCase) 0x4652_2000_0000_0003 },
            ],
            cbPayItems = [],
        });

        totals.Totalizer.Should().Be(10m);
        totals.CIZero.Should().Be(0m);
        totals.CINormal.Should().Be(10m);
    }

    [Theory]
    [InlineData(0x4652_2000_0000_0048, true)]
    [InlineData(0x4652_2000_0000_0043, false)]
    [InlineData(0x4652_2000_0000_0008, false)]
    [InlineData(0x4652_2000_0000_0098, false)]
    public void MultipurposeVouchers_AreTheNotTaxableVouchers(long chargeItemCase, bool multipurpose)
        => FRTotalsCalculator.IsMultipurposeVoucher(new ChargeItem { ftChargeItemCase = (ChargeItemCase) chargeItemCase }).Should().Be(multipurpose);

    [Theory]
    [MemberData(nameof(VatCases))]
    public void ChargeItem_LandsInItsV12Bucket(long chargeItemCase, decimal vatRate, string bucket)
    {
        var totals = FRTotalsCalculator.From(new ReceiptRequest
        {
            cbChargeItems = [new ChargeItem { Amount = 10m, VATRate = vatRate, ftChargeItemCase = (ChargeItemCase) chargeItemCase }],
            cbPayItems = [],
        });

        Buckets(totals).Should().BeEquivalentTo(Expected(bucket, 10m));
        totals.Totalizer.Should().Be(10m);
    }

    [Theory]
    [MemberData(nameof(PayCases))]
    public void PayItem_LandsInItsBucket(long payItemCase, string bucket)
    {
        var totals = FRTotalsCalculator.From(new ReceiptRequest
        {
            cbChargeItems = [],
            cbPayItems = [new PayItem { Amount = -20m, ftPayItemCase = (PayItemCase) payItemCase }],
        });

        Buckets(totals).Should().BeEquivalentTo(Expected(bucket, -20m));
    }

    private static Dictionary<string, decimal> Buckets(FRTotals t) => new()
    {
        [nameof(FRTotals.CINormal)] = t.CINormal,
        [nameof(FRTotals.CIReduced1)] = t.CIReduced1,
        [nameof(FRTotals.CIReduced2)] = t.CIReduced2,
        [nameof(FRTotals.CIReducedS)] = t.CIReducedS,
        [nameof(FRTotals.CIZero)] = t.CIZero,
        [nameof(FRTotals.CIUnknown)] = t.CIUnknown,
        [nameof(FRTotals.PICash)] = t.PICash,
        [nameof(FRTotals.PINonCash)] = t.PINonCash,
        [nameof(FRTotals.PIInternal)] = t.PIInternal,
        [nameof(FRTotals.PIUnknown)] = t.PIUnknown,
    };

    private static Dictionary<string, decimal> Expected(string bucket, decimal amount)
    {
        var expected = Buckets(new FRTotals());
        expected[bucket] = amount;
        return expected;
    }
}
