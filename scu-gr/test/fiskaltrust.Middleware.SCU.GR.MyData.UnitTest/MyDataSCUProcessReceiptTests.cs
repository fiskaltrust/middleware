using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class MyDataSCUProcessReceiptTests
{
    [Fact]
    public async Task ProcessReceiptAsync_OnSuccess_WritesBackAnIdenticalSegment()
    {
        // On success the SCU writes the filed (series, aa) back into the country
        // segment. For queue-stamped identifications this must be an identity: the doc
        // numbering derives from the very segment the queue stamped, so the value stays
        // byte-identical. This pins the invariant the queue's migration seed relies on —
        // a regression to the old blind append would produce "ft1#CB-A-42CB-A-42" and
        // corrupt the next migration seed.
        var responseDoc = new ResponseDoc
        {
            response =
            [
                new ResponseType
                {
                    statusCode = "Success",
                    ItemsElementName = [ItemsChoiceType.invoiceUid, ItemsChoiceType.invoiceMark, ItemsChoiceType.authenticationCode],
                    Items = ["test-uid", 400001L, "test-auth"],
                },
            ],
        };
        var httpClient = new HttpClient(new StubHttpMessageHandler(SerializeResponseDoc(responseDoc)))
        {
            BaseAddress = new Uri("https://mydata.test.example.com"),
        };
        var scu = new MyDataSCU(httpClient, "https://receipts.example.com", sandbox: false, new MasterDataConfiguration
        {
            Account = new AccountMasterData { VatId = "Test" },
        });

        var receiptRequest = new ReceiptRequest
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
                    Amount = 100,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                    VATRate = 24,
                },
            ],
            cbPayItems = [new PayItem { Amount = 100 }],
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            cbReceiptAmount = 100,
        };
        var receiptResponse = new ReceiptResponse
        {
            ftState = (State) 0x4752_2000_0000_0000,
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftCashBoxIdentification = "CB-A",
            ftReceiptIdentification = "ft1#CB-A-42",
        };

        var result = await scu.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = receiptRequest,
            ReceiptResponse = receiptResponse,
        }, []);

        result.ReceiptResponse.ftState.IsState(State.Success).Should().BeTrue();
        result.ReceiptResponse.ftReceiptIdentification.Should().Be("ft1#CB-A-42");
        result.ReceiptResponse.ftSignatures.Should().Contain(s => s.Caption == "invoiceMark" && s.Data == "400001");
    }

    [Fact]
    public async Task ProcessReceiptAsync_WithoutQueueSegment_AppendsFiledSeriesAndAa()
    {
        // Direct SCU consumers that don't pre-append a segment still get a
        // self-describing response: the filed (series, aa) — derived via the legacy
        // fallback (series = ftCashBoxIdentification, aa = hex numerator) — is written
        // back after the "#", matching the historical behaviour.
        var responseDoc = new ResponseDoc
        {
            response =
            [
                new ResponseType
                {
                    statusCode = "Success",
                    ItemsElementName = [ItemsChoiceType.invoiceUid, ItemsChoiceType.invoiceMark, ItemsChoiceType.authenticationCode],
                    Items = ["test-uid", 400002L, "test-auth"],
                },
            ],
        };
        var httpClient = new HttpClient(new StubHttpMessageHandler(SerializeResponseDoc(responseDoc)))
        {
            BaseAddress = new Uri("https://mydata.test.example.com"),
        };
        var scu = new MyDataSCU(httpClient, "https://receipts.example.com", sandbox: false, new MasterDataConfiguration
        {
            Account = new AccountMasterData { VatId = "Test" },
        });

        var receiptRequest = new ReceiptRequest
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
                    Amount = 100,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                    VATRate = 24,
                },
            ],
            cbPayItems = [new PayItem { Amount = 100 }],
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            cbReceiptAmount = 100,
        };
        var receiptResponse = new ReceiptResponse
        {
            ftState = (State) 0x4752_2000_0000_0000,
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftCashBoxIdentification = "CB-A",
            ftReceiptIdentification = "ftA#",
        };

        var result = await scu.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = receiptRequest,
            ReceiptResponse = receiptResponse,
        }, []);

        result.ReceiptResponse.ftState.IsState(State.Success).Should().BeTrue();
        result.ReceiptResponse.ftReceiptIdentification.Should().Be("ftA#CB-A-10");
    }

    private static string SerializeResponseDoc(ResponseDoc responseDoc)
    {
        var serializer = new XmlSerializer(typeof(ResponseDoc));
        using var writer = new StringWriter();
        serializer.Serialize(writer, responseDoc);
        return writer.ToString();
    }

    private sealed class StubHttpMessageHandler(string responseContent) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/xml"),
            };
            return Task.FromResult(response);
        }
    }
}
