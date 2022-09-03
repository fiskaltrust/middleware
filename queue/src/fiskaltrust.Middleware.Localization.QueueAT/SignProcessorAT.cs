using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Models;
using fiskaltrust.Middleware.Localization.QueueAT.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueAT.RequestCommands.Factories;
using fiskaltrust.Middleware.Queue;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Queue.Extensions;
using fiskaltrust.Middleware.Localization.QueueAT.Extensions;
using fiskaltrust.storage.serialization.AT.V0;
using fiskaltrust.Middleware.Localization.QueueAT.Helpers;
using System.Runtime.InteropServices;

namespace fiskaltrust.Middleware.Localization.QueueAT
{
    public class SignProcessorAT : IMarketSpecificSignProcessor
    {
        private readonly MiddlewareConfiguration _middlewareConfiguration;
        private readonly SignatureFactory _signatureFactory;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IJournalATRepository _journalATRepository;
        private readonly IRequestCommandFactory _requestCommandFactory;


        public SignProcessorAT(MiddlewareConfiguration middlewareConfiguration, SignatureFactory signatureFactory, IConfigurationRepository configurationRepository,
            IJournalATRepository journalATRepository, IRequestCommandFactory requestCommandFactory)
        {
            _middlewareConfiguration = middlewareConfiguration;
            _signatureFactory = signatureFactory;
            _configurationRepository = configurationRepository;
            _journalATRepository = journalATRepository;
            _requestCommandFactory = requestCommandFactory;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            var queueAT = await _configurationRepository.GetQueueATAsync(queueItem.ftQueueId).ConfigureAwait(false);

            // Currently never used & not supported in v2+
            if (!string.IsNullOrEmpty(queueAT.ClosedSystemKind))
            {
                throw new NotImplementedException("ClosedSystemKind is currently not supported.");
            }

            var requestCommandResponse = await PerformReceiptRequest(request, queueItem, queue, queueAT).ConfigureAwait(false);
            if (_middlewareConfiguration.IsSandbox)
            {
                requestCommandResponse.ReceiptResponse.ftSignatures = requestCommandResponse.ReceiptResponse.ftSignatures.Concat(_signatureFactory.CreateSandboxSignature(queueAT.ftQueueATId));
            }

            var additionalActionJournals = new List<ftActionJournal>();
            additionalActionJournals.AddRange(ProcessSSCDFailedReceipt(queueItem, requestCommandResponse.ReceiptResponse, requestCommandResponse.JournalAT, queue, queueAT));
            additionalActionJournals.AddRange(ProcessFailedReceipt(queueItem, request, requestCommandResponse.ReceiptResponse, queueAT));
            additionalActionJournals.AddRange(ProcessHandwrittenReceipt(queueItem, request, requestCommandResponse.ReceiptResponse, queueAT));

            AddStateFlagIfNewMonthOrYearStarted(request, requestCommandResponse.ReceiptResponse, queueAT);

            if (requestCommandResponse.JournalAT != null)
            {
                await _journalATRepository.InsertAsync(requestCommandResponse.JournalAT);
            }
            await _configurationRepository.InsertOrUpdateQueueATAsync(queueAT).ConfigureAwait(false);

            return (requestCommandResponse.ReceiptResponse, requestCommandResponse.ActionJournals.Concat(additionalActionJournals).ToList());
        }

        private void AddStateFlagIfNewMonthOrYearStarted(ReceiptRequest request, ReceiptResponse response, ftQueueAT queueAT)
        {
            if (!queueAT.LastSettlementMoment.HasValue)
            {
                return;
            }

            var lastSettlementMoment = queueAT.LastSettlementMoment.Value;

            // If the date of the month is < 15, use the end of the "current" month. Otherwise, use the end of the following one.
            var lastSettlementMonth = lastSettlementMoment.Month;
            var lastSettlementYear = lastSettlementMoment.Year;
            if (lastSettlementMoment.Day < 15)
            {
                lastSettlementMonth = lastSettlementMoment.AddMonths(-1).Month;
                lastSettlementYear = lastSettlementMoment.AddMonths(-1).Year;
            }

            var nextSettlement = new DateTime(lastSettlementYear, lastSettlementMonth, 1).AddMonths(2);

            if (request.cbReceiptMoment >= nextSettlement)
            {
                if (nextSettlement.Month == 1 || request.cbReceiptMoment.Year > nextSettlement.Year)
                {
                    // New year started since last settlement
                    response.ftState |= 0x20;
                }
                else
                {
                    // New month started since last settlement
                    response.ftState |= 0x10;
                }
            }
        }

