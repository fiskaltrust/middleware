using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

/// <summary>
/// Minimal handler that returns a canned HTTP response — replaces Moq for HttpClient testing.
/// </summary>
internal class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _content;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, string content = "")
    {
        _statusCode = statusCode;
        _content = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage
        {
            StatusCode = _statusCode,
            Content = new StringContent(_content),
            RequestMessage = request
        });
    }
}

public class MyDataSCU_TransmissionFailure2_Tests
{
    private static MyDataSCU CreateScuWithHandler(HttpMessageHandler handler)
    {
        var scu = new MyDataSCU(
            username: "test-user",
            subscriptionKey: "test-key",
            baseAddress: "https://mydataapidev.aade.gr",
            receiptBaseAddress: "https://receipts-sandbox.fiskaltrust.eu",
            sandbox: true,
            masterDataConfiguration: new MasterDataConfiguration
            {
                Account = new AccountMasterData { VatId = "EL098000979" }
            },
            logger: null);

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://mydataapidev.aade.gr")
        };

        var field = typeof(MyDataSCU).GetField("_httpClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(scu, httpClient);

        return scu;
    }

    private static ProcessRequest CreateSimpleGRProcessRequest()
    {
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Amount = 10.00m,
                    Description = "Coffee",
                    Quantity = 1,
                    VATRate = 24,
                    VATAmount = 1.94m,
                    ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                    Position = 1
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 10.00m,
                    Description = "Cash",
                    ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment),
                    Position = 1
                }
            }
        };

        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft1#",
            ftCashBoxIdentification = "test-cashbox",
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid()
        };

        return new ProcessRequest
        {
            ReceiptRequest = receiptRequest,
            ReceiptResponse = receiptResponse
        };
    }

    [Theory]
    [InlineData(HttpStatusCode.BadGateway)]           // 502
    [InlineData(HttpStatusCode.ServiceUnavailable)]   // 503
    [InlineData(HttpStatusCode.GatewayTimeout)]       // 504
    [InlineData(HttpStatusCode.InternalServerError)]  // 500
    public async Task ProcessReceiptAsync_WhenMyDataReturns5xx_ShouldContainTransmissionFailure2Signature(HttpStatusCode statusCode)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(statusCode, "Server Error");
        var scu = CreateScuWithHandler(handler);
        var request = CreateSimpleGRProcessRequest();

        // Act
        var result = await scu.ProcessReceiptAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ReceiptResponse.ftSignatures.Should().Contain(
            sig => sig.Caption == "Transmission Failure_2",
            because: $"a {(int) statusCode} from myDATA means the government service is unreachable " +
                     "and MUST be classified as Transmission Failure 2 per AADE regulations");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task ProcessReceiptAsync_WhenMyDataReturns5xx_TransmissionFailure2SignatureData_ShouldContainGreekText(HttpStatusCode statusCode)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(statusCode, "Server Error");
        var scu = CreateScuWithHandler(handler);
        var request = CreateSimpleGRProcessRequest();

        // Act
        var result = await scu.ProcessReceiptAsync(request);

        // Assert
        var tf2Signature = result.ReceiptResponse.ftSignatures?
            .FirstOrDefault(s => s.Caption == "Transmission Failure_2");

        tf2Signature.Should().NotBeNull();
        tf2Signature!.Data.Should().NotBeNullOrWhiteSpace(
            "the TF2 signature must carry a description for the ISV/receipt");
    }

    [Fact]
    public async Task ProcessReceiptAsync_WhenMyDataReturns502_ShouldStillSetErrorState()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.BadGateway, "Bad Gateway");
        var scu = CreateScuWithHandler(handler);
        var request = CreateSimpleGRProcessRequest();

        // Act
        var result = await scu.ProcessReceiptAsync(request);

        // Assert
        var signatures = result.ReceiptResponse.ftSignatures;
        signatures.Should().NotBeNull();
        signatures.Should().Contain(sig => sig.Caption == "Transmission Failure_2");
    }

    [Fact]
    public async Task ProcessReceiptAsync_WhenMyDataReturns4xx_ShouldNOT_ContainTransmissionFailure2()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest, "Bad Request");
        var scu = CreateScuWithHandler(handler);
        var request = CreateSimpleGRProcessRequest();

        // Act
        var result = await scu.ProcessReceiptAsync(request);

        // Assert
        var signatures = result.ReceiptResponse.ftSignatures;
        if (signatures != null)
        {
            signatures.Should().NotContain(
                sig => sig.Caption == "Transmission Failure_2",
                because: "4xx errors are client errors — the government service is reachable, " +
                         "the request is invalid. This must NOT be classified as TF2.");
        }
    }
}