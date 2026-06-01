using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class GetGroupedByPositionTests
{
    private static ReceiptRequest BuildChargeRequest(List<ChargeItem> chargeItems) =>
        new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = chargeItems,
            cbPayItems = new List<PayItem>(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001)
        };

    private static ReceiptRequest BuildPayRequest(List<PayItem> payItems) =>
        new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>(),
            cbPayItems = payItems,
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Pay0x3005)
        };

    private static ChargeItem CI(decimal position, string description = "") =>
        new ChargeItem { Position = position, Amount = 1m, Description = description };

    private static PayItem PI(decimal position, string description = "") =>
        new PayItem { Position = position, Amount = 1m, Description = description };

    // === ChargeItem grouping ===

    [Fact]
    public void GroupChargeItemsByPosition_AllWholeNumbers_EachItsOwnGroup()
    {
        var request = BuildChargeRequest(new List<ChargeItem> { CI(1, "a"), CI(2, "b"), CI(3, "c") });

        var result = request.GetGroupedChargeItemsByPosition();

        result.Should().HaveCount(3);
        result[0].chargeItem.Description.Should().Be("a");
        result[0].modifiers.Should().BeEmpty();
        result[1].chargeItem.Description.Should().Be("b");
        result[2].chargeItem.Description.Should().Be("c");
    }

    [Fact]
    public void GroupChargeItemsByPosition_BaseFollowedBySubPositions_GroupsTogether()
    {
        var request = BuildChargeRequest(new List<ChargeItem> { CI(3, "base"), CI(3.1m, "mod1"), CI(3.2m, "mod2") });

        var result = request.GetGroupedChargeItemsByPosition();

        result.Should().HaveCount(1);
        result[0].chargeItem.Description.Should().Be("base");
        result[0].modifiers.Should().HaveCount(2);
        result[0].modifiers[0].Description.Should().Be("mod1");
        result[0].modifiers[1].Description.Should().Be("mod2");
    }

    [Fact]
    public void GroupChargeItemsByPosition_OnlySubPositions_LowestBecomesBase()
    {
        var request = BuildChargeRequest(new List<ChargeItem> { CI(5.1m, "a"), CI(5.2m, "b"), CI(5.3m, "c") });

        var result = request.GetGroupedChargeItemsByPosition();

        result.Should().HaveCount(1);
        result[0].chargeItem.Description.Should().Be("a");
        result[0].modifiers.Should().HaveCount(2);
        result[0].modifiers[0].Description.Should().Be("b");
        result[0].modifiers[1].Description.Should().Be("c");
    }

    [Fact]
    public void GroupChargeItemsByPosition_LowerPositionAfterHigher_DemotesPreviousBaseToModifier()
    {
        // Input order: 5.2 then 5.1 — 5.1 must end up as base, 5.2 demoted
        var request = BuildChargeRequest(new List<ChargeItem> { CI(5.2m, "high"), CI(5.1m, "low"), CI(5.3m, "highest") });

        var result = request.GetGroupedChargeItemsByPosition();

        result.Should().HaveCount(1);
        result[0].chargeItem.Description.Should().Be("low");
        result[0].modifiers[0].Description.Should().Be("high");
        result[0].modifiers[1].Description.Should().Be("highest");
    }

    [Fact]
    public void GroupChargeItemsByPosition_FullExampleScenario_GroupsCorrectly()
    {
        // The exact example from the request: 1, 2, 3, 3.1, 3.2, 4, 5.1, 5.2, 5.3
        var request = BuildChargeRequest(new List<ChargeItem>
        {
            CI(1, "a"), CI(2, "b"), CI(3, "c"), CI(3.1m, "c.1"), CI(3.2m, "c.2"),
            CI(4, "d"), CI(5.1m, "e"), CI(5.2m, "e.1"), CI(5.3m, "e.2")
        });

        var result = request.GetGroupedChargeItemsByPosition();

        result.Should().HaveCount(5);
        result[0].chargeItem.Description.Should().Be("a"); result[0].modifiers.Should().BeEmpty();
        result[1].chargeItem.Description.Should().Be("b"); result[1].modifiers.Should().BeEmpty();
        result[2].chargeItem.Description.Should().Be("c"); result[2].modifiers.Should().HaveCount(2);
        result[2].modifiers[0].Description.Should().Be("c.1");
        result[2].modifiers[1].Description.Should().Be("c.2");
        result[3].chargeItem.Description.Should().Be("d"); result[3].modifiers.Should().BeEmpty();
        result[4].chargeItem.Description.Should().Be("e"); result[4].modifiers.Should().HaveCount(2);
        result[4].modifiers[0].Description.Should().Be("e.1");
        result[4].modifiers[1].Description.Should().Be("e.2");
    }

    [Fact]
    public void GroupChargeItemsByPosition_DuplicatePosition_Throws()
    {
        var request = BuildChargeRequest(new List<ChargeItem> { CI(5.1m, "a"), CI(5.1m, "b") });

        var act = () => request.GetGroupedChargeItemsByPosition();

        act.Should().Throw<ArgumentException>().WithMessage("*5.1*");
    }

    [Fact]
    public void GroupChargeItemsByPosition_DuplicatePositionVersusBase_Throws()
    {
        // 3 (base) and a later 3.0 → both have the same numeric value
        var request = BuildChargeRequest(new List<ChargeItem> { CI(3, "a"), CI(3.0m, "b") });

        var act = () => request.GetGroupedChargeItemsByPosition();

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GroupChargeItemsByPosition_PositionZero_EachItsOwnGroup()
    {
        var request = BuildChargeRequest(new List<ChargeItem> { CI(0, "a"), CI(0, "b"), CI(0, "c") });

        var result = request.GetGroupedChargeItemsByPosition();

        result.Should().HaveCount(3);
        result.Should().OnlyContain(g => g.modifiers.Count == 0);
    }

    [Fact]
    public void GroupChargeItemsByPosition_Empty_ReturnsEmpty()
    {
        var request = BuildChargeRequest(new List<ChargeItem>());

        var result = request.GetGroupedChargeItemsByPosition();

        result.Should().BeEmpty();
    }

    // === PayItem grouping (same rule) ===

    [Fact]
    public void GroupPayItemsByPosition_BaseAndSubPositions_GroupsTogether()
    {
        var request = BuildPayRequest(new List<PayItem> { PI(1, "cash"), PI(1.1m, "tip"), PI(2, "card") });

        var result = request.GetGroupedPayItemsByPosition();

        result.Should().HaveCount(2);
        result[0].payItem.Description.Should().Be("cash");
        result[0].modifiers.Should().ContainSingle().Which.Description.Should().Be("tip");
        result[1].payItem.Description.Should().Be("card");
    }

    [Fact]
    public void GroupPayItemsByPosition_DuplicatePosition_Throws()
    {
        var request = BuildPayRequest(new List<PayItem> { PI(1, "a"), PI(1.5m, "b"), PI(1.5m, "c") });

        var act = () => request.GetGroupedPayItemsByPosition();

        act.Should().Throw<ArgumentException>().WithMessage("*1.5*");
    }
}
