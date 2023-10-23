using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueAT.RequestCommands.Factories
{
    public interface IRequestCommandFactory
    {
        public RequestCommand Create(ftQueue queue, ftQueueAT queueAT, ReceiptRequest request);
    }
}
