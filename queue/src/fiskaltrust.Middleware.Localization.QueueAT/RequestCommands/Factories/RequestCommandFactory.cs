using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueAT.Extensions;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueAT.RequestCommands.Factories
{
    public class RequestCommandFactory : IRequestCommandFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public RequestCommandFactory(IServiceProvider serviceCollection) => _serviceProvider = serviceCollection;

        public RequestCommand Create(ftQueue queue, ftQueueAT queueAT, ReceiptRequest request)
        {
            // Queue is not active, and receipt is not initial-operation
            if ((queue.IsNew() || queue.IsDeactivated()) && !request.IsInitialOperationReceipt())
            {
                return _serviceProvider.GetRequiredService<DisabledQueueReceiptCommand>();
            }
            
            return (request.ftReceiptCase & 0xFFFF) switch
            {
                0x0002 => _serviceProvider.GetRequiredService<ZeroReceiptCommand>(),
                0x0003 => _serviceProvider.GetRequiredService<InitialOperationReceiptCommand>(),
                0x0004 => _serviceProvider.GetRequiredService<OutOfOperationReceiptCommand>(),
                0x0005 => _serviceProvider.GetRequiredService<MonthlyClosingReceiptCommand>(),
                0x0006 => _serviceProvider.GetRequiredService<YearlyClosingReceiptCommand>(),
                _ => _serviceProvider.GetRequiredService<PosReceiptCommand>()
            };
        }
    }
}