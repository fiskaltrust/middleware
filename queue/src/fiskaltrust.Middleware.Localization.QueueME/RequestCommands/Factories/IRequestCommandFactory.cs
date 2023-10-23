using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands.Factories
{
    public interface IRequestCommandFactory
    {
        public RequestCommand Create(ReceiptRequest request);
    }
}
