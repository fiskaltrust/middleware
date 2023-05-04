using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.Factories
{
    public interface IRequestCommandFactory
    {
        public RequestCommand Create(ReceiptRequest request);
    }
}
