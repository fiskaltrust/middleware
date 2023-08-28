using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;
using System;
using fiskaltrust.Middleware.Contracts.Exceptions;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.v2.Receipt;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.v2.Invoice;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.v2.DailyOperations;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.v2.Lifecycle;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.v2.Log;
using fiskaltrust.Middleware.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class ReceiptTypeProcessor
    {
        private readonly IITSSCDProvider _itSSCDProvider;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly ISSCD _signingDevice;
        private readonly ILogger<ZeroReceipt0x200> _logger;
        private readonly IMiddlewareQueueItemRepository _queueItemRepository;

        public ReceiptTypeProcessor(IITSSCDProvider itSSCDProvider, IConfigurationRepository configurationRepository, ISSCD signingDevice, ILogger<ZeroReceipt0x200> logger, IMiddlewareQueueItemRepository queueItemRepository)
        {
            _itSSCDProvider = itSSCDProvider;
            _configurationRepository = configurationRepository;
            _signingDevice = signingDevice;
            _logger = logger;
            _queueItemRepository = queueItemRepository;
        }

        public IReceiptTypeProcessor Create(ReceiptRequest request)
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

        public IReceiptTypeProcessor GetRequestCommandForV2(long receiptCase)
        {
            var casePart = receiptCase & 0xFFFF;
            if (!Enum.IsDefined(typeof(ITReceiptCases), casePart))
            {
                throw new UnknownReceiptCaseException(casePart);
            }

            var itCase = (ITReceiptCases) casePart;
            return itCase switch
            {
                ITReceiptCases.UnknownReceipt0x0000 => new UnknownReceipt0x0000(_itSSCDProvider),
                ITReceiptCases.PointOfSaleReceipt0x0001 => new PointOfSaleReceipt0x0001(_itSSCDProvider),
                ITReceiptCases.PaymentTransfer0x0002 => new PaymentTransfer0x0002(),
                ITReceiptCases.PointOfSaleReceiptWithoutObligation0x0003 => new PointOfSaleReceiptWithoutObligation0x0003(),
                ITReceiptCases.ECommerce0x0004 => new ECommerce0x0004(),
                ITReceiptCases.Protocol0x0005 => new Protocol0x0005(_itSSCDProvider),
                ITReceiptCases.InvoiceUnknown0x1000 => new InvoiceUnknown0x1000(_itSSCDProvider),
                ITReceiptCases.InvoiceB2C0x1001 => new InvoiceB2C0x1001(_itSSCDProvider),
                ITReceiptCases.InvoiceB2B0x1002 => new InvoiceB2B0x1002(_itSSCDProvider),
                ITReceiptCases.InvoiceB2G0x1003 => new InvoiceB2G0x1003(_itSSCDProvider),
                ITReceiptCases.ZeroReceipt0x200 => new ZeroReceipt0x200(_itSSCDProvider, _signingDevice, _logger, _queueItemRepository),
                ITReceiptCases.DailyClosing0x2011 => new DailyClosing0x2011(_itSSCDProvider),
                ITReceiptCases.MonthlyClosing0x2012 => new MonthlyClosing0x2012(_itSSCDProvider),
                ITReceiptCases.YearlyClosing0x2013 => new YearlyClosing0x2013(_itSSCDProvider),
                ITReceiptCases.InitialOperationReceipt0x4001 => new InitialOperationReceipt0x4001(_itSSCDProvider, _configurationRepository),
                ITReceiptCases.OutOfOperationReceipt0x4002 => new OutOfOperationReceipt0x4002(_itSSCDProvider),
                ITReceiptCases.ShiftClosing0x2010 => new ShiftClosing0x2010(),
                ITReceiptCases.OneReceipt0x2001 => new OneReceipt0x2001(),
                ITReceiptCases.ProtocolUnspecified0x3000 => new ProtocolUnspecified0x3000(),
                ITReceiptCases.ProtocolTechnicalEvent0x3001 => new ProtocolTechnicalEvent0x3001(),
                ITReceiptCases.ProtocolAccountingEvent0x3002 => new ProtocolAccountingEvent0x3002(),
                ITReceiptCases.InternalUsageMaterialConsumption0x3003 => new InternalUsageMaterialConsumption0x3003(),
                ITReceiptCases.Order0x3004 => new Order0x3004(),
                ITReceiptCases.InitSCUSwitch0x4011 => new InitSCUSwitch0x4011(),
                ITReceiptCases.FinishSCUSwitch0x4012 => new FinishSCUSwitch0x4012(),
                _ => throw new UnknownReceiptCaseException(casePart),
            };
        }

        public IReceiptTypeProcessor GetRequestCommandForV0(long receiptCase)
        {
            var casePart = receiptCase & 0xFFFF;
            return casePart switch
            {
                0x0000 => new UnknownReceipt0x0000(_itSSCDProvider),
                0x0001 => new PointOfSaleReceipt0x0001(_itSSCDProvider),
                0x0002 => new ZeroReceipt0x200(_itSSCDProvider, _signingDevice, _logger, _queueItemRepository),
                0x0003 => new InitialOperationReceipt0x4001(_itSSCDProvider, _configurationRepository),
                0x0004 => new OutOfOperationReceipt0x4002(_itSSCDProvider),
                0x0005 => new MonthlyClosing0x2012(_itSSCDProvider),
                0x0006 => new YearlyClosing0x2013(_itSSCDProvider),
                0x0007 => new DailyClosing0x2011(_itSSCDProvider),
                _ => throw new UnknownReceiptCaseException(casePart),
            };
        }
    }
}
