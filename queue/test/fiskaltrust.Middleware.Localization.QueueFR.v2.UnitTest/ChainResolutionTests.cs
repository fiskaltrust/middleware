using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

public class ChainResolutionTests
{
    private static ReceiptRequest Request(ReceiptCase receiptCase) => new() { ftReceiptCase = receiptCase.WithCountry("FR") };

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000, FRReceiptChain.Ticket)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001, FRReceiptChain.Ticket)]
    [InlineData(ReceiptCase.ECommerce0x0004, FRReceiptChain.Ticket)]
    [InlineData(ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003, FRReceiptChain.Bill)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002, FRReceiptChain.PaymentProof)]
    [InlineData(ReceiptCase.DeliveryNote0x0005, FRReceiptChain.Bill)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002, FRReceiptChain.Invoice)]
    [InlineData(ReceiptCase.ZeroReceipt0x2000, FRReceiptChain.Technical)]
    [InlineData(ReceiptCase.OneReceipt0x2001, FRReceiptChain.Technical)]
    [InlineData(ReceiptCase.ShiftClosing0x2010, FRReceiptChain.ShiftClosing)]
    [InlineData(ReceiptCase.DailyClosing0x2011, FRReceiptChain.DailyClosing)]
    [InlineData(ReceiptCase.MonthlyClosing0x2012, FRReceiptChain.MonthlyClosing)]
    [InlineData(ReceiptCase.YearlyClosing0x2013, FRReceiptChain.YearlyClosing)]
    [InlineData(ReceiptCase.InitialOperationReceipt0x4001, FRReceiptChain.Technical)]
    [InlineData(ReceiptCase.OutOfOperationReceipt0x4002, FRReceiptChain.Technical)]
    [InlineData(ReceiptCase.ProtocolTechnicalEvent0x3001, FRReceiptChain.Log)]
    [InlineData(ReceiptCase.ProtocolAccountingEvent0x3002, FRReceiptChain.Log)]
    [InlineData(ReceiptCase.Order0x3004, FRReceiptChain.Bill)]
    [InlineData(ReceiptCase.Pay0x3005, FRReceiptChain.PaymentProof)]
    [InlineData(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010, FRReceiptChain.Duplicate)]
    public void ResolveChain_MapsTheReceiptCaseToItsChain(ReceiptCase receiptCase, FRReceiptChain expected)
        => Request(receiptCase).ResolveChain().Should().Be(expected);

    /// <summary>
    /// A sale flagged as made in a foreign country stays a ticket: NF525 does not exempt it from the
    /// grand totals, and v1.2 in production counted it, since the PosSystem API drops the flag.
    /// </summary>
    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000, FRReceiptChain.Ticket)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001, FRReceiptChain.Ticket)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001, FRReceiptChain.Invoice)]
    public void ResolveChain_SaleInForeignCountry_KeepsTheChainOfItsCase(ReceiptCase receiptCase, FRReceiptChain expected)
    {
        var request = new ReceiptRequest { ftReceiptCase = receiptCase.WithCountry("FR").WithFlag(ReceiptCaseFlags.SaleInForeignCountry) };

        request.ResolveChain().Should().Be(expected);
    }

    [Theory]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.DailyClosing0x2011)]
    public void ResolveChain_TrainingNeverEntersAFiscalChain(ReceiptCase receiptCase)
    {
        var request = new ReceiptRequest { ftReceiptCase = receiptCase.WithCountry("FR").WithFlag(ReceiptCaseFlags.Training) };

        request.ResolveChain().Should().Be(FRReceiptChain.Training);
    }

    [Fact]
    public void Identifier_IsUniquePerChain()
    {
        var identifiers = Enum.GetValues<FRReceiptChain>().Select(x => x.Identifier()).ToList();

        identifiers.Should().OnlyHaveUniqueItems("two chains sharing a letter would make their numbering indistinguishable");
    }

    [Fact]
    public void AppendChainIdentification_WritesTheChainLetterAndNumber()
    {
        var chain = new FRChainState(FRReceiptChain.DailyClosing) { Numerator = 42 };
        var response = new ReceiptResponse { ftReceiptIdentification = "ft123#" };

        ReceiptIdentificationHelper.AppendChainIdentification(response, chain);

        response.ftReceiptIdentification.Should().Be("ft123#Z42");
    }
}
