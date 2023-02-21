using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Extensions;
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
                0x0000 => _serviceProvider.ResolveWith<PosReceiptCommand>(queueIt),
                0x0001 => _serviceProvider.ResolveWith<PosReceiptCommand>(queueIt),
                0x0003 => _serviceProvider.ResolveWith<InitialOperationReceiptCommand>(queueIt),
                0x0004 => _serviceProvider.ResolveWith<OutOfOperationReceiptCommand>(queueIt),
                0x0007 => _serviceProvider.ResolveWith<DailyClosingReceiptCommand>(queueIt),
                0x0005 => _serviceProvider.ResolveWith<MonthlyClosingReceiptCommand>(queueIt),
                0x0006 => _serviceProvider.ResolveWith<YearlyClosingReceiptCommand>(queueIt),
                _ => throw new NotImplementedException($"The given receipt case 0x{request.ftReceiptCase:x} is not supported. Please see docs.fiskaltrust.cloud for a list of supported types.")
            };
            return command;
        }
    }
}