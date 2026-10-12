using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

/// <summary>
/// The invariant a POSNET sale line has to satisfy: the register prints and totalizes price,
/// quantity and value, so price x quantity must equal the value — measured against the quantity as
/// the protocol carries it, not as the caller wrote it.
/// </summary>
public class PosNetReceiptMapperTests
{
    [Theory]
    [InlineData(200, 1)]          // no il is sent at all
    [InlineData(600, 3)]          // 3 x 2.00
    [InlineData(500, 2.5)]        // 2.5 x 2.00, within the wire precision
    [InlineData(2470, 1.235)]     // exactly three decimals: 1.235 x 20.00
    public void AQuantityTheProtocolCanCarry_IsAccepted(long totalGrosze, decimal quantity)
    {
        var act = () => PosNetReceiptMapper.ToUnitPriceGrosze("Woda", totalGrosze, quantity);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(2469, 1.2345)]    // travels as il1.235, and 1.235 x 20.00 is 24.70 against 24.69
    [InlineData(1000, 0.0004)]    // travels as il0.000
    [InlineData(10000, 1.00005)]
    public void AQuantityWithMoreDecimalsThanTheWireCarries_IsRejected(long totalGrosze, decimal quantity)
    {
        var act = () => PosNetReceiptMapper.ToUnitPriceGrosze("Woda", totalGrosze, quantity);

        act.Should().Throw<PLValidationException>()
            .WithMessage($"*more than {PosNetCommands.QuantityDecimals} decimal places*");
    }

    [Fact]
    public void TheAcceptedQuantitiesHoldTheInvariantOnTheWire()
    {
        const long totalGrosze = 2470;
        const decimal quantity = 1.235m;

        var unitPriceGrosze = PosNetReceiptMapper.ToUnitPriceGrosze("Woda", totalGrosze, quantity);
        var onTheWire = decimal.Parse(
            Render(PosNetCommands.Trline("Woda", 0, unitPriceGrosze, quantity, totalGrosze), "il"),
            System.Globalization.CultureInfo.InvariantCulture);

        (unitPriceGrosze * onTheWire).Should().Be(totalGrosze);
    }

    /// <summary>A line total that does not divide by its quantity is still refused.</summary>
    [Fact]
    public void ALineTotalThatDoesNotDivideByItsQuantity_IsRejected()
    {
        var act = () => PosNetReceiptMapper.ToUnitPriceGrosze("Woda", totalGrosze: 1000, quantity: 3m);

        act.Should().Throw<PLValidationException>().WithMessage("*whole number of grosze per unit*");
    }

    private static string Render(PosNetCommand command, string parameter)
        => command.Parameters.Single(p => p.Key == parameter).Value;

    // --- e-receipt customer identifier (IDZ) from ftReceiptCaseData.PL.eReceipt.customerId (middleware#764) ---

    private static ReceiptRequest RequestWithCaseData(object? receiptCaseData) => new() { ftReceiptCaseData = receiptCaseData };

    /// <summary>A well-formed PL payload whose identifier survives JSON escaping (tabs, non-ASCII).</summary>
    private static ReceiptRequest RequestWithEReceiptCustomerId(string customerId)
        => RequestWithCaseData(new { PL = new { eReceipt = new { customerId } } });

    [Fact]
    public void AnEReceiptCustomerId_IsReadFromThePlPayload_CaseInsensitively()
    {
        var request = RequestWithCaseData("""{"pl": {"ERECEIPT": {"CustomerId": "KID0123456789ABC"}, "printout": {"lines": []}}}""");

        PosNetReceiptMapper.GetEReceiptCustomerId(request).Should().Be("KID0123456789ABC");
    }

    [Fact]
    public void AnEReceiptCustomerId_IsNotReadFromCbCustomer()
    {
        // The customer identity in cbCustomer must never bind an e-receipt by itself — binding makes
        // the receipt paperless, so it is an explicit request in the PL payload.
        var request = new ReceiptRequest { cbCustomer = """{"CustomerId": "KID0123456789ABC", "eReceiptCustomerId": "KID0123456789ABC"}""" };

        PosNetReceiptMapper.GetEReceiptCustomerId(request).Should().BeNull();
    }

    [Theory]
    [InlineData(null)]                                                     // no case data at all
    [InlineData("")]                                                       // empty payload
    [InlineData("""{"DE": {"something": 1}}""")]                           // another market's payload
    [InlineData("""{"PL": {"printout": {"lines": []}}}""")]                // PL payload without eReceipt
    [InlineData("""{"PL": {"eReceipt": {}}}""")]                           // eReceipt without customerId
    [InlineData("""{"PL": {"eReceipt": {"customerId": ""}}}""")]           // present but empty
    [InlineData("""{"PL": {"eReceipt": {"customerId": "   "}}}""")]        // present but blank
    [InlineData("""{"PL": {"eReceipt": {"customerId": 42}}}""")]           // not a string
    [InlineData("""{"PL": {"eReceipt": "KID"}}""")]                        // eReceipt not an object
    [InlineData("""{"PL": {"eReceipt": {"customerId": "KID""")]            // malformed JSON
    [InlineData("not json at all")]                                        // malformed JSON
    public void ACaseDataWithoutAUsableEReceiptCustomerId_MeansNoBinding(string? receiptCaseData)
    {
        PosNetReceiptMapper.GetEReceiptCustomerId(RequestWithCaseData(receiptCaseData)).Should().BeNull();
    }

    [Fact]
    public void AnEReceiptCustomerIdAtTheIdzLimit_IsAccepted()
    {
        var request = RequestWithEReceiptCustomerId(new string('A', 128));

        PosNetReceiptMapper.GetEReceiptCustomerId(request).Should().HaveLength(128);
    }

    [Fact]
    public void AnEReceiptCustomerIdOverTheIdzLimit_IsRejected()
    {
        var request = RequestWithEReceiptCustomerId(new string('A', 129));

        var act = () => PosNetReceiptMapper.GetEReceiptCustomerId(request);

        act.Should().Throw<PLValidationException>().WithMessage("*129*128*");
    }

    [Theory]
    [InlineData("KIDżółć")]       // non-ASCII letters
    [InlineData("KID 123")]   // non-breaking space
    [InlineData("KID\t123")]      // a control character would open a protocol field
    public void ANonAsciiEReceiptCustomerId_IsRejected(string customerId)
    {
        var request = RequestWithEReceiptCustomerId(customerId);

        var act = () => PosNetReceiptMapper.GetEReceiptCustomerId(request);

        act.Should().Throw<PLValidationException>().WithMessage("*non-ASCII*");
    }
}
