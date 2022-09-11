using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueME.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands.Factories
{
    public class RequestCommandFactory : IRequestCommandFactory
    {
        public RequestCommandFactory() { }

        public RequestCommand Create(ReceiptRequest request)
        {
            //TODO check if ValidTo is in time
            //CorrectiveInvType
            var command = (request.ftReceiptCase & 0xFFFF) switch
            {
                0x0000 => GetPosReceiptCommand(request),
                0x0001 => GetPosReceiptCommand(request),
                0x0002 => _serviceProvider.GetRequiredService<ZeroReceiptCommand>(),
                0x0003 => _serviceProvider.GetRequiredService<InitialOperationReceiptCommand>(),
                0x0004 => _serviceProvider.GetRequiredService<OutOfOperationReceiptCommand>(),
                0x0005 => _serviceProvider.GetRequiredService<MonthlyClosingReceiptCommand>(),
                0x0006 => _serviceProvider.GetRequiredService<YearlyClosingReceiptCommand>(),
                0x0007 => _serviceProvider.GetRequiredService<CashDepositReceiptCommand>(),
                0x0008 => _serviceProvider.GetRequiredService<CashWithdrawalReceiptCommand>(),
                _ => throw new NotImplementedException($"The given receipt case 0x{request.ftReceiptCase:x} is not supported. Please see docs.fiskaltrust.cloud for a list of supported types.")
            };
            return command;
        }

        private RequestCommand GetPosReceiptCommand(ReceiptRequest request)
        {
            return request.IsVoidedComplete()
                ? _serviceProvider.GetRequiredService<CompleteVoidedReceiptCommand>()
                : request.IsVoidedPartial()
                    ? _serviceProvider.GetRequiredService<PartialVoidedReceiptCommand>()
                    : _serviceProvider.GetRequiredService<PosReceiptCommand>();
        }
    }
}