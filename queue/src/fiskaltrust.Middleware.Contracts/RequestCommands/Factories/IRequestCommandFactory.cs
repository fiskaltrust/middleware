using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Contracts.RequestCommands.Factories
{
    public interface IRequestCommandFactory
    {
        public RequestCommand Create(ReceiptRequest request);
    }
}
