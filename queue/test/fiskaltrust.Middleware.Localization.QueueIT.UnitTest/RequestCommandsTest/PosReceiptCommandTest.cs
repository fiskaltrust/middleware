using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using System.Linq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Moq;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Localization.QueueIT.v2.Receipt;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest
{
    public class PosReceiptCommandTest
    {

        [Fact]
        public async Task ExecuteAsync_RegisterInvoice_ValidResultAsync()
        {
            var request = new ReceiptRequest()
            {
                cbReceiptReference = "Reference007",
                cbChargeItems = new[]
                {
                    new ChargeItem()
                    {
                        Description = "Testitem1",
                        Amount = 9999.98m,
                        ftChargeItemCase = 0x4954000000000001,
                        Quantity= 2,
                    },
                    new ChargeItem()
                    {
                        Description = "Testitem2",
                        Amount = 10,
                        ftChargeItemCase = 0x4954000000000002,
                        Quantity= 1,
                    },
                    new ChargeItem()
                    {
                        Description = "Discount 22% vat",
                        Amount = -100,
                        ftChargeItemCase = 0x4954000000000023,
                    },
                },
                cbPayItems = new PayItem[]
                {
                    new PayItem(){
                        Description = "Cash",
                        Amount = 9909.98m,
                        ftPayItemCase = 0x4954000000000001
                    }
                }
            };

            var desscdMock = new Mock<IITSSCDProvider>();
            desscdMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>())).Returns(() => throw new NotImplementedException());
            desscdMock.Setup(x => x.GetRTInfoAsync()).ReturnsAsync(new RTInfo
            {

            });


            var queueIt = new ftQueueIT() { CashBoxIdentification = "testserial", ftSignaturCreationUnitITId = Guid.NewGuid() };
      
            var posReceiptCommand = new PointOfSaleReceipt0x0001(desscdMock.Object);

            var queue = new ftQueue() { ftQueueId = Guid.NewGuid(), ftReceiptNumerator = 5 };
            var queueItem = new ftQueueItem() { ftQueueId = queue.ftQueueId, ftQueueItemId = Guid.NewGuid(), ftQueueRow = 7 };

            var (receiptResponse, actionJournals) = await posReceiptCommand.ExecuteAsync(queue, queueIt, request, new ReceiptResponse(), queueItem);

            var nrSig = receiptResponse.ftSignatures.Where(x => x.Caption == "<receipt-number>").FirstOrDefault();
            var znrSig = receiptResponse.ftSignatures.Where(x => x.Caption == "<z-number>").FirstOrDefault();
            var amntSig = receiptResponse.ftSignatures.Where(x => x.Caption == "<receipt-amount>").FirstOrDefault();
            var tsmpSig = receiptResponse.ftSignatures.Where(x => x.Caption == "<receipt-timestamp>").FirstOrDefault();


            znrSig.Data.Should().Be("0");
            nrSig.Data.Should().Be("245");
            amntSig.Data.Should().Be("9909,98");
            tsmpSig.Data.Should().Be("1999-01-01 00:00:01");

        }
    }
}
