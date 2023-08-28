using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Exceptions;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.Middleware.Contracts.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.Factories
{
    public class RequestCommandFactory : IRequestCommandFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public RequestCommandFactory(IServiceProvider serviceCollection) => _serviceProvider = serviceCollection;

        public RequestCommand GetRequestCommandForV2(long receiptCase)
        {
            var casePart = receiptCase & 0xFFFF;
            if (!Enum.IsDefined(typeof(ITReceiptCases), casePart))
            {
                throw new UnknownReceiptCaseException(casePart);
            }

            var itCase = (ITReceiptCases) casePart;
            switch (itCase)
            {
                case ITReceiptCases.UnknownReceipt0x0000:
                case ITReceiptCases.PointOfSaleReceipt0x0001:
                case ITReceiptCases.PaymentTransfer0x0002:
                case ITReceiptCases.PointOfSaleReceiptWithoutObligation0x0003:
                case ITReceiptCases.ECommerce0x0004:
                case ITReceiptCases.Protocol0x0005:
                case ITReceiptCases.InvoiceUnknown0x1000:
                case ITReceiptCases.InvoiceB2C0x1001:
                case ITReceiptCases.InvoiceB2B0x1002:
                case ITReceiptCases.InvoiceB2G0x1003:
                    return _serviceProvider.GetService<GenericSCUReceiptCommand>();
                case ITReceiptCases.ZeroReceipt0x200:
                    return _serviceProvider.GetService<ZeroReceiptCommandIT>();
                case ITReceiptCases.DailyClosing0x2011:
                    return _serviceProvider.GetService<DailyClosingReceiptCommand>();
                case ITReceiptCases.MonthlyClosing0x2012:
                    return _serviceProvider.GetService<MonthlyClosingReceiptCommand>();
                case ITReceiptCases.YearlyClosing0x2013:
                    return _serviceProvider.GetService<YearlyClosingReceiptCommand>();
                case ITReceiptCases.InitialOperationReceipt0x4001:
                    return _serviceProvider.GetService<InitialOperationReceiptCommand>();
                case ITReceiptCases.OutOfOperationReceipt0x4002:
                    return _serviceProvider.GetService<OutOfOperationReceiptCommand>();
                case ITReceiptCases.ShiftClosing0x2010:
                case ITReceiptCases.OneReceipt0x2001:
                case ITReceiptCases.ProtocolUnspecified0x3000:
                case ITReceiptCases.ProtocolTechnicalEvent0x3001:
                case ITReceiptCases.ProtocolAccountingEvent0x3002:
                case ITReceiptCases.InternalUsageMaterialConsumption0x3003:
                case ITReceiptCases.Order0x3004:
                case ITReceiptCases.InitSCUSwitch:
                case ITReceiptCases.FinishSCUSwitch:
                    return _serviceProvider.GetService<QueueOnlyProcessingCommand>();
                default:
                    throw new UnknownReceiptCaseException(casePart);
            }
        }

        public RequestCommand GetRequestCommandForV0(long receiptCase)
        {
            var casePart = receiptCase & 0xFFFF;
            return casePart switch
            {
                0x0000 or 0x0001 => _serviceProvider.GetService<GenericSCUReceiptCommand>(),
                0x0003 => _serviceProvider.GetService<InitialOperationReceiptCommand>(),
                0x0004 => _serviceProvider.GetService<OutOfOperationReceiptCommand>(),
                0x0007 => _serviceProvider.GetService<DailyClosingReceiptCommand>(),
                0x0005 => _serviceProvider.GetService<MonthlyClosingReceiptCommand>(),
                0x0006 => _serviceProvider.GetService<YearlyClosingReceiptCommand>(),
                0x0002 => _serviceProvider.GetService<ZeroReceiptCommandIT>(),
                _ => throw new UnknownReceiptCaseException(casePart),
            };
        }

        public RequestCommand Create(ReceiptRequest request)
        {
            if (request.IsV2Receipt())
            {
                return GetRequestCommandForV2(request.ftReceiptCase);
            }
            else
            {
                return GetRequestCommandForV0(request.ftReceiptCase);
            }
        }
    }
}