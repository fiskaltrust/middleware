using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings;
using fiskaltrust.ifPOS.v1;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.SingleRecordings
{
    public class BonposUstFactory
    {
        private readonly DSFinVKParameters _parameters;
        private readonly ILogger _logger;

        public BonposUstFactory(DSFinVKParameters parameters, ILogger logger)
        {
            _parameters = parameters;
            _logger = logger;
        }

        public IEnumerable<BonposUst> Create(ReceiptEntry receiptEntry, long zNumber, DateTime dailyClosingMoment)
        {
            var request = receiptEntry.ReceiptRequest;
            var response = receiptEntry.ReceiptResponse;

            IEnumerable<BonposUst> bonposUst;
            if (request.IsReceiptProcessType())
            {
                var bonposUstFromChargeItems = request.cbChargeItems?
                .Select((chargeItem, index) => Create(index, chargeItem, response, request.cbReceiptMoment, zNumber, dailyClosingMoment))
                ?? Enumerable.Empty<BonposUst>();
                var bonposUstFromPayItems = request.cbPayItems?
                    .Where(x => x != null && x.MustBeTransformedToBusinessCase())
                    .Select((chargeItem, index) => Create(index + bonposUstFromChargeItems.Count(), chargeItem, response, request, zNumber, dailyClosingMoment))
                    ?? Enumerable.Empty<BonposUst>();
                bonposUst = bonposUstFromChargeItems.Concat(bonposUstFromPayItems);
            }
            else
            {
                if (request.cbChargeItems?.Count() > 0)
                {
                    bonposUst = request.cbChargeItems?.Select((chargeItem, index) => Create(index, chargeItem, response, request.cbReceiptMoment, zNumber, dailyClosingMoment)).ToList();
                }
                else
                {
                    bonposUst = new List<BonposUst>();
                }
            }
            return bonposUst;
        }

        private BonposUst Create(int position, ChargeItem chargeItem, ReceiptResponse response, DateTime cbReceiptMoment, long zNumber, DateTime dailyClosingMoment) 
        {
            var amount = CalculationHelper.GetAmount(chargeItem.ftChargeItemCase,chargeItem.Quantity, chargeItem.Amount);
            var vat = Math.Abs(chargeItem.CalculateVAT(_logger, cbReceiptMoment)) * CalculationHelper.GetSignOfAmount(chargeItem.ftChargeItemCase, chargeItem.Quantity, chargeItem.Amount);

            return new BonposUst
            {
                Z_KASSE_ID = _parameters.CashboxIdentification,
                Z_ERSTELLUNG = dailyClosingMoment,
                Z_NR = zNumber,
                BON_ID = response.ftReceiptIdentification,
                POS_ZEILE = position.ToString(CultureInfo.InvariantCulture),
                UST_SCHLUESSEL = chargeItem.GetVATKey(_logger, cbReceiptMoment),
                POS_BRUTTO = amount,
                POS_NETTO = amount - vat,
                POS_UST = vat
            };
        }

        private BonposUst Create(int position, PayItem payItem, ReceiptResponse response, ReceiptRequest request, long zNumber, DateTime dailyClosingMoment)
        {
            var amount = payItem.InverseAmount();

            return new BonposUst
            {
                Z_KASSE_ID = _parameters.CashboxIdentification,
                Z_ERSTELLUNG = dailyClosingMoment,
                Z_NR = zNumber,
                BON_ID = response.ftReceiptIdentification,
                POS_ZEILE = position.ToString(CultureInfo.InvariantCulture),
                UST_SCHLUESSEL = DSFinVKConstants.VatKeys.NotTaxable,
                POS_BRUTTO = amount,
                POS_NETTO = amount,
                POS_UST = 0
            };
        }
    }
}
