using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.CashpointClosing;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Helpers;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.CashpointClosing
{
    public class ZWaehrungenFactory
    {
        private const string MAIN_CURRENCY = "EUR";

        private readonly DSFinVKParameters _parameters;

        public ZWaehrungenFactory(DSFinVKParameters parameters)
        {
            _parameters = parameters;
        }

        public IEnumerable<ZWaehrung> Create(IEnumerable<ReceiptRequest> requests, long zNumber, DateTime dailyClosingMoment)
        {
            var payItems = requests
                .Where(x => x.cbPayItems != null)
                .SelectMany(x => x.cbPayItems)
                .Where(x => x != null && x.IsCashPaymentType())
                .Select(x => new { PayItem = x, CaseData = SerializationHelper.GetPayItemCaseData(x) });

            var mainCurrencyAmount = payItems.Where(x => !x.PayItem.HasForeignCurrency(x.CaseData))
                                .Sum(x => CalculationHelper.GetAmount(x.PayItem.ftPayItemCase, x.PayItem.Quantity, x.PayItem.Amount));

            yield return new ZWaehrung
            {
                Z_KASSE_ID = _parameters.CashboxIdentification,
                Z_ERSTELLUNG = dailyClosingMoment,
                Z_NR = zNumber,
                ZAHLART_WAEH = MAIN_CURRENCY,
                ZAHLART_BETRAG_WAEH = mainCurrencyAmount
            };

            var payItemGroups = payItems
                .GroupBy(x => x.CaseData?.CurrencyCode)
                .Where(x => x.Key != null);

            foreach (var group in payItemGroups)
            {
                var amount = group.Sum(x => CalculationHelper.GetAmount(x.PayItem.ftPayItemCase,x.PayItem.Quantity, x.CaseData.ForeignCurrencyAmount ?? 0));

                yield return new ZWaehrung
                {
                    Z_KASSE_ID = _parameters.CashboxIdentification,
                    Z_ERSTELLUNG = dailyClosingMoment,
                    Z_NR = zNumber,
                    ZAHLART_WAEH = group.Key,
                    ZAHLART_BETRAG_WAEH = amount
                };
            }
        }           
    }
}
