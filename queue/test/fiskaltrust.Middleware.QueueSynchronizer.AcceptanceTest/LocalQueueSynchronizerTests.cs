using fiskaltrust.Middleware.Contracts;

namespace fiskaltrust.Middleware.QueueSynchronizer.AcceptanceTest
{
    public class LocalQueueSynchronizerTests : AbstractQueueSynchronizerTests
    {
        public override ISignProcessor CreateDecorator(ISignProcessor signProcessor) =>
            new LocalQueueSynchronizationDecorator(signProcessor);
    }
}
