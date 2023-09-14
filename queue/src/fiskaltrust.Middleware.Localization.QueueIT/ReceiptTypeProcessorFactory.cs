using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.v2.DailyOperations;
using fiskaltrust.Middleware.Localization.QueueIT.v2.Invoice;
using fiskaltrust.Middleware.Localization.QueueIT.v2.Lifecycle;
using fiskaltrust.Middleware.Localization.QueueIT.v2.Log;
using fiskaltrust.Middleware.Localization.QueueIT.v2.Receipt;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class ReceiptTypeProcessorFactory
    {
        private readonly IITSSCD _iITSSCD;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IMiddlewareQueueItemRepository _middlewareQueueItemRepository;
        private readonly IJournalITRepository _journalITRepository;
        private readonly ILogger<ZeroReceipt0x200> _logger;

        public ReceiptTypeProcessorFactory(IITSSCD itSSCD, IConfigurationRepository configurationRepository, IMiddlewareQueueItemRepository middlewareQueueItemRepository, IJournalITRepository journalITRepository, ILogger<ZeroReceipt0x200> logger)
        {
            _configurationRepository = configurationRepository;
            _middlewareQueueItemRepository = middlewareQueueItemRepository;
            _journalITRepository = journalITRepository;
            _logger = logger;
            _iITSSCD = itSSCD;
        }

        public IReceiptTypeProcessor Create(ReceiptRequest request)
        {
            var casePart = request.ftReceiptCase & 0xFFFF;
            if (!Enum.IsDefined(typeof(ITReceiptCases), casePart))
            {
                return null;
            }

            var itCase = (ITReceiptCases) casePart;
            return itCase switch
            {
                ITReceiptCases.UnknownReceipt0x0000 => new UnknownReceipt0x0000(_iITSSCD, _journalITRepository),
                ITReceiptCases.PointOfSaleReceipt0x0001 => new PointOfSaleReceipt0x0001(_iITSSCD, _journalITRepository),
                ITReceiptCases.PaymentTransfer0x0002 => new PaymentTransfer0x0002(),
                ITReceiptCases.PointOfSaleReceiptWithoutObligation0x0003 => new PointOfSaleReceiptWithoutObligation0x0003(),
                ITReceiptCases.ECommerce0x0004 => new ECommerce0x0004(),
                ITReceiptCases.Protocol0x0005 => new Protocol0x0005(_iITSSCD, _journalITRepository),
                ITReceiptCases.InvoiceUnknown0x1000 => new InvoiceUnknown0x1000(),
                ITReceiptCases.InvoiceB2C0x1001 => new InvoiceB2C0x1001(),
                ITReceiptCases.InvoiceB2B0x1002 => new InvoiceB2B0x1002(),
                ITReceiptCases.InvoiceB2G0x1003 => new InvoiceB2G0x1003(),
                ITReceiptCases.ZeroReceipt0x200 => new ZeroReceipt0x200(_iITSSCD, _logger, _configurationRepository, _middlewareQueueItemRepository),
                ITReceiptCases.DailyClosing0x2011 => new DailyClosing0x2011(_iITSSCD, _journalITRepository),
                ITReceiptCases.MonthlyClosing0x2012 => new MonthlyClosing0x2012(_iITSSCD, _journalITRepository),
                ITReceiptCases.YearlyClosing0x2013 => new YearlyClosing0x2013(_iITSSCD, _journalITRepository),
                ITReceiptCases.InitialOperationReceipt0x4001 => new InitialOperationReceipt0x4001(_iITSSCD, _configurationRepository),
                ITReceiptCases.OutOfOperationReceipt0x4002 => new OutOfOperationReceipt0x4002(_iITSSCD, _configurationRepository),
                ITReceiptCases.ShiftClosing0x2010 => new ShiftClosing0x2010(),
                ITReceiptCases.OneReceipt0x2001 => new OneReceipt0x2001(),
                ITReceiptCases.ProtocolUnspecified0x3000 => new ProtocolUnspecified0x3000(),
                ITReceiptCases.ProtocolTechnicalEvent0x3001 => new ProtocolTechnicalEvent0x3001(),
                ITReceiptCases.ProtocolAccountingEvent0x3002 => new ProtocolAccountingEvent0x3002(),
                ITReceiptCases.InternalUsageMaterialConsumption0x3003 => new InternalUsageMaterialConsumption0x3003(),
                ITReceiptCases.Order0x3004 => new Order0x3004(),
                ITReceiptCases.InitSCUSwitch0x4011 => new InitSCUSwitch0x4011(),
                ITReceiptCases.FinishSCUSwitch0x4012 => new FinishSCUSwitch0x4012(),
                _ => null,
            };
        }
    }
}
