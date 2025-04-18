using System;
using System.Text.Json;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueES.UnitTest.QueueES.Processors
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
