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
                0x0000 => _serviceProvider.GetRequiredService<PosReceiptCommand>(),
                0x0001 => _serviceProvider.GetRequiredService<PosReceiptCommand>(),
                0x0002 => _serviceProvider.GetRequiredService<ZeroReceiptCommand>(),
                0x0003 => _serviceProvider.GetRequiredService<InitialOperationReceiptCommand>(),
                0x0004 => _serviceProvider.GetRequiredService<OutOfOperationReceiptCommand>(),
                0x0005 => _serviceProvider.GetRequiredService<MonthlyClosingReceiptCommand>(),
                0x0006 => _serviceProvider.GetRequiredService<YearlyClosingReceiptCommand>(),
                0x0007 => _serviceProvider.GetRequiredService<CashDepositReceiptCommand>(),
                0x0008 => _serviceProvider.GetRequiredService<CashWithdrawlReceiptCommand>(),
                _ => null
            };
            return command;
        }
    }
}