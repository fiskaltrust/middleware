using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.MasterData;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions;
using fiskaltrust.storage.V0.MasterData;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Helpers;
using fiskaltrust.Middleware.Journal.DE.Common.Models;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.MasterData
{
    public class StammAbschlussFactory
    {
        private readonly DSFinVKParameters _parameters;

        public StammAbschlussFactory(DSFinVKParameters parameters)
        {
            _parameters = parameters;
        }

        public StammAbschluss Create(MasterDataConfiguration masterData, IList<ReceiptEntry> receiptEntries, long zNumber, DateTime dailyClosingMoment)
        {
            var firstResponse = receiptEntries.FirstOrDefault()?.ReceiptResponse;
            var lastResponse = receiptEntries.LastOrDefault()?.ReceiptResponse;

            var payItems = receiptEntries
                .Select(x => x.ReceiptRequest)
                .SelectMany(x => x?.cbPayItems ?? Enumerable.Empty<PayItem>())
                .Where(x => x != null && !x.MustBeTransformedToBusinessCase())
                .Select(x => new { PayItem = x, CaseData = SerializationHelper.GetPayItemCaseData(x) });

            var cashPayItems = payItems.Where(x => x.PayItem.IsCashPaymentType());

            var overallAmount = payItems.Sum(x => x.PayItem.HasForeignCurrency(x.CaseData)
                                                        ? CalculationHelper.GetAmount(x.PayItem.ftPayItemCase, x.PayItem.Quantity, x.CaseData.BaseCurrencyAmount.Value)
                                                        : CalculationHelper.GetAmount(x.PayItem.ftPayItemCase, x.PayItem.Quantity, x.PayItem.Amount));
            var cashAmount = cashPayItems.Sum(x => x.PayItem.HasForeignCurrency(x.CaseData) 
                                                        ? CalculationHelper.GetAmount(x.PayItem.ftPayItemCase, x.PayItem.Quantity, x.CaseData.BaseCurrencyAmount.Value) 
                                                        : CalculationHelper.GetAmount(x.PayItem.ftPayItemCase, x.PayItem.Quantity, x.PayItem.Amount));

            return new StammAbschluss
            {
                Z_KASSE_ID = _parameters.CashboxIdentification,
                Z_ERSTELLUNG = dailyClosingMoment,
                Z_NR = zNumber,
                Z_BUCHUNGSTAG = "",
                TAXONOMIE_VERSION = TaskConstants.Versions.TAXONOMIE,
                Z_START_ID = firstResponse?.ftReceiptIdentification,
                Z_ENDE_ID = lastResponse?.ftReceiptIdentification,
                Z_SE_ZAHLUNGEN = overallAmount,
                Z_SE_BARZAHLUNGEN = cashAmount,
                NAME = masterData?.Account?.AccountName,
                STRASSE = masterData?.Account?.Street,
                PLZ = masterData?.Account?.Zip,
                ORT = masterData?.Account?.City,
                LAND = Conversion.From2DigitsTo3DigitsCountryCode(masterData?.Account?.Country),
                STNR = masterData?.Account?.TaxId,
                USTID = masterData?.Account?.VatId
            };
        }
    }
}
