using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest.QueueGR.Processors
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
    }
}
