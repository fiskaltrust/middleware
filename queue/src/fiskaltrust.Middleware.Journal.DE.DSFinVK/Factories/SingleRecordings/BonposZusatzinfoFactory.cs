using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Extensions;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Helpers;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.SingleRecordings
{
    public class BonposZusatzinfoFactory
    {
        private readonly ILogger _logger;
        private readonly DSFinVKParameters _parameters;
        private readonly CRC32 _crc32;

        public BonposZusatzinfoFactory(DSFinVKParameters parameters, ILogger logger)
        {
            _parameters = parameters;
            _crc32 = new CRC32();
            _logger = logger;
        }

        public IEnumerable<BonposZusatzinfo> Create(ReceiptEntry receiptEntry, long zNumber, DateTime dailyClosingMoment)
        {
            var request = receiptEntry.ReceiptRequest;
            var response = receiptEntry.ReceiptResponse;

            if (request.IsReceiptProcessType())
            {
                return request.cbChargeItems?
                .SelectMany((chargeItem, index) => Create(index, chargeItem, response, request.cbReceiptMoment, zNumber, dailyClosingMoment))
                ?? Enumerable.Empty<BonposZusatzinfo>();
            }
            else
            {
                return Enumerable.Empty<BonposZusatzinfo>();
            }
        }

        private IEnumerable<BonposZusatzinfo> Create(int position, ChargeItem chargeItem, ReceiptResponse response, DateTime cbReceiptMoment, long zNumber, DateTime dailyClosingMoment)
        {
            if (string.IsNullOrEmpty(chargeItem?.ftChargeItemCaseData))
            {
                yield break;
            }

            var chargeItemCaseData = SerializationHelper.GetChargeItemCaseData(chargeItem);

            foreach (var subItem in chargeItemCaseData?.SubItems ?? Enumerable.Empty<ChargeItemCaseDataSubItem>())
            {
                var amount = CalculationHelper.GetAmount(subItem.ftSubChargeItemCase, subItem.Quantity, subItem.Amount);
                var vat = Math.Abs(subItem.CalculateVAT(_logger, chargeItem.VATRate, chargeItem.Moment ?? cbReceiptMoment)) * CalculationHelper.GetSignOfAmount(subItem.ftSubChargeItemCase,subItem.Quantity, subItem.Amount);

                var productGroupId = subItem.ProductGroup?.ProductGroupId 
                    ?? _crc32.ComputeHash(subItem.ProductGroup?.ProductGroupName)?.ToString(CultureInfo.InvariantCulture) 
                    ?? _crc32.ComputeHash(chargeItem.ProductGroup)?.ToString(CultureInfo.InvariantCulture);

                yield return new BonposZusatzinfo
                {
                    Z_KASSE_ID = _parameters.CashboxIdentification,
                    Z_ERSTELLUNG = dailyClosingMoment,
                    Z_NR = zNumber,
                    BON_ID = response.ftReceiptIdentification,
                    POS_ZEILE = position.ToString(CultureInfo.InvariantCulture),
                    ZI_ART_NR = subItem.ProductNumber,
                    ZI_GTIN = subItem.GTIN,
                    ZI_NAME = subItem.Description,
                    ZI_WARENGR = subItem.ProductGroup?.ProductGroupName,
                    ZI_WARENGR_ID = productGroupId,
                    ZI_MENGE = subItem.Quantity,
                    ZI_FAKTOR = subItem.UnitQuantity,
                    ZI_EINHEIT = subItem.Unit,
                    ZI_UST_SCHLUESSEL = subItem.ftSubChargeItemCase.GetVATKey(_logger, chargeItem.VATRate, chargeItem.Moment ?? cbReceiptMoment),
                    ZI_BASISPREIS_BRUTTO = amount,
                    ZI_BASISPREIS_NETTO = amount - vat,
                    ZI_BASISPREIS_UST = vat
                };
            }
        }
    }
}
