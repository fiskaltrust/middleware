using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Exceptions;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.Factories
{
    public class RequestCommandFactory : IRequestCommandFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public RequestCommandFactory(IServiceProvider serviceCollection) => _serviceProvider = serviceCollection;

        public RequestCommand Create(ReceiptRequest request, ftQueueIT queueIt)
        {
            RequestCommand command = (request.ftReceiptCase & 0xFFFF) switch
            {
                0x0000 => _serviceProvider.GetService<PosReceiptCommand>(),
                0x0001 => _serviceProvider.GetService<PosReceiptCommand>(),  
                0x0003 => ActivatorUtilities.CreateInstance<InitialOperationReceiptCommand>(_serviceProvider, queueIt),
                0x0004 => _serviceProvider.GetService<OutOfOperationReceiptCommand>(),
                0x0007 => _serviceProvider.GetService<DailyClosingReceiptCommand>(),
                0x0005 => _serviceProvider.GetService<MonthlyClosingReceiptCommand>(),
                0x0006 => _serviceProvider.GetService<YearlyClosingReceiptCommand>(),
                _ => throw new UnknownReceiptCaseException(request.ftReceiptCase)
            };
            return command;
        }
    }
}