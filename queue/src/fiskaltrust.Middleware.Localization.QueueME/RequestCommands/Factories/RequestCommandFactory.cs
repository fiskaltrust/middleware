using System;
using fiskaltrust.ifPOS.v1;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands.Factories
{
    public class RequestCommandFactory : IRequestCommandFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public RequestCommandFactory(IServiceProvider serviceCollection) => _serviceProvider = serviceCollection;

        public RequestCommand Create(ReceiptRequest request)
        {
            //TODO check if ValidTo is in time
            //CorrectiveInvType
            //Voucher

            RequestCommand command = (request.ftReceiptCase & 0xFFFF) switch
            {
                0x0003 => _serviceProvider.GetRequiredService<InitialOperationReceiptCommand>(),
                0x0004 => _serviceProvider.GetRequiredService<OutOfOperationReceiptCommand>(),
                _ => null
            };
            return command;

        }
    }
}