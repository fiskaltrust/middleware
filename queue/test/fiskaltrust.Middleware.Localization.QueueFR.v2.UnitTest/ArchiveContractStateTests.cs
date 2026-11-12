using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// The values the archive-v2 contract reads from the stored response, written once when the receipt is signed
/// (ADR 0011): document and operation type, discount total, line count and <c>CountsAsLine</c>, the discount of
/// each sales line, and the endpoint of a French business buyer inferred as <c>0225:&lt;SIREN&gt;</c> (§1.2, §2).
/// </summary>
public class ArchiveContractStateTests
{
    private static ChargeItemCase Charge(ChargeItemCaseFlags? flag = null)
    {
        var value = (long) ChargeItemCase.NormalVatRate.WithCountry("FR");
        return (ChargeItemCase) (flag is null ? value : value | (long) flag.Value);
    }

    private static ChargeItem Sale(decimal position, decimal amount) => new() { Position = position, Quantity = 1, Amount = amount, VATRate = 20, Description = "a", ftChargeItemCase = Charge(), Currency = Currency.EUR };

    private static ChargeItem Discount(decimal position, decimal amount, object? caseData = null) => new()
    {
        Position = position, Quantity = 1, Amount = amount, VATRate = 20, Description = "remise", ftChargeItemCase = Charge(ChargeItemCaseFlags.ExtraOrDiscount), ftChargeItemCaseData = caseData, Currency = Currency.EUR,
    };

    private static ReceiptRequest Request(long receiptCase, params ChargeItem[] items) => new()
    {
        ftReceiptCase = (ReceiptCase) (0x4652_2000_0000_0000 | receiptCase),
        Currency = Currency.EUR,
        cbChargeItems = items.ToList(),
        cbPayItems = [],
    };

    private static MiddlewareStateDataFR Apply(ReceiptRequest request)
    {
        var response = new ReceiptResponse { ftState = State.Success.WithCountry("FR") };
        FRArchiveStateData.Apply(request, response, request.ResolveChain(), null);
        var json = JsonSerializer.Serialize(response.ftStateData, response.ftStateData!.GetType());
        return JsonSerializer.Deserialize<MiddlewareStateData>(json)!.FR!;
    }

    [Theory]
    [InlineData(0x0001, "ticket")]
    [InlineData(0x0002, "payment-proof")]
    [InlineData(0x0003, "receipt-without-obligation")]
    [InlineData(0x0006, "note")]
    [InlineData(0x0007, "pro-forma")]
    [InlineData(0x1001, "invoice")]
    [InlineData(0x3004, "order")]
    [InlineData(0x0002_0001, "ticket")]
    public void TheDocumentType_IsDerivedFromTheReceiptCase(long receiptCase, string documentType)
        => Apply(Request(receiptCase, Sale(1, 10))).DocumentType.Should().Be(documentType);

    [Theory]
    [InlineData(0x0001, "sale")]
    [InlineData(0x0100_0001, "refund")]
    [InlineData(0x0004_0001, "void")]
    [InlineData(0x0104_0001, "void")]
    public void TheOperationType_IsDerivedFromTheFlags(long receiptCase, string operationType)
        => Apply(Request(receiptCase, Sale(1, 10))).OperationType.Should().Be(operationType);

    [Fact]
    public void ReceiptsOutsideTheSalesChains_StoreNoDocumentType()
    {
        var response = new ReceiptResponse { ftState = State.Success.WithCountry("FR") };
        FRArchiveStateData.Apply(Request(0x2011), response, FRReceiptChain.DailyClosing, null);
        (response.ftStateData is null || JsonSerializer.Serialize(response.ftStateData).Contains("DocumentType") == false).Should().BeTrue();
    }

    [Fact]
    public void TheDiscountTotal_AndTheLineCount_CoverEveryDiscountAndSalesLine()
    {
        var state = Apply(Request(0x0001, Discount(1, -0.50m), Sale(2, 10), Discount(3, -1), Sale(4, 5)));

        state.DiscountTotal.Should().Be(-1.50m, "a receipt-level discount counts too");
        state.LineCount.Should().Be(2);
        state.ChargeItems!.Select(x => x.CountsAsLine).Should().Equal(false, true, false, true);
    }

    [Fact]
    public void ADiscount_AppliesToTheLineBeforeIt_EvenWhenPositionsRepeat()
    {
        var state = Apply(Request(0x0001, Sale(0, 10), Sale(0, 5), Discount(0, -1, new { FR = new { DiscountCode = "D1", DiscountRate = 10 } })));

        state.ChargeItems![0].DiscountAmount.Should().BeNull("the discount follows sale B, not the first line with the same Position");
        state.ChargeItems[1].DiscountAmount.Should().Be(-1);
        state.ChargeItems[1].DiscountCode.Should().Be("D1");
        state.ChargeItems[1].DiscountRate.Should().Be("10");
    }

    [Fact]
    public void AReceiptLevelDiscount_AppliesToNoLine()
    {
        var state = Apply(Request(0x0001, Discount(1, -1), Sale(2, 10)));

        state.ChargeItems![0].AppliesTo.Should().BeNull();
        state.ChargeItems[1].DiscountAmount.Should().BeNull();
    }

    [Fact]
    public void TwoRatesTheSummaryKeyCannotTellApart_MakeOneLine()
    {
        var a = Sale(1, 10);
        var b = Sale(2, 5);
        b.VATRate = 20.00000000000001m;

        Apply(Request(0x0001, a, b)).VATSummary.Should().ContainSingle();
    }

    [Fact]
    public void AFrenchBusinessBuyersEndpoint_IsNeverInferred()
    {
        var request = Request(0x1002, Sale(1, 10));
        request.cbCustomer = new Dictionary<string, string?> { ["CustomerName"] = "Client", ["CustomerCountry"] = "FR", ["CustomerTaxId"] = "123456789" };

        // market-fr#221: eInvoicing data stays out of the fiscal state; the archive reads a sent endpoint from the request.
        JsonSerializer.Serialize(Apply(request)).Should().NotContain("0225").And.NotContain("CustomerEndpointId");
    }
}
