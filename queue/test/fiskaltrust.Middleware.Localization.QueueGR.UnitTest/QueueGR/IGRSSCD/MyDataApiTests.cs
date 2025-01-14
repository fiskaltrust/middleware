using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.myDataSCU;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest.QueueGR.IGRSSCD
{
    public class MyDataApiTests
    {
        [Fact]
        public async Task Test()
        {
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.Parse("6244b69d-15c5-4653-8e22-c72e6e954883"),
                ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0000,
                cbTerminalID = "1",
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbReceiptMoment = DateTime.UtcNow,
                cbChargeItems =
                [
                    new ChargeItem
                    {
                        Position = 1,
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
                        VATAmount = 1.2m,
                        Amount = 6.2m,
                        VATRate = 24m,
                        Quantity = 1,
                        Description = "ChargeItem1"
                    },
                    new ChargeItem
                    {
                        Position = 2,
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
                        VATAmount = 1.2m,
                        Amount = 6.2m,
                        VATRate = 24m,
                        Quantity = 1,
                        Description = "ChargeItem2"
                    }
                ],
                cbPayItems =
                [
                    new PayItem
                    {
                        ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                        Amount = 6.2m,
                        Description = "Cash"
                    }
                ]
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = (State) 0x4752_2000_0000_0000,
                ftCashBoxIdentification = "fiskaltrust1",
                ftCashBoxID = receiptRequest.ftCashBoxID,
                cbReceiptReference = receiptRequest.cbReceiptReference,
                cbTerminalID = receiptRequest.cbTerminalID,
                ftQueueID = Guid.Parse("30100f56-6009-48fb-a612-90143e48a67b"),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 5,
                ftReceiptIdentification = "ft123#",
                ftReceiptMoment = DateTime.UtcNow
            };

            var sut = new MyDataApiClient("user11111111", "41291863a36d552c4d7fc8195d427dd3", true);
            // var payload = sut.GenerateInvoicePayload(receiptRequest, receiptResponse);


            var result = await sut.ProcessReceiptAsync(new GRSSCD.ProcessRequest
            {
                ReceiptRequest = receiptRequest,
                ReceiptResponse = receiptResponse
            });

            var req = JsonSerializer.Serialize(receiptRequest);
            var data = JsonSerializer.Serialize(result.ReceiptResponse);

            var issueRequest = new
            {
                ReceiptRequest = receiptRequest,
                ReceiptResponse = receiptResponse
            };

            var dd = JsonSerializer.Serialize(issueRequest);
        }
    }
}
