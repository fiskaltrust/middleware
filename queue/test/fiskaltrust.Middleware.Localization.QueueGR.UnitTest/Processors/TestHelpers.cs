using System;
using System.Threading.Tasks;
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
    }
}
