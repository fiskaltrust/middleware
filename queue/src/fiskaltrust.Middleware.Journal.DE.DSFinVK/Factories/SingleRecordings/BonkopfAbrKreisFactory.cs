using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.SingleRecordings
{
    public class BonkopfAbrKreisFactory
    {
        private readonly DSFinVKParameters _paramaters;

        public BonkopfAbrKreisFactory(DSFinVKParameters parameters) => _paramaters = parameters;

        public IEnumerable<BonkopfAbrKreis> Create(ReceiptEntry kassenbeleg, long zNumber, DateTime dailyClosingMoment)
        {
            var request = kassenbeleg.ReceiptRequest;
            var response = kassenbeleg.ReceiptResponse;

            if (!string.IsNullOrEmpty(request.cbPreviousReceiptReference))
            {
                yield return new BonkopfAbrKreis
                {
                    Z_KASSE_ID = _paramaters.CashboxIdentification,
                    Z_ERSTELLUNG = dailyClosingMoment,
                    Z_NR = zNumber,
                    BON_ID = response.ftReceiptIdentification,
                    ABRECHNUNGSKREIS = request.cbPreviousReceiptReference
                };
            }
            else if (!string.IsNullOrEmpty(request.cbArea))
            {
                yield return new BonkopfAbrKreis
                {
                    Z_KASSE_ID = _paramaters.CashboxIdentification,
                    Z_ERSTELLUNG = dailyClosingMoment,
                    Z_NR = zNumber,
                    BON_ID = response.ftReceiptIdentification,
                    ABRECHNUNGSKREIS = request.cbArea
                };
            }
            else if (!string.IsNullOrEmpty(kassenbeleg.QueueItem.cbReceiptReference))
            {
                yield return new BonkopfAbrKreis
                {
                    Z_KASSE_ID = _paramaters.CashboxIdentification,
                    Z_ERSTELLUNG = dailyClosingMoment,
                    Z_NR = zNumber,
                    BON_ID = response.ftReceiptIdentification,
                    ABRECHNUNGSKREIS = kassenbeleg.QueueItem.cbReceiptReference
                };
            }
        }
    }
}
