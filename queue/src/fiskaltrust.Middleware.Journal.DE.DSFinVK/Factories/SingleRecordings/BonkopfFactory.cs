using System;
using System.Globalization;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Extensions;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.SingleRecordings
{
    public class BonkopfFactory
    {
        private readonly DSFinVKParameters _parameters;

        public BonkopfFactory(DSFinVKParameters parameters)
        {
            _parameters = parameters;
        }

        public Bonkopf Create(ReceiptEntry kassenbeleg, long zNumber, DateTime dailyClosingMoment)
        {
            var request = kassenbeleg.ReceiptRequest;
            var response = kassenbeleg.ReceiptResponse;

            var customer = SerializationHelper.GetCustomer(request);
            var bonNumber = long.Parse(response.ftReceiptIdentification.Split('#')[0].Substring(2), NumberStyles.HexNumber);

            var receiptCaseData = SerializationHelper.GetReceiptCaseData(request);
            var chargeItemTurnover = request.cbChargeItems != null ? request.cbChargeItems.Where(x => x != null).
                Sum(x => CalculationHelper.GetAmount(x.ftChargeItemCase, x.Quantity, x.Amount)) : 0;
            if (request.IsReceiptProcessType())
            {
                var payItemTurnover = request.cbPayItems != null
                    ? request.cbPayItems
                        .Where(pi => pi != null && pi.MustBeTransformedToBusinessCase())
                        .Select(pi => pi)
                        .Sum(x => CalculationHelper.InverseAmountOnPayitemReceivable(
                        CalculationHelper.GetAmount(x.ftPayItemCase, x.Quantity, x.Amount)))
                    : 0;

                var turnover = request.IsAbortedTransaction() ? 0 : chargeItemTurnover + payItemTurnover;

                return new Bonkopf
                {
                    Z_KASSE_ID = _parameters.CashboxIdentification,
                    Z_ERSTELLUNG = dailyClosingMoment,
                    Z_NR = zNumber,
                    BON_ID = response.ftReceiptIdentification,
                    BON_NR = bonNumber,
                    BON_TYP = request.GetBonType(),
                    BON_NAME = receiptCaseData?.ReceiptName ?? GetDefaultNameIfAVSonstige(kassenbeleg.ReceiptRequest),
                    TERMINAL_ID = null,  // Removed as of new regulations in DSFinV-K 2.3
                    BON_STORNO = request.IsVoidReceipt(),
                    BON_START = response.GetDateTimeFromSignature(SignatureTypesDE.VorgangsBeginn) ?? request.GetReceiptActionStartMoment(),
                    BON_ENDE = response.GetDateTimeFromSignature(SignatureTypesDE.LogTime) ?? request.cbReceiptMoment,
                    BEDIENER_ID = receiptCaseData?.UserId ?? request.cbUser ?? "Nicht angegeben",
                    BEDIENER_NAME = request.cbUser,
                    KUNDE_NAME = customer?.CustomerName,
                    KUNDE_ID = customer?.CustomerId,
                    KUNDE_TYP = customer?.CustomerType,
                    KUNDE_STRASSE = customer?.CustomerStreet,
                    KUNDE_PLZ = customer?.CustomerZip,
                    KUNDE_ORT = customer?.CustomerCity,
                    KUNDE_LAND = Conversion.From2DigitsTo3DigitsCountryCode(customer?.CustomerCountry),
                    KUNDE_USTID = customer?.CustomerVATId,
                    BON_NOTIZ = receiptCaseData?.ReceiptNote,
                    UMS_BRUTTO = turnover
                };
            }
            if (_parameters.IncludeOrders)
            {
                return new Bonkopf
                {
                    Z_KASSE_ID = _parameters.CashboxIdentification,
                    Z_ERSTELLUNG = dailyClosingMoment,
                    Z_NR = zNumber,
                    BON_ID = response.ftReceiptIdentification,
                    BON_NR = bonNumber,
                    BON_NAME = receiptCaseData?.ReceiptName ?? GetDefaultNameIfAVSonstige(kassenbeleg.ReceiptRequest),
                    BON_TYP = request.GetBonType(),
                    BON_START = response.GetDateTimeFromSignature(SignatureTypesDE.VorgangsBeginn) ?? request.GetReceiptActionStartMoment(),
                    BON_ENDE = response.GetDateTimeFromSignature(SignatureTypesDE.LogTime) ?? request.cbReceiptMoment,
                    BEDIENER_ID = receiptCaseData?.UserId ?? request.cbUser ?? "Nicht angegeben",
                    BON_NOTIZ = receiptCaseData?.ReceiptNote,
                    // The charge items of other actions are exported to Bonpos, so the Bonkopf holds their cumulated value (DSFinV-K 2.4 Tz. 3.1.2).
                    UMS_BRUTTO = chargeItemTurnover
                };
            }
            return null;
        }

        private string GetDefaultNameIfAVSonstige(ReceiptRequest request)
                 => request.GetBonType() == DSFinVKConstants.BonTypes.OtherActionElse ? request.GetDescription() : null;

    }
}
