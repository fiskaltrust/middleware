using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.CashpointClosing;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Helpers;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.CashpointClosing
{
    public class ZZahlartFactory
    {
        private readonly DSFinVKParameters _parameters;

        public ZZahlartFactory(DSFinVKParameters parameters)
        {
            _parameters = parameters;
        }

        public IEnumerable<ZZahlart> Create(IEnumerable<ReceiptRequest> requests, long zNumber, DateTime dailyClosingMoment)
        {
            var payItems = requests
                .Where(x => x.cbPayItems != null)
                .SelectMany(x => x.cbPayItems)
                .Where(x => x != null && !x.MustBeTransformedToBusinessCase())
                .Select(x => new { PayItem = x, CaseData = SerializationHelper.GetPayItemCaseData(x) });

            var payItemGroups = payItems
                .GroupBy(x => x.PayItem.GetPaymentType())
                .Where(x => x.Key != null);

            var result = new List<ZZahlart>();
            foreach (var group in payItemGroups)
            {
                var amount = group.Where(x => !x.PayItem.HasForeignCurrency(x.CaseData)).Sum(x => CalculationHelper.GetAmount(x.PayItem.ftPayItemCase, x.PayItem.Quantity, x.PayItem.Amount));

                result.Add(new ZZahlart
                {
                    Z_KASSE_ID = _parameters.CashboxIdentification,
                    Z_ERSTELLUNG = dailyClosingMoment,
                    Z_NR = zNumber,
                    ZAHLART_TYP = group.Key,
                    ZAHLART_NAME = string.Empty,  // TODO Support this..sometime
                    Z_ZAHLART_BETRAG = amount
                });
            }

            var foreignAmount = payItems
                .Where(x => x.PayItem.HasForeignCurrency(x.CaseData))
                .Sum(x => CalculationHelper.GetAmount(x.PayItem.ftPayItemCase, x.PayItem.Quantity, x.CaseData.BaseCurrencyAmount.Value));
            var cashResult = result.FirstOrDefault(x => x.ZAHLART_TYP == DSFinVKConstants.PaymentTypes.Cash);

            if (foreignAmount != 0)
            {
                if (cashResult != null)
                {
                    cashResult.Z_ZAHLART_BETRAG += foreignAmount;
                }
                else
                {
                    result.Add(new ZZahlart
                    {
                        Z_KASSE_ID = _parameters.CashboxIdentification,
                        Z_ERSTELLUNG = dailyClosingMoment,
                        Z_NR = zNumber,
                        ZAHLART_TYP = DSFinVKConstants.PaymentTypes.Cash,
                        ZAHLART_NAME = string.Empty,  // TODO Support this..sometime
                        Z_ZAHLART_BETRAG = foreignAmount
                    });
                }
            }

            return result;
        }
    }
}
