using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Exceptions;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.Middleware.Contracts.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands;
using Microsoft.Extensions.DependencyInjection;
using DailyClosingReceiptCommand = fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands.DailyClosingReceiptCommand;
using InitialOperationReceiptCommand = fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands.InitialOperationReceiptCommand;
using MonthlyClosingReceiptCommand = fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands.MonthlyClosingReceiptCommand;
using OutOfOperationReceiptCommand = fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands.OutOfOperationReceiptCommand;
using YearlyClosingReceiptCommand = fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands.YearlyClosingReceiptCommand;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.Factories
{
    public class RequestCommandFactory : IRequestCommandFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public RequestCommandFactory(IServiceProvider serviceCollection) => _serviceProvider = serviceCollection;

        public RequestCommand Create(ReceiptRequest request)
        {
            RequestCommand command = (request.ftReceiptCase & 0xFFFF) switch
            {
                0x0000 => _serviceProvider.GetService<PosReceiptCommand>(),
                0x0001 => _serviceProvider.GetService<PosReceiptCommand>(),
                0x0003 => _serviceProvider.GetService<InitialOperationReceiptCommand>(),
                0x0004 => _serviceProvider.GetService<OutOfOperationReceiptCommand>(),
                0x0007 => _serviceProvider.GetService<DailyClosingReceiptCommand>(),
                0x0005 => _serviceProvider.GetService<MonthlyClosingReceiptCommand>(),
                0x0006 => _serviceProvider.GetService<YearlyClosingReceiptCommand>(),
                0x0002 => _serviceProvider.GetService<ZeroReceiptCommandDEFAULT>(),
                _ => throw new UnknownReceiptCaseException(request.ftReceiptCase)
            };

            return command;
        }
    }
}