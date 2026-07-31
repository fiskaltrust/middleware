using System;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
using Moq;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest.Processors
{
    public static class TestHelpers
    {
        public static ftQueue CreateQueue()
        {
            return new ftQueue
            {
                ftQueueId = Guid.NewGuid(),
            };
        }

        public static ftQueueItem CreateQueueItem()
        {
            return new ftQueueItem
            {
                ftQueueId = Guid.NewGuid(),
                ftQueueItemId = Guid.NewGuid(),
            };
        }

        /// <summary>
        /// Lazily-resolved IConfigurationRepository whose GetQueueGRAsync returns a fresh
        /// ftQueueGR for any queue id and accepts InsertOrUpdateQueueGRAsync writes.
        /// Use this when a processor takes the repo but the test doesn't assert on counter
        /// state.
        /// </summary>
        public static AsyncLazy<IConfigurationRepository> CreateConfigurationRepositoryStub(ftQueueGR? queueGR = null)
        {
            var repo = new Mock<IConfigurationRepository>();
            repo.Setup(x => x.GetQueueGRAsync(It.IsAny<Guid>()))
                .ReturnsAsync(queueGR ?? new ftQueueGR());
            repo.Setup(x => x.InsertOrUpdateQueueGRAsync(It.IsAny<ftQueueGR>()))
                .Returns(Task.CompletedTask);
            return new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(repo.Object));
        }

        /// <summary>
        /// Lazily-resolved IMiddlewareQueueItemRepository whose GetByQueueRowAsync serves
        /// the given queue items by ftQueueRow (null for rows without an item). Pass no
        /// items for processors under test that never need the queue-item history.
        /// </summary>
        public static AsyncLazy<IMiddlewareQueueItemRepository> CreateQueueItemRepositoryStub(params ftQueueItem[] queueItems)
        {
            var repo = new Mock<IMiddlewareQueueItemRepository>();
            repo.Setup(x => x.GetByQueueRowAsync(It.IsAny<long>()))
                .ReturnsAsync((long row) => queueItems.FirstOrDefault(x => x.ftQueueRow == row));
            return new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repo.Object));
        }
    }
}
