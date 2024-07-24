using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories;
using fiskaltrust.storage.V0;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Tests.PartitionKey;

public class PartitionKeyTests
{
    [Fact]
    public async Task QueueItemRepository_Should_UseQueueRowAsPartitionKey()
    {
        var queueConfig = new QueueConfiguration { QueueId = Guid.NewGuid() };
        var queueItem = new ftQueueItem
        {
            ftQueueRow = 123456789,
            ftQueueItemId = Guid.NewGuid(),
            cbReceiptReference = "test"
        };

        var referenceTableClient = new Mock<TableClient>(MockBehavior.Loose);
        var referenceTableServiceClient = new Mock<TableServiceClient>(MockBehavior.Loose);
        referenceTableServiceClient.Setup(x => x.GetTableClient(It.IsAny<string>())).Returns(referenceTableClient.Object);
        var receiptReferenceIndexRepository = new AzureTableStorageReceiptReferenceIndexRepository(queueConfig, referenceTableServiceClient.Object);

        var queueItemTableClient = new Mock<TableClient>(MockBehavior.Strict);
        queueItemTableClient
            .Setup(x => x.UpsertEntityAsync(It.Is<TableEntity>(y => y.PartitionKey == Mapper.GetHashString(queueItem.ftQueueRow)), It.IsAny<TableUpdateMode>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult((Azure.Response) null));
        var queueItemTableServiceClient = new Mock<TableServiceClient>(MockBehavior.Loose);
        queueItemTableServiceClient.Setup(x => x.GetTableClient(It.IsAny<string>())).Returns(queueItemTableClient.Object);

        var queueItemRepository = new AzureTableStorageQueueItemRepository(queueConfig, queueItemTableServiceClient.Object, receiptReferenceIndexRepository);
        await queueItemRepository.InsertAsync(queueItem);
    }
}