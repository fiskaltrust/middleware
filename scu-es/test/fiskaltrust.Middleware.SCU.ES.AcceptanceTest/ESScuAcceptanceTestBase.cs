using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.SCU.ES.Common.Models;
using FluentAssertions;

namespace fiskaltrust.Middleware.SCU.ES.AcceptanceTest;

public abstract class ESScuAcceptanceTestBase
{
    protected abstract IESSSCD CreateScu();

    [Fact]
    public async Task Test_Echo_ShouldReturnSameMessage()
    {
        var scu = CreateScu();
        var request = new EchoRequest { Message = "Test Message" };
        var response = await scu.EchoAsync(request);
        request.Message.Should().Be(response.Message);
    }

    [Fact]
    public virtual async Task Test_ProcessReceipt_PosReceipt_WithCash()
    {
        var scu = CreateScu();
        var receiptRequest = ReceiptExamples.GetPosReceiptWithCash(ReceiptCase.PointOfSaleReceipt0x0001);
        var processRequest = CreateProcessRequest(receiptRequest);

        var processResponse = await scu.ProcessReceiptAsync(processRequest);

        AssertSuccessfulReceipt(processResponse);
    }

    [Fact]
    public virtual async Task Test_ProcessReceipt_Invoice_WithCash()
    {
        var scu = CreateScu();
        var receiptRequest = ReceiptExamples.GetPosReceiptWithCash(ReceiptCase.InvoiceB2C0x1001);
        var processRequest = CreateProcessRequest(receiptRequest);

        var processResponse = await scu.ProcessReceiptAsync(processRequest);

        AssertSuccessfulReceipt(processResponse);
    }


    [Fact]
    public virtual async Task Test_Invoice_WithCustomer_WithCash_USCustomer()
    {
        var scu = CreateScu();
        var receiptRequest = new ReceiptRequest
        {
            cbReceiptReference = $"POS-{Guid.NewGuid().ToString()[..8]}",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems =
            [
                new ChargeItem
                {
                    Quantity = 2.0m,
                    Amount = 42.0m,
                    VATRate = 21.0m,
                    VATAmount = 7.24m,
                    Description = "Product A",
                    ftChargeItemCase = (ChargeItemCase)0x4753_0000_0000_0001
                },
                new ChargeItem
                {
                    Quantity = 1.0m,
                    Amount = 10.0m,
                    VATRate = 10.0m,
                    VATAmount = 0.91m,
                    ftChargeItemCase = (ChargeItemCase)0x4753_0000_0000_0001,
                    Description = "Product B"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Quantity = 1,
                    Description = "Cash",
                    ftPayItemCase = (PayItemCase)0x4753_0000_0000_0001,
                    Amount = 52.0m
                }
            ],
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001.WithCountry("ES").WithVersion(2),
            cbCustomer = new MiddlewareCustomer
            {
                CustomerIdentifier = "US673888750",
                CustomerZip = "28001",
                CustomerCity = "Madrid",
                CustomerCountry = "US",
                CustomerName = "John Doe",
                CustomerStreet = "Calle de Example, 1"
            }
        };
        var processRequest = CreateProcessRequest(receiptRequest);

        var processResponse = await scu.ProcessReceiptAsync(processRequest);

        AssertSuccessfulReceipt(processResponse);
    }


    [Fact]
    public virtual async Task Test_Invoice_WithCustomer_WithCash()
    {
        var scu = CreateScu();
        var receiptRequest = new ReceiptRequest
        {
            cbReceiptReference = $"POS-{Guid.NewGuid().ToString()[..8]}",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems =
            [
                new ChargeItem
                {
                    Quantity = 2.0m,
                    Amount = 42.0m,
                    VATRate = 21.0m,
                    VATAmount = 7.24m,
                    Description = "Product A",
                    ftChargeItemCase = (ChargeItemCase)0x4753_0000_0000_0001
                },
                new ChargeItem
                {
                    Quantity = 1.0m,
                    Amount = 10.0m,
                    VATRate = 10.0m,
                    VATAmount = 0.91m,
                    ftChargeItemCase = (ChargeItemCase)0x4753_0000_0000_0001,
                    Description = "Product B"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Quantity = 1,
                    Description = "Cash",
                    ftPayItemCase = (PayItemCase)0x4753_0000_0000_0001,
                    Amount = 52.0m
                }
            ],
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001.WithCountry("ES").WithVersion(2),
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "B10646545",
                CustomerZip = "28001",
                CustomerCity = "Madrid",
                CustomerCountry = "ES",
                CustomerName = "John Doe",
                CustomerStreet = "Calle de Example, 1"
            }
        };
        var processRequest = CreateProcessRequest(receiptRequest);

        var processResponse = await scu.ProcessReceiptAsync(processRequest);

        AssertSuccessfulReceipt(processResponse);
    }

    [Theory]
    [InlineData(ReceiptCase.ZeroReceipt0x2000)]
    [InlineData(ReceiptCase.OneReceipt0x2001)]
    [InlineData(ReceiptCase.DailyClosing0x2011)]
    [InlineData(ReceiptCase.MonthlyClosing0x2012)]
    [InlineData(ReceiptCase.YearlyClosing0x2013)]
    public virtual async Task Test_ProcessReceipt_WithReceiptCasesNotForwarded_ShouldWork(ReceiptCase receiptCase)
    {
        // Arrange
        var scu = CreateScu();
        var receiptRequest = new ReceiptRequest
        {
            cbReceiptReference = $"POS-{Guid.NewGuid().ToString()[..8]}",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = [],
            cbPayItems = [],
            ftReceiptCase = receiptCase.WithCountry("ES").WithVersion(2)
        };

        var processRequest = CreateProcessRequest(receiptRequest);
        var processResponse = await scu.ProcessReceiptAsync(processRequest);

        AssertSuccessfulReceipt(processResponse);
    }

    protected virtual ProcessRequest CreateProcessRequest(ReceiptRequest receiptRequest)
    {
        var cashBoxIdentification = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, 10);
        var receiptResponse = new ReceiptResponse
        {
            ftCashBoxID = receiptRequest.ftCashBoxID,
            ftCashBoxIdentification = cashBoxIdentification,
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            cbTerminalID = receiptRequest.cbTerminalID,
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = $"0#{cashBoxIdentification}/1",
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x4753_0000_0000_0000,
            ftSignatures = new List<SignatureItem>()
        };

        // Create minimal state data
        var middlewareStateData = new MiddlewareStateData
        {
            ES = new MiddlewareStateDataES
            {
                LastReceipt = null
            }
        };

        // Serialize to JSON and then parse as JsonElement to match what the SCU expects
        var json = JsonSerializer.Serialize(middlewareStateData);
        receiptResponse.ftStateData = JsonSerializer.Deserialize<JsonElement>(json);

        return new ProcessRequest
        {
            ReceiptRequest = receiptRequest,
            ReceiptResponse = receiptResponse
        };
    }

    protected virtual void AssertSuccessfulReceipt(ProcessResponse processResponse)
    {
        processResponse.Should().NotBeNull();
        processResponse.ReceiptResponse.Should().NotBeNull();

        processResponse.ReceiptResponse.ftState.State().Should().Be(State.Success);
    }

    protected virtual void AssertErrorReceipt(ProcessResponse processResponse)
    {
        processResponse.Should().NotBeNull();
        processResponse.ReceiptResponse.Should().NotBeNull();

        processResponse.ReceiptResponse.ftState.State().Should().Be(State.Error);
    }
}
