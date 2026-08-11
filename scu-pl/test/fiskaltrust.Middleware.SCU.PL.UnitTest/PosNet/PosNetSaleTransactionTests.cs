using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

public class PosNetSaleTransactionTests
{
    [Fact]
    public void TheSpecSaleExample_ProducesTheDocumentedCommandSequence()
    {
        // POT-I-DEV-05 p.233: Apples 2.00 on rate B, 5.00 paid by card, 3.00 change in cash.
        var transaction = new PosNetSaleTransaction();
        transaction.Begin();
        transaction.AddLine("Apples", vatSlotIndex: 1, unitPriceGrosze: 200, quantity: 1m, totalGrosze: 200);
        transaction.AddPayment(paymentType: 2, amountGrosze: 500, isChange: false);
        transaction.AddPayment(paymentType: 0, amountGrosze: 300, isChange: true);
        var commands = transaction.End();

        commands.Should().HaveCount(5);
        Render(commands[0]).Should().Be("trinit bm0");
        Render(commands[1]).Should().Be("trline naApples vt1 pr200");
        Render(commands[2]).Should().Be("trpayment ty2 wa500 re0");
        Render(commands[3]).Should().Be("trpayment ty0 wa300 re1");
        Render(commands[4]).Should().Be("trend to200 re300 fp500");
    }

    [Fact]
    public void AddLine_BeforeBegin_IsRejected()
    {
        var transaction = new PosNetSaleTransaction();

        var act = () => transaction.AddLine("Apples", 1, 200, 1m, 200);

        act.Should().Throw<PLValidationException>().WithMessage("*trinit*");
    }

    [Fact]
    public void AddPayment_BeforeAnyLine_IsRejected()
    {
        var transaction = new PosNetSaleTransaction();
        transaction.Begin();

        var act = () => transaction.AddPayment(0, 500, isChange: false);

        act.Should().Throw<PLValidationException>().WithMessage("*sale line*");
    }

    [Fact]
    public void AddLine_AfterAPayment_IsRejected()
    {
        var transaction = new PosNetSaleTransaction();
        transaction.Begin();
        transaction.AddLine("Apples", 1, 200, 1m, 200);
        transaction.AddPayment(0, 200, isChange: false);

        var act = () => transaction.AddLine("Pears", 1, 100, 1m, 100);

        act.Should().Throw<PLValidationException>();
    }

    [Fact]
    public void End_WithoutAnyPayment_IsRejected()
    {
        var transaction = new PosNetSaleTransaction();
        transaction.Begin();
        transaction.AddLine("Apples", 1, 200, 1m, 200);

        var act = () => transaction.End();

        act.Should().Throw<PLValidationException>().WithMessage("*payment*");
    }

    [Fact]
    public void End_WhenPaymentsMinusChangeDoNotSettleTheTotal_IsRejected()
    {
        var transaction = new PosNetSaleTransaction();
        transaction.Begin();
        transaction.AddLine("Apples", 1, 200, 1m, 200);
        transaction.AddPayment(0, 500, isChange: false);
        transaction.AddPayment(0, 100, isChange: true);

        var act = () => transaction.End();

        act.Should().Throw<PLValidationException>().WithMessage("*settle*");
    }

    [Fact]
    public void AddLine_WithNegativeAmount_IsRejected()
    {
        var transaction = new PosNetSaleTransaction();
        transaction.Begin();

        var act = () => transaction.AddLine("Return", 1, 200, 1m, -200);

        act.Should().Throw<PLValidationException>().WithMessage("*positive*");
    }

    [Fact]
    public void Begin_Twice_IsRejected()
    {
        var transaction = new PosNetSaleTransaction();
        transaction.Begin();

        var act = () => transaction.Begin();

        act.Should().Throw<PLValidationException>();
    }

    private static string Render(PosNetCommand command)
    {
        var parts = new List<string> { command.Mnemonic };
        parts.AddRange(command.Parameters.Select(p => $"{p.Key}{p.Value}"));
        return string.Join(' ', parts);
    }
}
