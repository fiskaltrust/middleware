﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Extensions;
using fiskaltrust.Middleware.Localization.QueueAT.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Services;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueAT.RequestCommands
{
    internal class MonthlyClosingReceiptCommand : RequestCommand
    {
        private readonly ExportService _exportService;

        public override string ReceiptName => "Monthly-closing receipt";

        public MonthlyClosingReceiptCommand(IATSSCDProvider sscdProvider, MiddlewareConfiguration middlewareConfiguration, QueueATConfiguration queueATConfiguration,
            ExportService exportService, ILogger<RequestCommand> logger)
            : base(sscdProvider, middlewareConfiguration, queueATConfiguration, logger)
        {
            _exportService = exportService;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueAT queueAT, ReceiptRequest request, ftQueueItem queueItem)
        {
            ThrowIfTraining(request);
            var response = CreateReceiptResponse(request, queueItem, queueAT, queue);

            if (request.cbChargeItems?.Count() != 0 || request.cbPayItems?.Count() != 0)
            {
                var notZeroReceiptActionJournal = CreateActionJournal(queue, queueItem, $"Tried to create a monthly receipt for {queue.ftQueueId}, but the incoming receipt is not a zero receipt.");
                _logger.LogInformation(notZeroReceiptActionJournal.Message);

                return new RequestCommandResponse
                {
                    ReceiptResponse = response,
                    ActionJournals = new() { notZeroReceiptActionJournal }
                };
            }

            var aj = CreateActionJournal(queue, queueItem, $"Monthly receipt #{queueAT.LastSettlementMonth} of Queue {queue.ftQueueId}. Turnover-Counter: {queueAT.ftCashTotalizer}");

            var (actionJournals, journalAT) = await SignReceiptAndProcessMonthlyReceiptAsync(queue, queueAT, queueItem, request, response);
            actionJournals.Add(aj);

            var notificationSignatures = CreateNotificationSignatures(actionJournals);
            response.ftSignatures = response.ftSignatures.Concat(notificationSignatures).ToArray();

            return new RequestCommandResponse
            {
                ReceiptResponse = response,
                ActionJournals = actionJournals,
                JournalAT = journalAT
            };
        }

        internal async Task<(List<ftActionJournal> actionJournals, ftJournalAT journalAT)> SignReceiptAndProcessMonthlyReceiptAsync(ftQueue queue, ftQueueAT queueAT, ftQueueItem queueItem, ReceiptRequest request, ReceiptResponse response)
        {
            var actionJournals = new List<ftActionJournal>();

            var (receiptIdentification, ftStateData, _, signatureItems, journalAT) = await SignReceiptAsync(queueAT, request, response.ftReceiptIdentification, response.ftReceiptMoment, queueItem.ftQueueItemId);
            response.ftSignatures = response.ftSignatures.Concat(signatureItems).ToArray();
            response.ftReceiptIdentification = receiptIdentification;
            response.ftStateData = ftStateData;

            if (journalAT != null)
            {
                // Receipt successfully signed, process monthly receipt
                queue.StopMoment = DateTime.UtcNow;
                response.ftSignatures = response.ftSignatures.Extend(new SignaturItem
                {
                    Caption = $"Monatsbeleg #{queueAT.LastSettlementMonth}",
                    Data = $"{queueAT.ftCashTotalizer:0.00}",
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = (long) SignaturItem.Types.AT_StorageObligation
                });

                if (_queueATConfiguration.EnableMonthlyExport)
                {
                    var from = queueAT.LastSettlementMoment?.Ticks ?? 0;
                    var filename = Path.Combine(_middlewareConfiguration.ServiceFolder, $"{queueAT.ftQueueATId}_{DateTime.Now:yyyyMMddhhmmssfff}_{queueAT.CashBoxIdentification}_{queueAT.LastSettlementMonth:00}_rksv_dep.json");
                    await _exportService.PerformRksvJournalExportAsync(from, DateTime.UtcNow.Ticks, filename);
                }

                // TODO: It's not super smart IMO to mix cbReceiptMoment and DateTime.UtcNow here, but it was like this before - might make sense to change this though.
                queueAT.LastSettlementMoment = request.cbReceiptMoment;
                queueAT.LastSettlementMonth += 1;
                if ((response.ftState & 0x10L) > 0)
                {
                    response.ftState -= 0x10L;
                }

                return (actionJournals, journalAT);
            }
            else
            {
                var signingFailedActionJournal = CreateActionJournal(queue, queueItem, $"Monthly receipt failed: Could not sign receipt.");
                _logger.LogInformation(signingFailedActionJournal.Message);
                actionJournals.Add(signingFailedActionJournal);

                return (actionJournals, null);
            }
        }
    }
}
