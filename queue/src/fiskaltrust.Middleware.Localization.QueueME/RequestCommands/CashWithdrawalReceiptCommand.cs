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
    public class CashWithdrawalReceiptCommand : RequestCommand
    {
        public CashWithdrawalReceiptCommand(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository,
            IMiddlewareJournalMERepository journalMeRepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareActionJournalRepository actionJournalRepository, QueueMEConfiguration queueMeConfiguration) :
            base(logger, configurationRepository, journalMeRepository, queueItemRepository, actionJournalRepository, queueMeConfiguration)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueMe, bool subsequent = false)
        {

                var scu = await ConfigurationRepository.GetSignaturCreationUnitMEAsync(queueMe.ftSignaturCreationUnitMEId.Value).ConfigureAwait(false);
                var withdrawalChargeItems = request.cbChargeItems.Where(x => x.ftChargeItemCase == 0x4D45000000000021);
                if (!withdrawalChargeItems.Any())
                {
                    throw new Exception("An cash-withdrawal receipt was sent that did not include any cash withdrawal charge items.");
                }

                var registerCashWithdrawalRequest = new RegisterCashWithdrawalRequest
                {
                    Amount = withdrawalChargeItems.Sum(x => x.Amount),
                    Moment = request.cbReceiptMoment,
                    RequestId = queueItem.ftQueueItemId,
                    SubsequentDeliveryType = subsequent ? SubsequentDeliveryType.NoInternet : null,
                    TcrCode = scu.TcrCode
                };
                await client.RegisterCashWithdrawalAsync(registerCashWithdrawalRequest).ConfigureAwait(false);
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
            return await ActionJournalExists(queueItem, request.ftReceiptCase).ConfigureAwait(false) == false;
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
                Message = "Cash-withdrawal receipt was processed."
            };
        }
    }
}
