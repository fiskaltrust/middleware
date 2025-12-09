using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Models;
using Xunit;

namespace fiskaltrust.Middleware.SCU.ES.AcceptanceTest;

public abstract class ESScuAcceptanceTestBase
{
    protected abstract IESSSCD CreateScu();

    [Fact]
    public async Task Test_Echo_ShouldReturnSameMessage()
    {
        // Arrange
        var scu = CreateScu();
        var request = new EchoRequest { Message = "Test Message" };

        // Act
        var response = await scu.EchoAsync(request);

        // Assert
        Assert.Equal(request.Message, response.Message);
    }

    [Fact]
    public virtual async Task Test_ProcessReceipt_PosReceipt_WithCash()
    {
        // Arrange
        var scu = CreateScu();
        var receiptRequest = ReceiptExamples.GetPosReceiptWithCash();
        var processRequest = CreateProcessRequest(receiptRequest);

        // Act
        var processResponse = await scu.ProcessReceiptAsync(processRequest);

        // Assert
        AssertSuccessfulReceipt(processResponse);
        Assert.NotNull(processResponse.ReceiptResponse.ftSignatures);
        Assert.NotEmpty(processResponse.ReceiptResponse.ftSignatures);
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

        var currentMoment = DateTime.UtcNow;
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "TERM001",
            cbReceiptReference = $"POS-{Guid.NewGuid().ToString()[..8]}",
            cbUser = "TestUser",
            cbReceiptMoment = currentMoment,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = 1.0m,
                    Amount = 100.0m,
                    UnitPrice = 100.0m,
                    VATRate = 21.0m,
                    VATAmount = 17.36m,
                    Description = "Service",
                    ftChargeItemCase = (ChargeItemCase)0x4753_0000_0000_0001,
                    Moment = currentMoment
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Quantity = 1,
                    Description = "Card",
                    ftPayItemCase = (PayItemCase)0x4753_0000_0000_0002,
                    Moment = currentMoment,
                    Amount = 100.0m
                }
            },
            ftReceiptCase = ((ReceiptCase) 0x4753_0000_0000_0000).WithCase(receiptCase)
        };

        var processRequest = CreateProcessRequest(receiptRequest);

        var processResponse = await scu.ProcessReceiptAsync(processRequest);
        AssertErrorReceipt(processResponse);
    }

    [Fact]
    public virtual async Task Test_ProcessReceipt_VoidReceipt()
    {
        // Arrange
        var scu = CreateScu();

        // First, create a normal receipt
        var normalReceiptRequest = ReceiptExamples.GetPosReceiptWithCash();
        var normalProcessRequest = CreateProcessRequest(normalReceiptRequest);
        var normalResponse = await scu.ProcessReceiptAsync(normalProcessRequest);

        // Now create a void receipt
        var voidReceiptRequest = ReceiptExamples.GetVoidReceipt();
        var voidProcessRequest = CreateProcessRequest(voidReceiptRequest);

        // Act
        var voidResponse = await scu.ProcessReceiptAsync(voidProcessRequest);

        // Assert
        AssertSuccessfulReceipt(voidResponse);
        Assert.NotNull(voidResponse.ReceiptResponse.ftSignatures);
        Assert.NotEmpty(voidResponse.ReceiptResponse.ftSignatures);
    }

    [Fact]
    public virtual async Task Test_ProcessReceipt_RefundReceipt()
    {
        // Arrange
        var scu = CreateScu();

        // First, create a normal receipt
        var normalReceiptRequest = ReceiptExamples.GetPosReceiptWithCash();
        var normalProcessRequest = CreateProcessRequest(normalReceiptRequest);
        var normalResponse = await scu.ProcessReceiptAsync(normalProcessRequest);

        // Now create a refund receipt
        var refundReceiptRequest = ReceiptExamples.GetRefundReceipt();
        var refundProcessRequest = CreateProcessRequest(refundReceiptRequest);

        // Act
        var refundResponse = await scu.ProcessReceiptAsync(refundProcessRequest);

        // Assert
        AssertSuccessfulReceipt(refundResponse);
        Assert.NotNull(refundResponse.ReceiptResponse.ftSignatures);
        Assert.NotEmpty(refundResponse.ReceiptResponse.ftSignatures);
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
        Assert.NotNull(processResponse);
        Assert.NotNull(processResponse.ReceiptResponse);
        Assert.True(((ulong) processResponse.ReceiptResponse.ftState & 0xFFFF_0000_0000_0000) == 0x4753_0000_0000_0000,
            "Receipt should have ES country code");
    }

    protected virtual void AssertErrorReceipt(ProcessResponse processResponse)
    {
        Assert.NotNull(processResponse);
        Assert.NotNull(processResponse.ReceiptResponse);
        Assert.True(processResponse.ReceiptResponse.ftState.IsState(State.Error), "Receipt should have ES country code");
    }
}
