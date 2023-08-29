using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;
using System;
using fiskaltrust.Middleware.Contracts.Exceptions;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.v2.DailyOperations;
using fiskaltrust.Middleware.Localization.QueueIT.v2.Invoice;
using fiskaltrust.Middleware.Localization.QueueIT.v2.Receipt;
using fiskaltrust.Middleware.Localization.QueueIT.v2.Lifecycle;
using fiskaltrust.Middleware.Localization.QueueIT.v2.Log;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class ReceiptTypeProcessorFactory
    {
        private readonly IITSSCDProvider _itSSCDProvider;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly ILogger<ZeroReceipt0x200> _logger;

        public ReceiptTypeProcessorFactory(IITSSCDProvider itSSCDProvider, IConfigurationRepository configurationRepository, ILogger<ZeroReceipt0x200> logger)
        {
            _itSSCDProvider = itSSCDProvider;
            _configurationRepository = configurationRepository;
            _logger = logger;
        }

        public IReceiptTypeProcessor Create(ReceiptRequest request)
        {
            if (request.IsV2Receipt())
            {
                return GetRequestCommandForV2(request.ftReceiptCase);
            }
            else
            {
                var v2Case = GetV2CaseForV0(request.ftReceiptCase);
                return GetRequestCommandForV2((long) v2Case);
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
                ITReceiptCases.ZeroReceipt0x200 => new ZeroReceipt0x200(_itSSCDProvider, _logger, _configurationRepository),
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

        public long GetV2CaseForV0(long receiptCase)
        {
            var casePart = receiptCase & 0xFFFF;
            return casePart switch
            {
                0x0000 => (long) ITReceiptCases.UnknownReceipt0x0000,
                0x0001 => (long) ITReceiptCases.PointOfSaleReceipt0x0001,
                0x0002 => (long) ITReceiptCases.ZeroReceipt0x200,
                0x0003 => (long) ITReceiptCases.InitialOperationReceipt0x4001,
                0x0004 => (long) ITReceiptCases.OutOfOperationReceipt0x4002,
                0x0005 => (long) ITReceiptCases.MonthlyClosing0x2012,
                0x0006 => (long) ITReceiptCases.YearlyClosing0x2013,
                0x0007 => (long) ITReceiptCases.DailyClosing0x2011,
                _ => casePart
            };
        }
    }
}
