using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// P12c (market-fr#221): the grand totals are kept per VAT rate as well as per VAT class, so 2.1 % is no longer merged
/// into ReducedS. The per-rate amounts live in ftQueueFR StateV2 next to the v1 class columns.
/// </summary>
public class RateTotalsTests
{
    private static ChargeItem Item(decimal amount, decimal rate, ChargeItemCase vat, ChargeItemCaseFlags? flag = null)
    {
        var chargeItemCase = vat.WithCountry("FR");
        return new ChargeItem { Amount = amount, VATRate = rate, Quantity = 1, Currency = Currency.EUR, ftChargeItemCase = flag is null ? chargeItemCase : chargeItemCase.WithFlag(flag.Value) };
    }

    private static ReceiptRequest Sale(params ChargeItem[] items) => new()
    {
        ftCashBoxID = Guid.NewGuid(),
        ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("FR"),
        Currency = Currency.EUR,
        cbChargeItems = items.ToList(),
        cbPayItems = [],
    };

    [Fact]
    public void Calculator_KeepsTwoPointOnePercentApartFromReducedS()
    {
        var totals = FRTotalsCalculator.From(Sale(
            Item(10.00m, 20m, ChargeItemCase.NormalVatRate),
            Item(2.10m, 2.1m, ChargeItemCase.SuperReducedVatRate1),
            Item(8.50m, 8.5m, ChargeItemCase.UnknownService),
            Item(-5.00m, 20m, ChargeItemCase.NormalVatRate, ChargeItemCaseFlags.Downpayment)));

        totals.CIReducedS.Should().Be(10.60m, "the class still merges 2.1 % with the other super-reduced rates");
        totals.ByRate.Should().BeEquivalentTo(new Dictionary<string, decimal> { ["20"] = 10.00m, ["2.1"] = 2.10m, ["8.5"] = 8.50m },
            "down payments stay out, as in every total");
    }

    [Fact]
    public void Accumulator_KeepsThePerRateTotalsOfEveryPeriod_InStateV2()
    {
        var queueFR = new ftQueueFR();
        var accumulator = new FRPeriodTotalsAccumulator(queueFR);

        accumulator.Add(Sale(Item(10.00m, 20m, ChargeItemCase.NormalVatRate), Item(2.10m, 2.1m, ChargeItemCase.SuperReducedVatRate1)));
        accumulator.Add(Sale(Item(1.05m, 2.1m, ChargeItemCase.SuperReducedVatRate1)));

        var snapshot = new FRPeriodTotalsAccumulator(queueFR).Snapshot(FRTotalsPeriod.Month, [FRTotalsPeriod.Day, FRTotalsPeriod.Month]);
        snapshot.Current.ByRate.Should().BeEquivalentTo(new Dictionary<string, decimal> { ["20"] = 10.00m, ["2.1"] = 3.15m });
        snapshot.Closed.Single(x => x.Period == FRTotalsPeriod.Day).Totals.ByRate["2.1"].Should().Be(3.15m);
        snapshot.Perpetual.ByRate["2.1"].Should().Be(3.15m, "the perpetual total adds the ticket and invoice blocks, rates included");
        FRStateV2.Read(queueFR).RateTotals!["GDay"]["2.1"].Should().Be(3.15m);
    }

    [Fact]
    public void Reset_EmptiesThePerRateTotalsOfThePeriodOnly()
    {
        var queueFR = new ftQueueFR();
        var accumulator = new FRPeriodTotalsAccumulator(queueFR);
        accumulator.Add(Sale(Item(10.00m, 20m, ChargeItemCase.NormalVatRate)));

        accumulator.Reset(FRTotalsPeriod.Day);

        accumulator.Snapshot(FRTotalsPeriod.Day).Current.ByRate.Should().BeEmpty();
        accumulator.Snapshot(FRTotalsPeriod.Month).Current.ByRate["20"].Should().Be(10.00m);
    }

    [Fact]
    public void ABlockV2NeverWrote_TakesItsPerRateTotalsFromTheV1Classes()
    {
        // A queue migrated from v1.2: the class columns carry the v1 totals, StateV2 has no per-rate amounts yet.
        var queueFR = new ftQueueFR
        {
            GMonthTotalizer = 136.10m,
            GMonthCITotalNormal = 100.00m,
            GMonthCITotalReduced1 = 11.00m,
            GMonthCITotalReducedS = 2.10m,
            GMonthCITotalUnknown = 23.00m,
        };
        var accumulator = new FRPeriodTotalsAccumulator(queueFR);

        accumulator.Snapshot(FRTotalsPeriod.Month).Current.ByRate.Should().BeEquivalentTo(new Dictionary<string, decimal>
        {
            ["20"] = 100.00m, ["5.5"] = 11.00m, ["2.1"] = 2.10m, [FRTotals.UnknownRateKey] = 23.00m,
        });

        accumulator.Add(Sale(Item(2.10m, 2.1m, ChargeItemCase.SuperReducedVatRate1)));

        accumulator.Snapshot(FRTotalsPeriod.Month).Current.ByRate["2.1"].Should().Be(4.20m, "new sales add to the seeded amounts");
    }
}
