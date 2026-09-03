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

    // --- eReceiptCustomerId (IDZ) parsing from cbCustomer (middleware#764) ---

    private static ReceiptRequest RequestWithCustomer(object? cbCustomer) => new() { cbCustomer = cbCustomer };

    /// <summary>A well-formed cbCustomer whose identifier survives JSON escaping (tabs, non-ASCII).</summary>
    private static ReceiptRequest RequestWithEReceiptCustomerId(string customerId)
        => RequestWithCustomer(JsonSerializer.Serialize(new Dictionary<string, string> { ["eReceiptCustomerId"] = customerId }));

    [Fact]
    public void AnEReceiptCustomerId_IsReadFromCbCustomer_CaseInsensitively()
    {
        var request = RequestWithCustomer("""{"CustomerName": "Jan", "ERECEIPTCUSTOMERID": "KID0123456789ABC"}""");

        PosNetReceiptMapper.GetEReceiptCustomerId(request).Should().Be("KID0123456789ABC");
    }

    [Theory]
    [InlineData(null)]                                        // no customer at all
    [InlineData("")]                                          // empty payload
    [InlineData("""{"CustomerVATId": "1234563218"}""")]       // customer without the key
    [InlineData("""{"eReceiptCustomerId": ""}""")]            // present but empty
    [InlineData("""{"eReceiptCustomerId": "   "}""")]         // present but blank
    [InlineData("""{"eReceiptCustomerId": 42}""")]            // not a string
    [InlineData("""{"eReceiptCustomerId": "KID""")]           // malformed JSON
    [InlineData("not json at all")]                           // malformed JSON
    public void ACbCustomerWithoutAUsableEReceiptCustomerId_MeansNoBinding(string? cbCustomer)
    {
        PosNetReceiptMapper.GetEReceiptCustomerId(RequestWithCustomer(cbCustomer)).Should().BeNull();
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
    [InlineData("KID\u00A0123")]   // non-breaking space
    [InlineData("KID\t123")]      // a control character would open a protocol field
    public void ANonAsciiEReceiptCustomerId_IsRejected(string customerId)
    {
        var request = RequestWithEReceiptCustomerId(customerId);

        var act = () => PosNetReceiptMapper.GetEReceiptCustomerId(request);

        act.Should().Throw<PLValidationException>().WithMessage("*non-ASCII*");
    }
}
