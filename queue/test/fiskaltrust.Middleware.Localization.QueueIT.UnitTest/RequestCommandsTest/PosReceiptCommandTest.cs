using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using System.Linq;
using Xunit;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using FluentAssertions;
using Moq;
using fiskaltrust.Middleware.Localization.QueueIT.Services;

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
                        Amount = 100,
                        ftChargeItemCase = 0x4954000000000023,
                    },
                    new ChargeItem()
                    {
                        Description = "Discount overeall",
                        Amount = 100,
                        ftChargeItemCase = 0x4954000000000027,
                    }
                },
                cbPayItems = new PayItem[]
                {
                    new PayItem(){
                        Description = "Cash",
                        Amount = 9809.98m,
                        ftPayItemCase = 0x4954000000000001
                    }
                }
            };
            var inMemoryTestScu = new InMemoryTestScu();
   
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddStandardLoggers(LogLevel.Debug);
            var desscdMock = new Mock<IITSSCDProvider>();
            desscdMock.SetupGet( x => x.Instance).Returns(inMemoryTestScu);

            var configRepoMock = new Mock<IReadOnlyConfigurationRepository>();
            configRepoMock.Setup(x => x.GetQueueITAsync(It.IsAny<Guid>())).ReturnsAsync(new ftQueueIT());

            var posReceiptCommand = new PosReceiptCommand(desscdMock.Object, new SignatureItemFactoryIT(), Mock.Of<IJournalITRepository>(), configRepoMock.Object);

            var queue = new ftQueue() { ftQueueId = Guid.NewGuid(), ftReceiptNumerator = 5 };
            var queueItem = new ftQueueItem() { ftQueueId = queue.ftQueueId, ftQueueItemId = Guid.NewGuid(), ftQueueRow = 7 };

            var response = await posReceiptCommand.ExecuteAsync(queue, request, queueItem);

            var znrSig = response.ReceiptResponse.ftSignatures.Where(x => x.Caption == "<z-number>").FirstOrDefault();
            var amntSig = response.ReceiptResponse.ftSignatures.Where(x => x.Caption == "<amount>").FirstOrDefault();
            var tsmpSig = response.ReceiptResponse.ftSignatures.Where(x => x.Caption == "<timestamp>").FirstOrDefault();
            znrSig.Data.Should().Be("245");
            amntSig.Data.Should().Be("9809,98");
            tsmpSig.Data.Should().Be("1999-01-01 00:00:01");

         }
    }
}
