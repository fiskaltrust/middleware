using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands.Factories
{
    public class RequestCommandFactory : IRequestCommandFactory
    {
        public RequestCommandFactory() { }

        public RequestCommand Create(ReceiptRequest request)
        {
            throw new NotImplementedException($"The given receipt case 0x{request.ftReceiptCase:x} could not be processed by the Middleware.");
        }
    }
}