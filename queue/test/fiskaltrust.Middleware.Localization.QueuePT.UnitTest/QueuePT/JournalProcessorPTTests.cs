using System.Text;
using System.Text.Json;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT
{
    public class JournalProcessorPTTests
    {
        [Fact]
        public async Task JournalProcessorPT_ShouldReturnJournalResponse()
        {
            var storageProvider = new Mock<IStorageProvider>();
            var queueItems = new List<ftQueueItem>
            {
                new ftQueueItem
                {
                    request = JsonSerializer.Serialize(ReceiptExamples.CASH_SALES_RECEIPT),
                },
                new ftQueueItem
                {
                    request = JsonSerializer.Serialize(ReceiptExamples.CASH_SALES_RECEIPT),
                },
                new ftQueueItem
                {
                    request = JsonSerializer.Serialize(ReceiptExamples.CASH_SALES_RECEIPT),
                }
            };
            storageProvider.Setup(x => x.GetMiddlewareQueueItemRepository().GetAsync()).ReturnsAsync(queueItems);
            var processor = new JournalProcessorPT(storageProvider.Object);
            var result = processor.ProcessAsync(new JournalRequest());
            var journalResponse = await result.ToListAsync();
            var data = Encoding.UTF8.GetString(journalResponse.SelectMany(x => x.Chunk).ToArray());
        }
    }
}
