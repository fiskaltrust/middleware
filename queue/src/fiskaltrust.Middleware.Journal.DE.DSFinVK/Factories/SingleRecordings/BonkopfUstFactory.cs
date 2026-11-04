using System;
using System.Collections.Generic;
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
    public class BonkopfUstFactory
    {
        private readonly DSFinVKParameters _parameters;
        private readonly ILogger _logger;

        public BonkopfUstFactory(DSFinVKParameters parameters, ILogger logger)
        {
            _parameters = parameters;
            _logger = logger;
        }

        public IEnumerable<BonkopfUst> Create(ReceiptEntry kassenbeleg, long zNumber, DateTime dailyClosingMoment)
        {
            var request = kassenbeleg.ReceiptRequest;
            var response = kassenbeleg.ReceiptResponse;
            var result = new List<BonkopfUst>();

            if (request.IsReceiptProcessType())
            {
                result.AddRange(CreateFromChargeItems(request, response, zNumber, dailyClosingMoment));

                var nonTaxable = result.FirstOrDefault(x => x.UST_SCHLUESSEL == DSFinVKConstants.VatKeys.NotTaxable);
                var bonkopfUstFromTransformedPayItems = CreateFromTransformedPayItems(request, response, zNumber, dailyClosingMoment);
                if (nonTaxable != null && bonkopfUstFromTransformedPayItems.BON_BRUTTO != 0)
                {
                    nonTaxable.BON_BRUTTO += bonkopfUstFromTransformedPayItems.BON_BRUTTO;
                    nonTaxable.BON_NETTO += bonkopfUstFromTransformedPayItems.BON_NETTO;
                }
                else if (bonkopfUstFromTransformedPayItems.BON_BRUTTO != 0)
                {
                    result.Add(bonkopfUstFromTransformedPayItems);
                }

                if (request.IsAbortedTransaction())
                {
                    foreach (var bonkopfUst in result)
                    {
                        bonkopfUst.BON_BRUTTO = 0;
                        bonkopfUst.BON_NETTO = 0;
                        bonkopfUst.BON_UST = 0;
                    }
                }
            }
            else if (request.cbChargeItems?.Any(x => x != null) == true)
            {
                // Other actions (e.g. AVSonstige info-internal, AVBestellung) are exported with their charge items in Bonpos/Bonpos_USt,
                // so Bonkopf_USt holds the cumulated position values to keep Bonkopf and Bonpos reconcilable (DSFinV-K 2.4 Tz. 3.1.2).
                result.AddRange(CreateFromChargeItems(request, response, zNumber, dailyClosingMoment));
            }
            // Other actions without charge items (e.g. daily closing) have no amounts to split by VAT, so no Bonkopf_USt record is written:
            // an empty record would carry UST_SCHLUESSEL 0, which does not exist in Stamm_USt.
            return result;
        }

        private IEnumerable<BonkopfUst> CreateFromChargeItems(ReceiptRequest request, ReceiptResponse response, long zNumber, DateTime dailyClosingMoment)
        {
            var groupedByVatKey = request.cbChargeItems?.Where(x => x != null).GroupBy(x => x.GetVATKey(_logger, request.cbReceiptMoment)) ?? Enumerable.Empty<IGrouping<int, ChargeItem>>();
            foreach (var group in groupedByVatKey)
            {
                var grossAmount = group.Sum(x => CalculationHelper.GetAmount(x.ftChargeItemCase, x.Quantity, x.Amount));
                var vat = group.Sum(x => Math.Abs(x.CalculateVAT(_logger, request.cbReceiptMoment)) * CalculationHelper.GetSignOfAmount(x.ftChargeItemCase, x.Quantity, x.Amount));

                yield return new BonkopfUst
                {
                    Z_KASSE_ID = _parameters.CashboxIdentification,
                    Z_ERSTELLUNG = dailyClosingMoment,
                    Z_NR = zNumber,
                    BON_ID = response.ftReceiptIdentification,
                    UST_SCHLUESSEL = group.Key,
                    BON_BRUTTO = grossAmount,
                    BON_NETTO = grossAmount - vat,
                    BON_UST = vat
                };
            }
        }

        private BonkopfUst CreateFromTransformedPayItems(ReceiptRequest request, ReceiptResponse lastResponse, long zNumber, DateTime dailyClosingMoment)
        {
            var sum = request.cbPayItems?
                .Where(x => x.MustBeTransformedToBusinessCase())
                .Sum(x => CalculationHelper.InverseAmountOnPayitemReceivable(
                    CalculationHelper.GetAmount(x.ftPayItemCase, x.Quantity, x.Amount)))
                ?? 0;

            return new BonkopfUst
            {
                Z_KASSE_ID = _parameters.CashboxIdentification,
                Z_ERSTELLUNG = dailyClosingMoment,
                Z_NR = zNumber,
                BON_ID = lastResponse.ftReceiptIdentification,
                UST_SCHLUESSEL = DSFinVKConstants.VatKeys.NotTaxable,
                BON_BRUTTO = sum,
                BON_NETTO = sum,
                BON_UST = 0
            };
        }
    }
}
