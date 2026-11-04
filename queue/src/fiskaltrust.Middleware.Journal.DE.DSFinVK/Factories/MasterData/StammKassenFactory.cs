using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.MasterData;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.MasterData
{
    public class StammKassenFactory
    {
        private readonly ILogger _logger;
        private readonly DSFinVKParameters _parameters;

        public StammKassenFactory(DSFinVKParameters parameters, ILogger logger)
        {
            _parameters = parameters;
            _logger = logger;
        }

        public IEnumerable<StammKasse> Create(MasterDataConfiguration masterData, IList<ReceiptEntry> receiptEntries, long zNumber, DateTime dailyClosingMoment)
        {
            var posSystemIds = receiptEntries
                .Select(x => x.ReceiptRequest)
                .Select(x => x.ftPosSystemId)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct();

            var containsNonDetectableVatId = DailyClosingContainsNonDetectableVatId(receiptEntries);

            foreach (var posSystemId in posSystemIds)
            {
                var posSystem = masterData?.PosSystems?.FirstOrDefault(x => x.PosSystemId.ToString() == posSystemId);
                yield return new StammKasse
                {
                    Z_KASSE_ID = _parameters.CashboxIdentification,
                    Z_ERSTELLUNG = dailyClosingMoment,
                    Z_NR = zNumber,
                    KASSE_BRAND = posSystem?.Brand,
                    KASSE_MODELL = posSystem?.Model,
                    KASSE_SERIENNR = _parameters.CashboxIdentification,
                    KASSE_SW_BRAND = "fiskaltrust.Middleware",
                    KASSE_SW_VERSION = _parameters.MiddlewareVersion,
                    KASSE_BASISWAEH_CODE = posSystem != null ? posSystem.BaseCurrency : "EUR",
                    KEINE_UST_ZUORDNUNG = containsNonDetectableVatId ? "1" : "0"
                };
            }
        }

        private bool DailyClosingContainsNonDetectableVatId(IList<ReceiptEntry> receiptEntries)
        {
            var requests = receiptEntries.Select(x => x.ReceiptRequest);

            var chargeItems = requests
                .Where(x => x.cbChargeItems != null)
                .SelectMany(x => x.cbChargeItems.Select(ci => (chargeItem: ci, receiptMoment: x.cbReceiptMoment))
                .Where(x => x.chargeItem != null));

            return chargeItems.Any(x => x.chargeItem.GetVATKey(_logger, x.receiptMoment) == DSFinVKConstants.VatKeys.NotDetectable);
        }
    }
}
