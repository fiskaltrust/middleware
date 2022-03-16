using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands.Factories
{
    public interface IRequestCommandFactory
    {
        public RequestCommand Create(ftQueue queue, ReceiptRequest request);
    }
}
