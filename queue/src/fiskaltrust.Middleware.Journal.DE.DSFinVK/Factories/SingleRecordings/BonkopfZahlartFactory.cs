using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Helpers;
using fiskaltrust.Middleware.Journal.DE.Common.Models;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.SingleRecordings
{
    public class BonkopfZahlartFactory
    {
        private readonly DSFinVKParameters _parameters;

        public BonkopfZahlartFactory(DSFinVKParameters parameters)
        {
            _parameters = parameters;
        }

        public IEnumerable<BonkopfZahlart> Create(ReceiptEntry kassenbeleg, long zNumber, DateTime dailyClosingMoment)
        {
            var request = kassenbeleg.ReceiptRequest;
            var response = kassenbeleg.ReceiptResponse;

            if (request.IsReceiptProcessType() && !request.IsPaymentTypeNoneOnly())
            {

                var payItemGroups = (request.cbPayItems ?? Enumerable.Empty<PayItem>())
                .Where(x => x != null && !x.MustBeTransformedToBusinessCase())
                .Select(x => (payItem: x, payItemCaseData: SerializationHelper.GetPayItemCaseData(x)))
                .GroupBy(x => new
                {
                    PaymentType = x.payItem.GetPaymentType(),
                    PaymentName = x.payItemCaseData is { ItemCaseName: { } } ? x.payItemCaseData.ItemCaseName : x.payItem.Description,
                    CurrencyCode = x.payItemCaseData?.CurrencyCode
                })
                .Where(x => x.Key.PaymentType != null);

                foreach (var group in payItemGroups)
                {
                    var baseCurrencyAmount = !string.IsNullOrEmpty(group.Key.CurrencyCode)
                        ? group.Sum(x => x.payItemCaseData != null && x.payItemCaseData.BaseCurrencyAmount.HasValue ? CalculationHelper.GetAmount(x.payItem.ftPayItemCase, x.payItem.Quantity, x.payItemCaseData.BaseCurrencyAmount.Value) : 0)
                        : group.Sum(x => CalculationHelper.GetAmount(x.payItem.ftPayItemCase, x.payItem.Quantity, x.payItem.Amount));
                    var foreignCurrencyAmount = group.Sum(x => x.payItemCaseData != null && x.payItemCaseData.ForeignCurrencyAmount.HasValue ? CalculationHelper.GetAmount(x.payItem.ftPayItemCase, x.payItem.Quantity, x.payItemCaseData.ForeignCurrencyAmount.Value) : 0);

                    yield return new BonkopfZahlart
                    {
                        Z_KASSE_ID = _parameters.CashboxIdentification,
                        Z_ERSTELLUNG = dailyClosingMoment,
                        Z_NR = zNumber,
                        BON_ID = response.ftReceiptIdentification,
                        ZAHLART_TYP = group.Key.PaymentType,
                        ZAHLART_NAME = group.Key.PaymentName,
                        ZAHLWAEH_CODE = group.Key.CurrencyCode,
                        BASISWAEH_BETRAG = baseCurrencyAmount != 0 ? baseCurrencyAmount : null,
                        ZAHLWAEH_BETRAG = foreignCurrencyAmount != 0 ? foreignCurrencyAmount : null
                    };
                }
            }
            else
            {
                yield return new BonkopfZahlart
                {
                    Z_KASSE_ID = _parameters.CashboxIdentification,
                    Z_ERSTELLUNG = dailyClosingMoment,
                    Z_NR = zNumber,
                    BON_ID = response.ftReceiptIdentification,
                    ZAHLART_TYP = "Keine",
                    BASISWAEH_BETRAG = 0
                };
            }
        }
    }
}
