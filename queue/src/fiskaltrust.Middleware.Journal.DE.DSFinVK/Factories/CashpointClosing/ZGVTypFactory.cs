using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.CashpointClosing;
using fiskaltrust.ifPOS.v1;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.CashpointClosing
{
    public class ZGVTypFactory
    {
        private readonly DSFinVKParameters _parameters;
        private readonly ILogger _logger;

        public ZGVTypFactory(DSFinVKParameters parameters, ILogger logger)
        {
            _parameters = parameters;
            _logger = logger;
        }

        public IEnumerable<ZGVTyp> Create(IEnumerable<ReceiptRequest> requests, long zNumber, DateTime dailyClosingMoment)
        {
            var chargeItemGroups = requests
                .Where(x => x.cbChargeItems != null)
                .SelectMany(x => x.cbChargeItems.Select(ci => (chargeItem: ci, receiptMoment: x.cbReceiptMoment, request: x))
                .Where(x => x.chargeItem != null))
                .GroupBy(x => new { BusinessCaseType = x.chargeItem.GetBusinessCaseType(_logger, x.request), VatKey = x.chargeItem.GetVATKey(_logger, x.receiptMoment), AgencyId = GetAgencyId(x.chargeItem) });
            var payItemGroups = requests
                .Where(x => x.cbPayItems != null)
                .SelectMany(x => x.cbPayItems.Select(pi => (payItem: pi, receiptMoment: x.cbReceiptMoment, request: x))
                .Where(x => x.payItem != null && x.payItem.MustBeTransformedToBusinessCase()))
                .GroupBy(x => new { BusinessCaseType = x.payItem.GetBusinessCaseType(x.request), VatKey = DSFinVKConstants.VatKeys.NotTaxable, AgencyId = GetAgencyId(x.payItem) });

            var result = new List<ZGVTyp>();
            foreach (var group in chargeItemGroups)
            {
                var gross = group.Sum(x => CalculationHelper.GetAmount(x.chargeItem.ftChargeItemCase, x.chargeItem.Quantity, x.chargeItem.Amount));
                var vat = group.Sum(x => Math.Abs(x.chargeItem.CalculateVAT(_logger, x.receiptMoment)) * CalculationHelper.GetSignOfAmount(x.chargeItem.ftChargeItemCase, x.chargeItem.Quantity, x.chargeItem.Amount));
                result.Add(new ZGVTyp
                {
                    Z_KASSE_ID = _parameters.CashboxIdentification,
                    Z_ERSTELLUNG = dailyClosingMoment,
                    Z_NR = zNumber,
                    GV_TYP = group.Key.BusinessCaseType,
                    GV_NAME = string.Empty,
                    AGENTUR_ID = group.Key.AgencyId,
                    UST_SCHLUESSEL = group.Key.VatKey,
                    Z_UMS_BRUTTO = gross,
                    Z_UMS_NETTO = gross - vat,
                    Z_UST = vat
                });
            }

            foreach (var group in payItemGroups)
            {
                var gross = group.Sum(x => CalculationHelper.InverseAmountOnPayitemReceivable(
                CalculationHelper.GetAmount(x.payItem.ftPayItemCase, x.payItem.Quantity, x.payItem.Amount)));

                var existingZgvTyp = result.FirstOrDefault(x => x.GV_TYP == group.Key.BusinessCaseType && x.AGENTUR_ID == group.Key.AgencyId && x.UST_SCHLUESSEL == group.Key.VatKey);
                if (existingZgvTyp != null)
                {
                    existingZgvTyp.Z_UMS_BRUTTO += gross;
                    existingZgvTyp.Z_UMS_NETTO += gross;
                }
                else
                {
                    result.Add(new ZGVTyp
                    {
                        Z_KASSE_ID = _parameters.CashboxIdentification,
                        Z_ERSTELLUNG = dailyClosingMoment,
                        Z_NR = zNumber,
                        GV_TYP = group.Key.BusinessCaseType,
                        GV_NAME = string.Empty,
                        AGENTUR_ID = group.Key.AgencyId,
                        UST_SCHLUESSEL = group.Key.VatKey,
                        Z_UMS_BRUTTO = gross,
                        Z_UMS_NETTO = gross,
                        Z_UST = 0
                    });
                }
            }

            return result;
        }

        private long GetAgencyId(ChargeItem chargeItem) => AgencyIdProvider.GetIdByGuid(SerializationHelper.GetChargeItemCaseData(chargeItem)?.AgencyId);
        private long GetAgencyId(PayItem payItem) => AgencyIdProvider.GetIdByGuid(SerializationHelper.GetPayItemCaseData(payItem)?.AgencyId);
    }
}
