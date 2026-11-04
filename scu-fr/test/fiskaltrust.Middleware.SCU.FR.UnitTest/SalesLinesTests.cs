using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest;

/// <summary>The sales-line count the SCUs sign and the queue prints (R13 ENC-TIK-LIG-NBR, market-fr#148).</summary>
public class SalesLinesTests
{
    private static ChargeItem Line(ChargeItemCaseFlags? flag = null)
    {
        var chargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR");
        return new ChargeItem { Amount = 1m, ftChargeItemCase = flag is null ? chargeItemCase : chargeItemCase.WithFlag(flag.Value) };
    }

    [Fact]
    public void Count_LeavesOutDiscountsDownPaymentsAndDeposits()
    {
        var request = new ReceiptRequest
        {
            cbChargeItems = [Line(), Line(ChargeItemCaseFlags.Refund), Line(ChargeItemCaseFlags.ExtraOrDiscount), Line(ChargeItemCaseFlags.Downpayment), Line(ChargeItemCaseFlags.ReturnableDeposit)],
        };

        FRSalesLines.Count(request).Should().Be(2, "a refunded line is still a line of the document");
    }

    [Fact]
    public void Count_IsZeroWithoutChargeItems() => FRSalesLines.Count(new ReceiptRequest()).Should().Be(0);
}
