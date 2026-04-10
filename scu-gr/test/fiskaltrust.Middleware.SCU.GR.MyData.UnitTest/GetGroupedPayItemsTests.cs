using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class GetGroupedPayItemsTests
{
    private static PayItemCase GR(PayItemCase c) => ((PayItemCase) 0x4752_2000_0000_0000).WithCase(c);

    private static ReceiptRequest BuildRequest(List<PayItem> payItems) =>
        new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = System.DateTime.UtcNow,
            cbReceiptReference = System.Guid.NewGuid().ToString(),
            ftPosSystemId = System.Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>(),
            cbPayItems = payItems,
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Pay0x3005)
        };

    [Fact]
    public void SinglePayment_NoTip_ShouldReturnOneGroupWithNullTip()
    {
        var request = BuildRequest(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10m, Description = "Card", ftPayItemCase = GR(PayItemCase.CreditCardPayment) }
        });

        var result = request.GetGroupedPayItems();

        result.Should().HaveCount(1);
        result[0].payItem.Description.Should().Be("Card");
        result[0].tip.Should().BeNull();
    }

    [Fact]
    public void SinglePayment_FollowedByMatchingTip_ShouldGroup()
    {
        var request = BuildRequest(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10m, Description = "Card", ftPayItemCase = GR(PayItemCase.CreditCardPayment) },
            new PayItem { Position = 2, Amount = -1.5m, Description = "Tip", ftPayItemCase = GR(PayItemCase.CreditCardPayment).WithFlag(PayItemCaseFlags.Tip) }
        });

        var result = request.GetGroupedPayItems();

        result.Should().HaveCount(1);
        result[0].payItem.Description.Should().Be("Card");
        result[0].tip.Should().NotBeNull();
        result[0].tip!.Amount.Should().Be(-1.5m);
    }

    [Fact]
    public void SinglePayment_FollowedByTipWithDifferentCase_ShouldThrow()
    {
        var request = BuildRequest(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10m, Description = "Cash", ftPayItemCase = GR(PayItemCase.CashPayment) },
            new PayItem { Position = 2, Amount = -1m, Description = "Tip", ftPayItemCase = GR(PayItemCase.CreditCardPayment).WithFlag(PayItemCaseFlags.Tip) }
        });

        var act = () => request.GetGroupedPayItems();

        act.Should().Throw<ArgumentException>().Which.Message.Should().Contain("does not match");
    }

    [Fact]
    public void TwoPayments_EachFollowedByTip_ShouldGroupCorrectly()
    {
        var request = BuildRequest(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10m, Description = "Card 1", ftPayItemCase = GR(PayItemCase.CreditCardPayment) },
            new PayItem { Position = 2, Amount = -1.5m, Description = "Tip 1", ftPayItemCase = GR(PayItemCase.CreditCardPayment).WithFlag(PayItemCaseFlags.Tip) },
            new PayItem { Position = 3, Amount = 20m, Description = "Card 2", ftPayItemCase = GR(PayItemCase.CreditCardPayment) },
            new PayItem { Position = 4, Amount = -3m, Description = "Tip 2", ftPayItemCase = GR(PayItemCase.CreditCardPayment).WithFlag(PayItemCaseFlags.Tip) }
        });

        var result = request.GetGroupedPayItems();

        result.Should().HaveCount(2);
        result[0].payItem.Description.Should().Be("Card 1");
        result[0].tip!.Description.Should().Be("Tip 1");
        result[1].payItem.Description.Should().Be("Card 2");
        result[1].tip!.Description.Should().Be("Tip 2");
    }

    [Fact]
    public void TwoPayments_OnlyFirstFollowedByTip_SecondShouldHaveNoTip()
    {
        var request = BuildRequest(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10m, Description = "Card 1", ftPayItemCase = GR(PayItemCase.CreditCardPayment) },
            new PayItem { Position = 2, Amount = -2m, Description = "Tip", ftPayItemCase = GR(PayItemCase.CreditCardPayment).WithFlag(PayItemCaseFlags.Tip) },
            new PayItem { Position = 3, Amount = 15m, Description = "Card 2", ftPayItemCase = GR(PayItemCase.CreditCardPayment) }
        });

        var result = request.GetGroupedPayItems();

        result.Should().HaveCount(2);
        result[0].tip!.Description.Should().Be("Tip");
        result[1].tip.Should().BeNull("no tip follows the second card");
    }

    [Fact]
    public void TipNotDirectlyAfterPayment_ShouldThrow()
    {
        var request = BuildRequest(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10m, Description = "Card", ftPayItemCase = GR(PayItemCase.CreditCardPayment) },
            new PayItem { Position = 2, Amount = 5m, Description = "Cash", ftPayItemCase = GR(PayItemCase.CashPayment) },
            new PayItem { Position = 3, Amount = -1m, Description = "Tip for Card", ftPayItemCase = GR(PayItemCase.CreditCardPayment).WithFlag(PayItemCaseFlags.Tip) }
        });

        var act = () => request.GetGroupedPayItems();

        act.Should().Throw<ArgumentException>().Which.Message.Should().Contain("does not match");
    }

    [Fact]
    public void PaymentAlreadyHasTip_SecondTipShouldThrow()
    {
        var request = BuildRequest(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10m, Description = "Card", ftPayItemCase = GR(PayItemCase.CreditCardPayment) },
            new PayItem { Position = 2, Amount = -1m, Description = "Tip 1", ftPayItemCase = GR(PayItemCase.CreditCardPayment).WithFlag(PayItemCaseFlags.Tip) },
            new PayItem { Position = 3, Amount = -0.5m, Description = "Tip 2", ftPayItemCase = GR(PayItemCase.CreditCardPayment).WithFlag(PayItemCaseFlags.Tip) }
        });

        var act = () => request.GetGroupedPayItems();

        act.Should().Throw<ArgumentException>().Which.Message.Should().Contain("already has a tip");
    }

    [Fact]
    public void EmptyPayItems_ShouldReturnEmptyList()
    {
        var request = BuildRequest(new List<PayItem>());

        var result = request.GetGroupedPayItems();

        result.Should().BeEmpty();
    }

    [Fact]
    public void OnlyTipItems_ShouldThrow()
    {
        var request = BuildRequest(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = -1m, Description = "Tip", ftPayItemCase = GR(PayItemCase.CreditCardPayment).WithFlag(PayItemCaseFlags.Tip) }
        });

        var act = () => request.GetGroupedPayItems();

        act.Should().Throw<ArgumentException>().Which.Message.Should().Contain("no preceding payment");
    }

    [Fact]
    public void MixedCases_TipOnlyMatchesImmediatelyPrecedingSameCase()
    {
        var request = BuildRequest(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10m, Description = "Card", ftPayItemCase = GR(PayItemCase.CreditCardPayment) },
            new PayItem { Position = 2, Amount = -1m, Description = "Card Tip", ftPayItemCase = GR(PayItemCase.CreditCardPayment).WithFlag(PayItemCaseFlags.Tip) },
            new PayItem { Position = 3, Amount = 5m, Description = "Cash", ftPayItemCase = GR(PayItemCase.CashPayment) },
            new PayItem { Position = 4, Amount = -0.5m, Description = "Cash Tip", ftPayItemCase = GR(PayItemCase.CashPayment).WithFlag(PayItemCaseFlags.Tip) }
        });

        var result = request.GetGroupedPayItems();

        result.Should().HaveCount(2);
        result[0].payItem.Description.Should().Be("Card");
        result[0].tip!.Description.Should().Be("Card Tip");
        result[1].payItem.Description.Should().Be("Cash");
        result[1].tip!.Description.Should().Be("Cash Tip");
    }
}
