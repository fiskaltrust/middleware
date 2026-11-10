using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.MasterData;
using Microsoft.Extensions.Logging;
using static fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants.DSFinVKConstants;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.MasterData
{
    public class StammUstFactory
    {
        private const int NON_TAXABLE_ID = 5;
        private readonly ILogger _logger;
        private readonly DSFinVKParameters _parameters;

        public StammUstFactory(DSFinVKParameters parameters, ILogger logger)
        {
            _parameters = parameters;
            _logger = logger;
        }

        public IEnumerable<StammUst> Create(IList<ReceiptEntry> receiptEntries, long zNumber, DateTime dailyClosingMoment)
        {
            var requests = receiptEntries.Select(x => x.ReceiptRequest);

            var chargeItems = requests
                .Where(x => x.cbChargeItems != null)
                .SelectMany(x => x.cbChargeItems
                    .Select(ci => (chargeItem: ci, receiptMoment: x.cbReceiptMoment))
                    .Where(x => x.chargeItem != null));
            var groupedByVatKey = chargeItems.GroupBy(x => (VatKey: x.chargeItem.GetVATKey(_logger, x.receiptMoment), 
                                                            VatDefinition: x.chargeItem.GetVATDefinition(_logger),
                                                            VatRate: x.chargeItem.GetVATRate(_logger, x.receiptMoment)));

            var payItems = requests
                .Where(x => x.cbPayItems != null)
                .SelectMany(x => x.cbPayItems
                    .Select(pi => (payItem: pi, receiptMoment: x.cbReceiptMoment))
                    .Where(x => x.payItem!= null && x.payItem.MustBeTransformedToBusinessCase()));

            var result = new List<StammUst>();
            foreach (var group in groupedByVatKey)
            {
                var grossAmount = group.Sum(x => x.chargeItem.Amount);
                var vat = group.Sum(x => x.chargeItem.CalculateVAT(_logger, x.receiptMoment));

                result.Add(new StammUst
                {
                    Z_KASSE_ID = _parameters.CashboxIdentification,
                    Z_ERSTELLUNG = dailyClosingMoment,
                    Z_NR = zNumber,
                    UST_SCHLUESSEL = group.Key.VatKey,
                    UST_SATZ = group.Key.VatRate,
                    UST_BESCHR = group.Key.VatDefinition
                });
            }

            if(payItems.Any() && !result.Any(x => x.UST_SCHLUESSEL == NON_TAXABLE_ID))
            {
                result.Add(new StammUst
                {
                    Z_KASSE_ID = _parameters.CashboxIdentification,
                    Z_ERSTELLUNG = dailyClosingMoment,
                    Z_NR = zNumber,
                    UST_SCHLUESSEL = NON_TAXABLE_ID,
                    UST_SATZ = 0,
                    UST_BESCHR = VatDefinition.GetVatDefinition(NON_TAXABLE_ID)
                });
            }

            return result.OrderBy(x => x.UST_SCHLUESSEL);
        }
    }
}
