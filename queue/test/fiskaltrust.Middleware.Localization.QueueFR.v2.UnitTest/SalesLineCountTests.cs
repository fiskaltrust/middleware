using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

public class SalesLineCountTests
{
    private static ChargeItem Item(decimal amount = 10m, params ChargeItemCaseFlags[] flags)
    {
        var chargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery);
        foreach (var flag in flags)
        {
            chargeItemCase = chargeItemCase.WithFlag(flag);
        }
        return new() { Amount = amount, Currency = Currency.EUR, ftChargeItemCase = chargeItemCase };
    }

    private static ReceiptRequest Request(params ChargeItem[] chargeItems)
        => new() { ftCashBoxID = Guid.NewGuid(), ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("FR"), Currency = Currency.EUR, cbChargeItems = chargeItems.ToList() };

    [Fact]
    public void NoChargeItems_IsZero()
    {
        FRSalesLines.Count(Request()).Should().Be(0);
        FRSalesLines.Count(new ReceiptRequest { cbChargeItems = null! }).Should().Be(0);
    }

    [Fact]
    public void PlainSalesLines_AreCounted()
        => FRSalesLines.Count(Request(Item(), Item(), Item(5m))).Should().Be(3);

    [Theory]
    [InlineData(ChargeItemCaseFlags.ExtraOrDiscount, -2)]
    [InlineData(ChargeItemCaseFlags.ExtraOrDiscount, 2)]
    [InlineData(ChargeItemCaseFlags.Downpayment, 10)]
    [InlineData(ChargeItemCaseFlags.ReturnableDeposit, 0.5)]
    public void DiscountDownpaymentAndDepositLines_AreNotCounted(ChargeItemCaseFlags flag, double amount)
        => FRSalesLines.Count(Request(Item(), Item((decimal) amount, flag), Item())).Should().Be(2);

    [Theory]
    [InlineData(ChargeItemCaseFlags.Refund)]
    [InlineData(ChargeItemCaseFlags.Void)]
    public void RefundedAndVoidedLines_AreCounted(ChargeItemCaseFlags flag)
        => FRSalesLines.Count(Request(Item(-10m, flag), Item(-5m, flag))).Should().Be(2);

    [Fact]
    public void RefundedDiscount_IsNotCounted()
        => FRSalesLines.Count(Request(Item(-10m, ChargeItemCaseFlags.Refund), Item(1m, ChargeItemCaseFlags.Refund, ChargeItemCaseFlags.ExtraOrDiscount))).Should().Be(1);

    // market-fr#221, R13 note 7: with "group by position number" (0x0800_0000) a composite article counts as one line.

    private static ChargeItem At(decimal position, params ChargeItemCaseFlags[] flags)
    {
        var item = Item(10m, flags);
        item.Position = position;
        return item;
    }

    private static ReceiptRequest Grouped(long receiptCase, params ChargeItem[] chargeItems)
    {
        var request = Request(chargeItems);
        request.ftReceiptCase = (ReceiptCase) ((long) ((ReceiptCase) receiptCase).WithCountry("FR") | FRSalesLines.GroupByPositionNumber);
        return request;
    }

    [Fact]
    public void WithGrouping_APositionGroupCountsOnce()
        => FRSalesLines.Count(Grouped(0x0001, At(1), At(1.1m), At(1.2m), At(2), At(3.5m))).Should().Be(3);

    [Fact]
    public void WithGrouping_EachSalesLineAtPositionZeroCountsAlone()
        => FRSalesLines.Count(Grouped(0x0001, At(0), At(0), At(1), At(1.1m))).Should().Be(3);

    [Fact]
    public void WithGrouping_AGroupOfExcludedItemsOnlyDoesNotCount()
        => FRSalesLines.Count(Grouped(0x0001, At(1), At(2, ChargeItemCaseFlags.ExtraOrDiscount), At(2.1m, ChargeItemCaseFlags.Downpayment))).Should().Be(1);

    [Fact]
    public void WithGrouping_DuplicatePositionsAreNotRefused()
        => FRSalesLines.Count(Grouped(0x0001, At(1), At(1), At(1))).Should().Be(1);

    [Fact]
    public void WithoutGrouping_EverySalesLineCounts()
        => FRSalesLines.Count(Request(At(1), At(1.1m), At(1.2m))).Should().Be(3);

    [Fact]
    public void OnAClosingOrAZeroReceipt_TheBitIsNotRead()
    {
        FRSalesLines.Count(Grouped(0x2000, At(1), At(1.1m))).Should().Be(2);
        FRSalesLines.Count(Grouped(0x2013, At(1), At(1.1m))).Should().Be(2);
    }

    [Fact]
    public void TheArchive_MarksTheLineThatCountsEachGroup_AndAgreesWithTheCount()
    {
        var request = Grouped(0x0001, At(1, ChargeItemCaseFlags.ExtraOrDiscount), At(1.1m), At(1.2m), At(0), At(2));

        var lines = FRArchiveStateData.ChargeItemsOf(request.cbChargeItems, FRSalesLines.CountingLines(request));

        lines.Select(x => x.CountsAsLine).Should().Equal(false, true, false, true, true);
        lines.Count(x => x.CountsAsLine).Should().Be(FRSalesLines.Count(request));
    }
}
