using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.SingleRecordings
{
    public class BonReferenzFactory
    {
        private readonly DSFinVKParameters _parameters;

        public BonReferenzFactory(DSFinVKParameters parameters) => _parameters = parameters;

        public IEnumerable<BonReferenz> Create(List<ReceiptReferencesGroupedData> receiptReferences)
        {
            foreach (var receiptEntry in receiptReferences)
            {
                if (string.IsNullOrEmpty(receiptEntry.SourceReceiptIdentification))
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(receiptEntry.TargetReceiptCaseData) && !receiptEntry.SourceQueueItemId.HasValue)
                {
                    ReceiptCaseData targetReceiptCaseData;
                    try
                    {
                        targetReceiptCaseData = JsonConvert.DeserializeObject<ReceiptCaseData>(receiptEntry.TargetReceiptCaseData);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    var bonReferenz = createBonReferenz(receiptEntry);

                    bonReferenz.REF_TYP = targetReceiptCaseData.RefType;
                    bonReferenz.REF_NAME = targetReceiptCaseData.RefName;
                    bonReferenz.REF_DATUM = targetReceiptCaseData.RefMoment;
                    bonReferenz.REF_Z_KASSE_ID = targetReceiptCaseData.RefCashBoxIdentification;
                    bonReferenz.REF_Z_NR = targetReceiptCaseData.RefClosingNr;
                    bonReferenz.REF_BON_ID = targetReceiptCaseData.RefReceiptId;
                    yield return bonReferenz;

                }else
                {
                    var bonReferenz = createBonReferenz(receiptEntry);
                    bonReferenz.REF_TYP = "Transaktion";
                    bonReferenz.REF_DATUM = receiptEntry.SourceZMoment;
                    bonReferenz.REF_Z_KASSE_ID = _parameters.CashboxIdentification;
                    bonReferenz.REF_BON_ID = receiptEntry.SourceReceiptIdentification;

                    if (receiptEntry.SourceZNumber.HasValue)
                    {
                        bonReferenz.REF_Z_NR = receiptEntry.SourceZNumber.Value;
                    }

                    yield return bonReferenz;
                }
            }
        }

        private BonReferenz createBonReferenz(ReceiptReferencesGroupedData receiptEntry) {
            var bonref = new BonReferenz()
            {
                Z_KASSE_ID = _parameters.CashboxIdentification,
                BON_ID = receiptEntry.TargetReceiptIdentification,
                POS_ZEILE = null  // Not supported
            };
            if (receiptEntry.TargetZMoment.HasValue && receiptEntry.TargetZNumber.HasValue)
            {
                bonref.Z_ERSTELLUNG = receiptEntry.TargetZMoment.Value;
                bonref.Z_NR = receiptEntry.TargetZNumber.Value;
            }
            return bonref;
        }
    }
}
