using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace fiskaltrust.Middleware.SCU.GR.AcceptanceTests;

/// <summary>
/// Fuel documents against the AADE sandbox (fiskaltrust/market-gr#373). fuelInvoice is independent of the
/// invoice type (1.1 and 11.x). AADE forbids a counterpart on retail types (error 205) unless
/// invoiceHeader.fuelInvoice = true; then the buyer and the electricity supply number
/// (counterpart.supplyAccountNo) can be declared, e.g. for heating oil (A.1139/2025).
/// </summary>
[Trait("only", "local")]
public class FuelInvoiceAcceptanceTests
{
    private const string ISSUER_VAT = "112545020";
    private const string CUSTOMER_VAT = "026883248";

    private readonly ITestOutputHelper _output;

    public FuelInvoiceAcceptanceTests(ITestOutputHelper output) => _output = output;

    private static AADEFactory CreateFactory() => new(new fiskaltrust.storage.V0.MasterData.MasterDataConfiguration
    {
        Account = new fiskaltrust.storage.V0.MasterData.AccountMasterData { VatId = ISSUER_VAT },
        Outlet = new fiskaltrust.storage.V0.MasterData.OutletMasterData { LocationId = "0" }
    }, "https://test.receipts.example.com");

    private static ReceiptRequest CreateFuelReceipt(int fuelCode, string? supplyAccountNo, bool refund = false, bool withCustomer = true, bool b2b = false)
    {
        var invoice = new Dictionary<string, object> { ["invoiceHeader"] = new { fuelInvoice = true } };
        if (supplyAccountNo != null)
        {
            invoice["counterpart"] = new { supplyAccountNo };
        }
        var sign = refund ? -1 : 1;
        var receiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(b2b ? ReceiptCase.InvoiceB2B0x1002 : ReceiptCase.PointOfSaleReceipt0x0001);
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1,
                    Amount = sign * 124,
                    Quantity = sign * 100,
                    Description = "Diesel",
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                    VATRate = 24,
                    ftChargeItemCaseData = new { GR = new { mydataoverride = new { invoiceDetails = new { fuelCode } } } }
                }
            ],
            cbPayItems = [new PayItem { Amount = sign * 124, ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001 }],
            cbCustomer = withCustomer ? new { CustomerVATId = "EL" + CUSTOMER_VAT, CustomerCountry = "GR", CustomerName = "Πελάτης", CustomerStreet = "Κηφισίας", CustomerHouseNumber = "12", CustomerZip = "12345", CustomerCity = "Αθηνών" } : null,
            ftReceiptCase = refund ? receiptCase.WithFlag(ReceiptCaseFlags.Refund) : receiptCase,
            ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice } } }
        };
    }

    private async Task<ResponseType> SendAsync(ReceiptRequest request, bool expectCounterpart = true)
    {
        var response = new ReceiptResponse
        {
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftCashBoxIdentification = "FUEL",
            ftReceiptIdentification = "ft" + DateTime.UtcNow.Ticks.ToString("X") + "#",
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x4752_2000_0000_0000
        };
        var (doc, error) = CreateFactory().MapToInvoicesDoc(request, response);
        error.Should().BeNull();
        var xml = AADEFactory.GenerateInvoicePayload(doc!);
        _output.WriteLine(xml);
        if (expectCounterpart)
        {
            // a success must mean AADE accepted the declared buyer
            xml.Should().Contain($"<counterpart><vatNumber>{CUSTOMER_VAT}</vatNumber>");
        }
        else
        {
            xml.Should().NotContain("<counterpart>");
        }

        using var httpClient = new HttpClient { BaseAddress = new Uri("https://mydataapidev.aade.gr/") };
        httpClient.DefaultRequestHeaders.Add("aade-user-id", "user11111111");
        httpClient.DefaultRequestHeaders.Add("ocp-apim-subscription-key", "41291863a36d552c4d7fc8195d427dd3");
        var httpResponse = await httpClient.PostAsync("/myDataProvider/SendInvoices", new StringContent(xml, Encoding.UTF8, "application/xml"));
        var content = await httpResponse.Content.ReadAsStringAsync();
        _output.WriteLine(content);
        httpResponse.IsSuccessStatusCode.Should().BeTrue(content);

        using var reader = new StringReader(content);
        var result = (ResponseDoc) new XmlSerializer(typeof(ResponseDoc)).Deserialize(reader)!;
        return result.response[0];
    }

    [Theory]
    [InlineData(30)] // Diesel Θέρμανσης
    [InlineData(31)] // Diesel Θέρμανσης Premium
    [InlineData(10)] // Βενζίνη Αμόλυβδη 95
    public async Task RetailFuelReceipt_WithBuyerVat_ShouldBeAccepted(int fuelCode)
    {
        var result = await SendAsync(CreateFuelReceipt(fuelCode, supplyAccountNo: null));

        result.statusCode.Should().Be("Success");
    }

    [Fact]
    public async Task RetailHeatingOilReceipt_WithSupplyAccountNo_ShouldBeAccepted()
    {
        var result = await SendAsync(CreateFuelReceipt(30, supplyAccountNo: "12345678901"));

        result.statusCode.Should().Be("Success");
    }

    [Fact]
    public async Task RetailHeatingOilRefund_WithSupplyAccountNo_ShouldBeAccepted()
    {
        var result = await SendAsync(CreateFuelReceipt(30, supplyAccountNo: "12345678901", refund: true));

        result.statusCode.Should().Be("Success");
    }

    [Theory]
    [InlineData(10, false)] // anonymous petrol sale
    [InlineData(30, false)] // anonymous heating oil sale
    [InlineData(30, true)]  // anonymous heating oil refund (11.4)
    public async Task RetailFuelReceipt_WithoutCustomer_ShouldBeAccepted(int fuelCode, bool refund)
    {
        var result = await SendAsync(CreateFuelReceipt(fuelCode, supplyAccountNo: null, refund, withCustomer: false), expectCounterpart: false);

        result.statusCode.Should().Be("Success");
    }

    [Fact]
    public async Task FuelInvoice11_WithSupplyAccountNo_ShouldBeAccepted()
    {
        var result = await SendAsync(CreateFuelReceipt(30, supplyAccountNo: "12345678901", b2b: true));

        result.statusCode.Should().Be("Success");
    }

    [Theory]
    [InlineData(-100)] // refund charge item with negative quantity
    [InlineData(100)]  // refund charge item with positive quantity
    public async Task RetailHeatingOilRefund_ShouldSendPositiveQuantity(decimal chargeItemQuantity)
    {
        var request = CreateFuelReceipt(30, supplyAccountNo: null, refund: true);
        request.cbChargeItems[0].Quantity = chargeItemQuantity;

        var result = await SendAsync(request);

        result.statusCode.Should().Be("Success");
    }

    [Fact]
    public async Task RetailFuelReceipt_WithOtherChargeLine999_ShouldBeAccepted()
    {
        // On a fuel document every line needs a fuelCode and a quantity; non-fuel items (e.g. a car wash) use 999.
        var request = CreateFuelReceipt(30, supplyAccountNo: null);
        request.cbChargeItems.Add(new ChargeItem
        {
            Position = 2,
            Amount = 12.4m,
            Quantity = 1,
            Description = "Car wash",
            ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
            VATRate = 24,
            ftChargeItemCaseData = new { GR = new { mydataoverride = new { invoiceDetails = new { fuelCode = 999 } } } }
        });
        request.cbPayItems[0].Amount = 136.4m;

        var result = await SendAsync(request);

        result.statusCode.Should().Be("Success");
    }
}
