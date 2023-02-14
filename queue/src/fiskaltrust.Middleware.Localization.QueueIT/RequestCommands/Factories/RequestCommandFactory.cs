using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.Factories
{
    public class RequestCommandFactory : IRequestCommandFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public RequestCommandFactory(IServiceProvider serviceCollection) => _serviceProvider = serviceCollection;

        public RequestCommand Create(ftQueue queue, ftQueueDE queueDE, ReceiptRequest request)
        {
            RequestCommand command = (request.ftReceiptCase & 0xFFFF) switch
            {
                0x0007 => _serviceProvider.GetRequiredService<DailyClosingReceiptCommand>(),
                0x0005 => _serviceProvider.GetRequiredService<MonthlyClosingReceiptCommand>(),
                0x0006 => _serviceProvider.GetRequiredService<YearlyClosingReceiptCommand>(),
                _ => throw new NotImplementedException($"The given receipt case 0x{request.ftReceiptCase:x} is not supported. Please see docs.fiskaltrust.cloud for a list of supported types.")
            };
            return command;
        }
    }
}