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
    public void PaymentProofs_RefuseChargeItems_ButTakePayItems(long receiptCase)
    {
        Errors(Request(receiptCase, charge: 10m, pay: 10m)).Should().Contain("ChargeItemsNotAllowed");
        Errors(Request(receiptCase, pay: 200m)).Should().BeEmpty("a float pay-in carries only its pay item");
    }

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
