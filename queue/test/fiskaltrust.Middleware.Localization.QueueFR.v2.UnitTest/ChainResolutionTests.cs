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
    [InlineData(ReceiptCase.PaymentTransfer0x0002, FRReceiptChain.PaymentProof)]
    [InlineData(ReceiptCase.DeliveryNote0x0005, FRReceiptChain.Bill)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002, FRReceiptChain.Invoice)]
    [InlineData(ReceiptCase.ZeroReceipt0x2000, FRReceiptChain.GrandTotal)]
    [InlineData(ReceiptCase.DailyClosing0x2011, FRReceiptChain.GrandTotal)]
    [InlineData(ReceiptCase.InitialOperationReceipt0x4001, FRReceiptChain.GrandTotal)]
    [InlineData(ReceiptCase.OutOfOperationReceipt0x4002, FRReceiptChain.GrandTotal)]
    [InlineData(ReceiptCase.ProtocolTechnicalEvent0x3001, FRReceiptChain.Log)]
    [InlineData(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010, FRReceiptChain.Duplicate)]
    public void ResolveChain_MapsTheReceiptCaseToItsChain(ReceiptCase receiptCase, FRReceiptChain expected)
        => Request(receiptCase).ResolveChain().Should().Be(expected);

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

    [Theory]
    [InlineData("T", 42L)]
    [InlineData("G", 1L)]
    public void ReadNumerator_RoundTripsWhatAppendChainIdentificationWrote(string identifier, long numerator)
    {
        var chain = new FRChainState(Enum.GetValues<FRReceiptChain>().First(x => x.Identifier() == identifier)) { Numerator = numerator };
        var response = new ReceiptResponse { ftReceiptIdentification = "ft123#" };

        ReceiptIdentificationHelper.AppendChainIdentification(response, chain);

        ReceiptIdentificationHelper.ReadNumerator(response.ftReceiptIdentification, identifier).Should().Be(numerator);
    }

    [Fact]
    public void ReadNumerator_ForAnotherChain_ReturnsNull()
        => ReceiptIdentificationHelper.ReadNumerator("ft123#T42", "G").Should().BeNull();
}
