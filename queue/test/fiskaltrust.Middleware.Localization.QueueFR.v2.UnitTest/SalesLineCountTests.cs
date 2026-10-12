using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
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
        FRSalesLineCounter.Count(Request()).Should().Be(0);
        FRSalesLineCounter.Count(new ReceiptRequest { cbChargeItems = null! }).Should().Be(0);
    }

    [Fact]
    public void PlainSalesLines_AreCounted()
        => FRSalesLineCounter.Count(Request(Item(), Item(), Item(5m))).Should().Be(3);

    [Theory]
    [InlineData(ChargeItemCaseFlags.ExtraOrDiscount, -2)]
    [InlineData(ChargeItemCaseFlags.ExtraOrDiscount, 2)]
    [InlineData(ChargeItemCaseFlags.Downpayment, 10)]
    [InlineData(ChargeItemCaseFlags.ReturnableDeposit, 0.5)]
    public void DiscountDownpaymentAndDepositLines_AreNotCounted(ChargeItemCaseFlags flag, double amount)
        => FRSalesLineCounter.Count(Request(Item(), Item((decimal) amount, flag), Item())).Should().Be(2);

    [Theory]
    [InlineData(ChargeItemCaseFlags.Refund)]
    [InlineData(ChargeItemCaseFlags.Void)]
    public void RefundedAndVoidedLines_AreCounted(ChargeItemCaseFlags flag)
        => FRSalesLineCounter.Count(Request(Item(-10m, flag), Item(-5m, flag))).Should().Be(2);

    [Fact]
    public void RefundedDiscount_IsNotCounted()
        => FRSalesLineCounter.Count(Request(Item(-10m, ChargeItemCaseFlags.Refund), Item(1m, ChargeItemCaseFlags.Refund, ChargeItemCaseFlags.ExtraOrDiscount))).Should().Be(1);
}