        private List<ftActionJournal> ProcessFailedReceipt(ftQueueItem queueItem, ReceiptRequest request, ReceiptResponse response, ftQueueAT queueAT)
        {
            var actionJournals = new List<ftActionJournal>();
            if (request.HasFailedReceiptFlag())
            {
                if (!queueAT.UsedFailedMomentMin.HasValue)
                {
                    queueAT.UsedFailedMomentMin = request.cbReceiptMoment;
                    queueAT.UsedFailedMomentMax = request.cbReceiptMoment;

                    queueAT.UsedFailedQueueItemId = queueItem.ftQueueItemId;

                    actionJournals.Add(new ftActionJournal
                    {
                        ftActionJournalId = Guid.NewGuid(),
                        ftQueueId = queueAT.ftQueueATId,
                        ftQueueItemId = queueItem.ftQueueItemId,
                        Moment = DateTime.UtcNow,
                        Message = $"QueueItem {queueItem.ftQueueItemId} enabled mode \"UsedFailed\" of Queue {queueAT.ftQueueATId}"
                    });

                }
                queueAT.UsedFailedCount++;

                if (request.cbReceiptMoment < queueAT.UsedFailedMomentMin)
                {
                    queueAT.UsedFailedMomentMin = request.cbReceiptMoment;
                }

                if (request.cbReceiptMoment > queueAT.UsedFailedMomentMax)
                {
                    queueAT.UsedFailedMomentMax = request.cbReceiptMoment;
                }
            }

            if (queueAT.UsedFailedCount > 0)
            {
                response.ftState |= 0x8;
            }

            return actionJournals;
        }

        private List<ftActionJournal> ProcessSSCDFailedReceipt(ftQueueItem queueItem, ReceiptResponse response, ftJournalAT journalAT, ftQueue queue, ftQueueAT queueAT)
        {
            var actionJournals = new List<ftActionJournal>();

            if (queueAT.SSCDFailCount > 0)
            {
                response.ftState |= 0x2;
            }

            if (queueAT.SSCDFailMoment.HasValue && (DateTime.UtcNow.Subtract(queueAT.SSCDFailMoment.Value).TotalHours > 48))
            {
                response.ftState |= 0x4;

                if (!queueAT.SSCDFailMessageSent.HasValue)
                {
                    var aj = ATFONRegistrationHelper.CreateQueueDeactivationJournal(queue, queueAT, queueItem, journalAT, false);
                    actionJournals.Add(aj);

                    queueAT.SSCDFailMessageSent = DateTime.UtcNow;
                }
            }

            return actionJournals;
        }

        private List<ftActionJournal> ProcessHandwrittenReceipt(ftQueueItem queueItem, ReceiptRequest request, ReceiptResponse response, ftQueueAT queueAT)
        {
            var actionJournals = new List<ftActionJournal>();
            if (request.HasHandwrittenReceiptFlag())
            {
                if (!queueAT.UsedMobileMoment.HasValue || queueAT.UsedMobileMoment > request.cbReceiptMoment)
                {
                    queueAT.UsedMobileMoment = request.cbReceiptMoment;
                    queueAT.UsedMobileQueueItemId = queueItem.ftQueueItemId;

                    actionJournals.Add(new ftActionJournal
                    {
                        ftActionJournalId = Guid.NewGuid(),
                        ftQueueId = queueAT.ftQueueATId,
                        ftQueueItemId = queueItem.ftQueueItemId,
                        Moment = DateTime.UtcNow,
                        Message = $"QueueItem {queueItem.ftQueueItemId} enabled mode \"UsedMobile\" of Queue {queueAT.ftQueueATId}"
                    });
                }
                queueAT.UsedMobileCount++;
            }

            if (queueAT.UsedMobileCount > 0)
            {
                response.ftState |= 0x8;
            }

            return actionJournals;
        }

        private async Task<RequestCommandResponse> PerformReceiptRequest(ReceiptRequest request, ftQueueItem queueItem, ftQueue queue, ftQueueAT queueAT)
        {
            RequestCommand command;
            try
            {
                command = _requestCommandFactory.Create(queue, queueAT, request);
            }
            catch
            {
                throw new ArgumentException($"ReceiptCase {request.ftReceiptCase:X} unknown.");
            }

            return await command.ExecuteAsync(queue, queueAT, request, queueItem);
        }
    }
}
