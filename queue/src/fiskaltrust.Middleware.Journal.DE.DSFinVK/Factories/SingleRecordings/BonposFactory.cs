using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings;
using fiskaltrust.ifPOS.v1;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Journal.DE.Common.Models;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.SingleRecordings
{
    public class BonposFactory
    {
        private readonly DSFinVKParameters _parameters;
        private readonly CRC32 _crc32;
        private readonly ILogger _logger;

        public BonposFactory(DSFinVKParameters parameters, ILogger logger)
        {
            _parameters = parameters;
            _crc32 = new CRC32();
             _logger = logger;
        }

        public IEnumerable<Bonpos> Create(ReceiptEntry receiptEntry, long zNumber, DateTime dailyClosingMoment)
        {
            var request = receiptEntry.ReceiptRequest;
            var response = receiptEntry.ReceiptResponse;

            IEnumerable<Bonpos> bonpos;

            if (request.IsReceiptProcessType())
            {
                var bonposFromChargeItems = request.cbChargeItems?.Select((chargeItem, index) => Create(index, chargeItem, request, response, zNumber, dailyClosingMoment)) ?? Enumerable.Empty<Bonpos>();
                var bonposFromPayItems = request.cbPayItems?.Where(x => x.MustBeTransformedToBusinessCase()).Select((payItem, index) => Create(bonposFromChargeItems.Count() + index, payItem, request, response, zNumber, dailyClosingMoment)) ?? Enumerable.Empty<Bonpos>();

                bonpos = bonposFromChargeItems.Concat(bonposFromPayItems).ToList();
                if (request.IsAbortedTransaction())
                {
                    // DSFinV-K 2.4 Tz. 4.2.3 / Anhang I: StartTransaction signs empty processData and UpdateTransaction is not used, so the
                    // positions of an aborted transaction were never signed. They are cancelled during entry: P_STORNO = 1, no second line.
                    foreach (var position in bonpos)
                    {
                        position.P_STORNO = true;
                    }
                }
            }
            else
            {
                if (request.cbChargeItems?.Count() > 0)
                {
                    var bonposFromChargeItems = request.cbChargeItems?.Select((chargeItem, index) => Create(index, chargeItem, request, response, zNumber, dailyClosingMoment)).ToList();
                    bonposFromChargeItems.ForEach(x => x.GV_TYP = DSFinVKConstants.BusinessCaseTypes.Revenue);
                    bonpos = bonposFromChargeItems;
                }
                else
                {
                    bonpos = new List<Bonpos>();
                }
            }
            return bonpos;
        }

        private Bonpos Create(int position, ChargeItem chargeItem, ReceiptRequest request, ReceiptResponse response, long zNumber, DateTime dailyClosingMoment)
        {
            var chargeItemCaseData = SerializationHelper.GetChargeItemCaseData(chargeItem);

            return new Bonpos
            {
                Z_KASSE_ID = _parameters.CashboxIdentification,
                Z_ERSTELLUNG = dailyClosingMoment,
                Z_NR = zNumber,
                BON_ID = response.ftReceiptIdentification,
                POS_ZEILE = position.ToString(CultureInfo.InvariantCulture),
                GUTSCHEIN_NR = chargeItemCaseData?.VoucherNr,
                ARTIKELTEXT = chargeItem.Description,
                POS_TERMINAL_ID = null,  // Removed as of new regulations in DSFinV-K 2.3
                GV_TYP = chargeItem.GetBusinessCaseType(_logger, request),
                GV_NAME = string.Empty,  // TODO Support this..sometime
                P_STORNO = false,
                INHAUS = chargeItem.IsInHouseConsumption(),
                AGENTUR_ID = AgencyIdProvider.GetIdByGuid(chargeItemCaseData?.AgencyId),
                ART_NR = string.IsNullOrEmpty(chargeItem.ProductNumber) ? position.ToString(CultureInfo.InvariantCulture) : chargeItem.ProductNumber,
                GTIN = chargeItem.ProductBarcode,
                WARENGR_ID = chargeItemCaseData?.ProductGroupId ?? _crc32.ComputeHash(chargeItem.ProductGroup)?.ToString(CultureInfo.InvariantCulture),
                WARENGR = chargeItem.ProductGroup,
                MENGE = ReviseQuanityWhenNegative(chargeItem.Quantity, chargeItem.Amount),
                FAKTOR = chargeItem.UnitQuantity,
                EINHEIT = chargeItem.Unit,
                STK_BR = GetPricePerUnit(chargeItem)
            };
        }

        private Bonpos Create(int position, PayItem payItem, ReceiptRequest request, ReceiptResponse response, long zNumber, DateTime dailyClosingMoment)
        {
            var payItemCaseData = SerializationHelper.GetPayItemCaseData(payItem);
            return new Bonpos
            {
                Z_KASSE_ID = _parameters.CashboxIdentification,
                Z_ERSTELLUNG = dailyClosingMoment,
                Z_NR = zNumber,
                BON_ID = response.ftReceiptIdentification,
                POS_ZEILE = position.ToString(CultureInfo.InvariantCulture),
                GUTSCHEIN_NR = payItemCaseData?.VoucherNr,
                ARTIKELTEXT = payItem.Description,
                POS_TERMINAL_ID = null,
                GV_TYP = payItem.GetBusinessCaseType(request),
                GV_NAME = string.Empty,  // TODO Support this..sometime
                P_STORNO = false,
                INHAUS = true,
                AGENTUR_ID = AgencyIdProvider.GetIdByGuid(payItemCaseData?.AgencyId),
                ART_NR = payItem.MoneyNumber,
                GTIN = null,
                WARENGR_ID = payItemCaseData?.MoneyGroupId ?? _crc32.ComputeHash(payItem.MoneyGroup)?.ToString(CultureInfo.InvariantCulture),
                WARENGR = payItem.MoneyGroup,
                MENGE = ReviseQuanityWhenNegative(payItem.Quantity, payItem.InverseAmount()),
                FAKTOR = null,
                EINHEIT = null,
                STK_BR = Math.Abs(payItem.Amount)
            };
        }

        private static decimal GetPricePerUnit(ChargeItem chargeItem)
        {
            if (chargeItem.UnitPrice.HasValue)
            {
                return Math.Abs(chargeItem.UnitPrice.Value);
            }
            else
            {
                if (chargeItem.Quantity == 0)
                {
                    return Math.Abs(chargeItem.Amount);
                }
                else
                {
                    return Math.Abs(chargeItem.Amount) / Math.Abs(chargeItem.Quantity);
                }
            }

        }

        internal static decimal ReviseQuanityWhenNegative(decimal quantity, decimal amount) => quantity < 0 || amount < 0 ? -Math.Abs(quantity) : Math.Abs(quantity);
    }
}
