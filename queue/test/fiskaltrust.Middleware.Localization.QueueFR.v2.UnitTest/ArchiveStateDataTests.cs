using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// The queue stores, when it signs, the values the fiscal archive reads back as columns: the receipt or period
/// totals, the HT and VAT of each line and the VAT summary per rate (archive-v2 §1.2, §1.3, §2, §4, market-fr#221).
/// </summary>
public class ArchiveStateDataTests
{
    private static ChargeItemCase Charge(ChargeItemCase vat, ChargeItemCaseFlags? flag = null)
    {
        var value = (long) vat.WithCountry("FR");
        return (ChargeItemCase) (flag is null ? value : value | (long) flag.Value);
    }

    private static ReceiptRequest Ticket(params ChargeItem[] items) => new()
    {
        ftCashBoxID = Guid.NewGuid(),
        ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("FR"),
        Currency = Currency.EUR,
        cbChargeItems = items.ToList(),
        cbPayItems = [new PayItem { Amount = items.Sum(i => i.Amount), Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") }],
    };

    private static MiddlewareStateDataFR Apply(ReceiptRequest request, FRReceiptChain chain, FRPeriodTotals? periodTotals = null)
    {
        var response = new ReceiptResponse { ftState = State.Success.WithCountry("FR") };
        FRArchiveStateData.Apply(request, response, chain, periodTotals);
        // Read back as the archive does: through the stored JSON.
        var json = JsonSerializer.Serialize(response.ftStateData, response.ftStateData!.GetType());
        return JsonSerializer.Deserialize<MiddlewareStateData>(json)!.FR!;
    }

    [Fact]
    public void Lines_KeepTheSentVat_OrComputeItFromTheRate()
    {
        var state = Apply(Ticket(
            new ChargeItem { Position = 1, Quantity = 2, Amount = 4.40m, VATRate = 10, VATAmount = 0.40m, ftChargeItemCase = Charge(ChargeItemCase.DiscountedVatRate2), Currency = Currency.EUR },
            new ChargeItem { Position = 2, Quantity = 3, Amount = 10m, VATRate = 20, ftChargeItemCase = Charge(ChargeItemCase.NormalVatRate), Currency = Currency.EUR }), FRReceiptChain.Ticket);

        state.ChargeItems.Should().HaveCount(2);
        state.ChargeItems![0].Should().BeEquivalentTo(new { Position = 1m, VATAmount = 0.40m, AmountHT = 4.00m, UnitPriceHT = 2.00m, VATNature = "00" });
        state.ChargeItems[1].VATAmount.Should().Be(1.67m, "10 x 20 / 120 = 1.666…, rounded to the cent away from zero");
        state.ChargeItems[1].AmountHT.Should().Be(8.33m);
        state.ChargeItems[1].UnitPriceHT.Should().Be(2.78m);
    }

    [Fact]
    public void Discounts_ApplyToTheNearestPrecedingSalesLine_OrToTheNamedOne()
    {
        var named = new ChargeItem
        {
            Position = 4, Quantity = 1, Amount = -1m, VATRate = 20, Currency = Currency.EUR,
            ftChargeItemCase = Charge(ChargeItemCase.NormalVatRate, ChargeItemCaseFlags.ExtraOrDiscount),
            ftChargeItemCaseData = JsonSerializer.Deserialize<JsonElement>("{\"FR\":{\"AppliesTo\":1}}"),
        };
        var state = Apply(Ticket(
            new ChargeItem { Position = 1, Quantity = 1, Amount = 10m, VATRate = 20, ftChargeItemCase = Charge(ChargeItemCase.NormalVatRate), Currency = Currency.EUR },
            new ChargeItem { Position = 2, Quantity = 1, Amount = 5m, VATRate = 20, ftChargeItemCase = Charge(ChargeItemCase.NormalVatRate), Currency = Currency.EUR },
            new ChargeItem { Position = 3, Quantity = 1, Amount = -0.50m, VATRate = 20, ftChargeItemCase = Charge(ChargeItemCase.NormalVatRate, ChargeItemCaseFlags.ExtraOrDiscount), Currency = Currency.EUR },
            named), FRReceiptChain.Ticket);

        state.ChargeItems![2].AppliesTo.Should().Be(2m, "the nearest preceding sales line");
        state.ChargeItems[3].AppliesTo.Should().Be(1m, "FR.AppliesTo overrides it");
        state.ChargeItems[0].DiscountAmount.Should().Be(-1m);
        state.ChargeItems[1].DiscountAmount.Should().Be(-0.50m);
    }

    [Fact]
    public void VatSummary_GroupsEveryLineByRate()
    {
        var state = Apply(Ticket(
            new ChargeItem { Position = 1, Quantity = 1, Amount = 12m, VATRate = 20, VATAmount = 2m, ftChargeItemCase = Charge(ChargeItemCase.NormalVatRate), Currency = Currency.EUR },
            new ChargeItem { Position = 2, Quantity = 1, Amount = 6m, VATRate = 20, VATAmount = 1m, ftChargeItemCase = Charge(ChargeItemCase.NormalVatRate), Currency = Currency.EUR },
            new ChargeItem { Position = 3, Quantity = 1, Amount = 5.275m, VATRate = 5.5m, ftChargeItemCase = Charge(ChargeItemCase.DiscountedVatRate1), Currency = Currency.EUR }), FRReceiptChain.Ticket);

        state.VATSummary.Should().ContainKeys("20", "5.5");
        state.VATSummary!["20"].Should().BeEquivalentTo(new { AmountTTC = 18m, VATAmount = 3m, AmountHT = 15m, VATNature = "00" });
        state.VATSummary["5.5"].AmountHT.Should().Be(state.VATSummary["5.5"].AmountTTC - state.VATSummary["5.5"].VATAmount);
    }

    [Fact]
    public void SalesReceipt_StoresItsOwnTotals_WithoutPerpetual()
    {
        var state = Apply(Ticket(new ChargeItem { Position = 1, Quantity = 1, Amount = 10m, VATRate = 20, ftChargeItemCase = Charge(ChargeItemCase.NormalVatRate), Currency = Currency.EUR }), FRReceiptChain.Ticket);

        state.Totals!.Totalizer.Should().Be(10m);
        state.Totals.CINormal.Should().Be(10m);
        state.Totals.PICash.Should().Be(10m);
        state.Totals.Perpetual.Should().BeNull();
    }

    [Fact]
    public void Closing_StoresThePeriodAndPerpetualTotals()
    {
        var request = new ReceiptRequest { ftCashBoxID = Guid.NewGuid(), ftReceiptCase = ReceiptCase.DailyClosing0x2011.WithCountry("FR"), Currency = Currency.EUR, cbChargeItems = [], cbPayItems = [] };
        var period = new FRPeriodTotals
        {
            Period = FRTotalsPeriod.Day,
            Current = new FRTotals { Totalizer = 30m, CIReduced1 = 30m, PINonCash = 30m },
            Perpetual = new FRTotals { Totalizer = 1000m, CIReduced1 = 400m, CINormal = 600m, PICash = 1000m },
        };

        var state = Apply(request, FRReceiptChain.GrandTotal, period);

        state.Totals!.Totalizer.Should().Be(30m);
        state.Totals.CIDiscounted1.Should().Be(30m);
        state.Totals.PINonCash.Should().Be(30m);
        state.Totals.Perpetual!.Totalizer.Should().Be(1000m);
        state.Totals.Perpetual.CINormal.Should().Be(600m);
        state.ChargeItems.Should().BeNull("a closing has no lines");
    }

    [Fact]
    public void LogEntry_StoresNothing()
    {
        var request = new ReceiptRequest { ftCashBoxID = Guid.NewGuid(), ftReceiptCase = ReceiptCase.ProtocolTechnicalEvent0x3001.WithCountry("FR"), Currency = Currency.EUR, cbChargeItems = [], cbPayItems = [] };
        var response = new ReceiptResponse { ftState = State.Success.WithCountry("FR") };

        FRArchiveStateData.Apply(request, response, FRReceiptChain.Log, null);

        response.ftStateData.Should().BeNull();
    }
}
