using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands.Factories
{
    public class RequestCommandFactory : IRequestCommandFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public RequestCommandFactory(IServiceProvider serviceCollection) => _serviceProvider = serviceCollection;

        public RequestCommand Create(ftQueue queue, ReceiptRequest request)
        {
            throw new NotImplementedException($"The given receipt case 0x{request.ftReceiptCase:x} could not be processed by the Middleware.");
        }
    }
}