using System;
using System.Threading.Tasks;
using System.Linq;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;
using System.Collections.Generic;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class CashDepositReceiptCommand : RequestCommand
    {
        public CashDepositReceiptCommand(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository, IMiddlewareJournalMERepository journalMeRepository,
            IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareActionJournalRepository actionJournalRepository, QueueMEConfiguration queueMeConfiguration) :
            base(logger, configurationRepository, journalMeRepository, queueItemRepository, actionJournalRepository, queueMeConfiguration)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueMe, bool subsequent = false)
        {

                var scu = await ConfigurationRepository.GetSignaturCreationUnitMEAsync(queueMe.ftSignaturCreationUnitMEId.Value).ConfigureAwait(false);
                var depositChargeItems = request.cbChargeItems.Where(x => x.ftChargeItemCase == 0x4D45000000000020);
                if (!depositChargeItems.Any())
                {
                    throw new Exception("An opening-balance receipt was sent that did not include any cash deposit charge items.");
                }

                var registerCashDepositRequest = new RegisterCashDepositRequest
                {
                    Amount = depositChargeItems.Sum(x => x.Amount),
                    Moment = request.cbReceiptMoment,
                    RequestId = queueItem.ftQueueItemId,
                    TcrCode = scu.TcrCode,
                    SubsequentDeliveryType = subsequent ? SubsequentDeliveryType.NoInternet : null
                };


                var registerCashDepositResponse = await client.RegisterCashDepositAsync(registerCashDepositRequest).ConfigureAwait(false);
                await InsertJournalMe(queue, request, queueItem, registerCashDepositResponse).ConfigureAwait(false);
                var receiptResponse = CreateReceiptResponse(queue, request, queueItem);
                var actionJournalEntry = CreateActionJournal(queue, request.ftReceiptCase, queueItem);
                return new RequestCommandResponse
                {
                    ReceiptResponse = receiptResponse,
                    ActionJournals = new List<ftActionJournal>
                    {
                        actionJournalEntry
                    }
                };

        }

        public override async Task<bool> ReceiptNeedsReprocessing(ftQueueME queueMe, ftQueueItem queueItem, ReceiptRequest request)
        {
            var journalMe = await JournalMeRepository.GetByQueueItemId(queueItem.ftQueueItemId).FirstOrDefaultAsync().ConfigureAwait(false);
            return journalMe == null || string.IsNullOrEmpty(journalMe.FCDC);
        }

        private async Task InsertJournalMe(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, RegisterCashDepositResponse registerCashDepositResponse)
        {
            var journal = new ftJournalME
            {
                ftJournalMEId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                cbReference = request.cbReceiptReference,
                Number = queue.ftReceiptNumerator,
                FCDC = registerCashDepositResponse.FCDC,
                JournalType = request.ftReceiptCase,
                TimeStamp = DateTime.UtcNow.Ticks
            };
            await JournalMeRepository.InsertAsync(journal).ConfigureAwait(false);
        }

        private ftActionJournal CreateActionJournal(ftQueue queue, long type, ftQueueItem queueItem)
        {
            return new ftActionJournal
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                Type = $"{type:X}",
                Moment = DateTime.UtcNow,
                Message = "Cash-deposit receipt was processed."
            };
        }
    }
}
