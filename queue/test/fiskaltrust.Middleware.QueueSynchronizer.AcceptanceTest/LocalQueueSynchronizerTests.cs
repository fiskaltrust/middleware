using fiskaltrust.Middleware.Contracts;
using Microsoft.Extensions.Logging;
using Moq;

namespace fiskaltrust.Middleware.QueueSynchronizer.AcceptanceTest
{
    public class LocalQueueSynchronizerTests : AbstractQueueSynchronizerTests
    {
        public override ISignProcessor CreateDecorator(ISignProcessor signProcessor) =>
            new LocalQueueSynchronizationDecorator(signProcessor, Mock.Of<ILogger<LocalQueueSynchronizationDecorator>>());
    }
}
