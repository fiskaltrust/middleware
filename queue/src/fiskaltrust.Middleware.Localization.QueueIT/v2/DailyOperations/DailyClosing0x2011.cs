﻿using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.ifPOS.v1.it;
using System.Collections.Generic;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using System;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2.DailyOperations
{
    public class DailyClosing0x2011 : IReceiptTypeProcessor
    {
        private readonly IITSSCDProvider _itSSCDProvider;

        public ITReceiptCases ReceiptCase => ITReceiptCases.DailyClosing0x2011;

        public DailyClosing0x2011(IITSSCDProvider itSSCDProvider)
        {
            _itSSCDProvider = itSSCDProvider;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            try
            {
                var actionJournalEntry = ActionJournalFactory.CreateDailyClosingActionJournal(queue, queueItem, request);
                var result = await _itSSCDProvider.ProcessReceiptAsync(new ProcessRequest
                {
                    ReceiptRequest = request,
                    ReceiptResponse = receiptResponse
                });
                var zNumber = result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber).Data;
                receiptResponse.ftReceiptIdentification += $"Z{zNumber.PadLeft(4, '0')}";
                receiptResponse.ftSignatures = result.ReceiptResponse.ftSignatures;
                return (receiptResponse, new List<ftActionJournal>
                {
                    actionJournalEntry
                });
            }
            catch (Exception ex)
            {
                receiptResponse.SetReceiptResponseErrored($"The daily closing operation failed with the following error message: {ex.Message}");
                return (receiptResponse, new List<ftActionJournal>());
            }
        }
    }
}
