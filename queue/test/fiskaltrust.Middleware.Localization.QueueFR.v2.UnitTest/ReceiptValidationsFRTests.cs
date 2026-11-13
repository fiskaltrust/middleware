using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Validation;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// The QueueFR v1.2 data-consistency rules (workerFR CheckDataConsistency), each v2 case mapped to
/// the v0 case the PosSystem API sent v1.2 (market-fr#221).
/// </summary>
public class ReceiptValidationsFRTests
{
    private static ReceiptRequest Request(long receiptCase, decimal charge = 0m, decimal pay = 0m, long payCase = 0x0001) => new()
    {
        ftReceiptCase = (ReceiptCase) (0x4652_2000_0000_0000 | receiptCase),
        Currency = Currency.EUR,
        cbPreviousReceiptReference = "receipt-1",
        cbChargeItems = charge == 0m ? [] : [new ChargeItem { Amount = charge, Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") }],
        cbPayItems = pay == 0m ? [] : [new PayItem { Amount = pay, Currency = Currency.EUR, ftPayItemCase = (PayItemCase) (0x4652_2000_0000_0000 | payCase) }],
    };

    private static IEnumerable<string> Errors(ReceiptRequest request) => new ReceiptValidationsFR().Validate(request).Errors.Select(x => x.ErrorCode);

    [Theory]
    [InlineData(0x2000)]
    [InlineData(0x2001)]
    [InlineData(0x2010)]
    [InlineData(0x2011)]
    [InlineData(0x2012)]
    [InlineData(0x2013)]
    [InlineData(0x3002)]
    [InlineData(0x3010)]
    [InlineData(0x3011)]
    [InlineData(0x4001)]
    [InlineData(0x4002)]
    [InlineData(0x4011)]
    [InlineData(0x4012)]
    public void ReceiptsWithoutASale_RefuseChargeAndPayItems(long receiptCase)
    {
        Errors(Request(receiptCase, charge: 10m)).Should().Contain("ChargeItemsNotAllowed");
        Errors(Request(receiptCase, pay: 10m)).Should().Contain("PayItemsNotAllowed");
        Errors(Request(receiptCase)).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0x0002)]
    [InlineData(0x3005)]
    public void PaymentProofs_RefuseSalesChargeItems_ButTakePayItems(long receiptCase)
    {
        Errors(Request(receiptCase, charge: 10m, pay: 10m)).Should().Contain("ChargeItemsNotAllowed");
        Errors(Request(receiptCase, pay: 200m)).Should().BeEmpty("a float pay-in carries only its pay item");
    }

    private static ReceiptRequest DrawerMove(long receiptCase, long chargeItemCase, decimal amount)
    {
        var request = Request(receiptCase, pay: amount);
        request.cbChargeItems = [new ChargeItem { Amount = amount, Currency = Currency.EUR, ftChargeItemCase = (ChargeItemCase) (0x4652_2000_0000_0000 | chargeItemCase) }];
        return request;
    }

    /// <summary>
    /// A drawer move takes the charge item of the public reference tables (market-fr ADR 0016):
    /// a not-taxable (V=8) cash transfer (S=A), receivable (S=9) or voucher (S=4).
    /// </summary>
    [Theory]
    [InlineData(0x0002, 0x00A8)]
    [InlineData(0x0002, 0x0098)]
    [InlineData(0x0002, 0x0048)]
    [InlineData(0x3005, 0x00A8)]
    [InlineData(0x3005, 0x0098)]
    [InlineData(0x3005, 0x0048)]
    public void PaymentProofs_TakeANotTaxableDrawerMoveChargeItem(long receiptCase, long chargeItemCase)
    {
        Errors(DrawerMove(receiptCase, chargeItemCase, 500m)).Should().BeEmpty();
        Errors(DrawerMove(receiptCase, chargeItemCase, -500m)).Should().BeEmpty("France lets money leave the drawer (ADR 0016)");
    }

    [Theory]
    [InlineData(0x0002, 0x00A3, "a cash transfer must not be taxed")]
    [InlineData(0x0002, 0x0018, "a not-taxable delivery is a sale")]
    [InlineData(0x0002, 0x0028, "a not-taxable service is a sale")]
    [InlineData(0x3005, 0x0038, "a tip is a sale")]
    [InlineData(0x3005, 0x0047, "a voucher must be not taxable")]
    public void PaymentProofs_RefuseAnyOtherChargeItem(long receiptCase, long chargeItemCase, string because)
        => Errors(DrawerMove(receiptCase, chargeItemCase, 500m)).Should().Contain("ChargeItemsNotAllowed", because);

    [Fact]
    public void ReceiptsWithoutASale_RefuseADrawerMoveChargeItemToo()
        => Errors(DrawerMove(0x2011, 0x00A8, 500m)).Should().Contain("ChargeItemsNotAllowed");

    [Theory]
    [InlineData(0x0001)]
    [InlineData(0x0004)]
    [InlineData(0x1001)]
    [InlineData(0x3001)]
    public void SalesAndTechnicalEvents_KeepTheirItems(long receiptCase)
        => Errors(Request(receiptCase, charge: 10m, pay: 10m)).Should().NotContain(["ChargeItemsNotAllowed", "PayItemsNotAllowed"]);

    /// <summary>
    /// Table check, pro forma and order follow the shared v2 rules (market-fr ADR 0016): France adds
    /// neither a balance rule nor a required internal pay item, as v1.2 did for its bill.
    /// </summary>
    [Theory]
    [InlineData(0x0006)]
    [InlineData(0x0007)]
    [InlineData(0x3004)]
    public void Bills_FollowTheSharedRules(long receiptCase)
    {
        Errors(Request(receiptCase, charge: 10m)).Should().BeEmpty("a pro forma or a table check sends no payment");
        Errors(Request(receiptCase, charge: 10m, pay: 9m)).Should().BeEmpty("the shared ReceiptBalance rule decides");
        Errors(Request(receiptCase, charge: 10m, pay: 10m, payCase: 0x000D)).Should().BeEmpty();
    }
}
