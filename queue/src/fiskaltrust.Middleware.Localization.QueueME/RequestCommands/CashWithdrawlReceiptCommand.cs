using System;
using System.Threading.Tasks;
using System.Linq;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;
using fiskaltrust.Middleware.Contracts.Constants;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class CashWithdrawlReceiptCommand : RequestCommand
    {
        public CashWithdrawlReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryME signatureFactory, IConfigurationRepository configurationRepository,
            IJournalMERepository journalMERepository, IQueueItemRepository queueItemRepository, IActionJournalRepository actionJournalRepository) :
            base(logger, signatureFactory, configurationRepository, journalMERepository, queueItemRepository, actionJournalRepository)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueME)
        {
            try
            {
                if (queueME == null || !queueME.ftSignaturCreationUnitMEId.HasValue)
                {
                    throw new ENUNotRegisteredException();
                }
                var scu = await _configurationRepository.GetSignaturCreationUnitMEAsync(queueME.ftSignaturCreationUnitMEId.Value).ConfigureAwait(false);
                var registerCashWithdrawalRequest = new RegisterCashWithdrawalRequest()
                {
                    Amount = request.cbReceiptAmount ?? request.cbChargeItems.Sum(x => x.Amount),
                    Moment = request.cbReceiptMoment,
                    RequestId = queueItem.ftQueueItemId,
                    SubsequentDeliveryType = null,
                    TcrCode = scu.TcrCode
                };
                await client.RegisterCashWithdrawalAsync(registerCashWithdrawalRequest).ConfigureAwait(false);
                var receiptResponse = CreateReceiptResponse(request, queueItem);
                var actionJournalEntry = await CreateActionJournal(queue, (long) JournalTypes.CashDepositME, queueItem).ConfigureAwait(false);
                return new RequestCommandResponse()
                {
                    ReceiptResponse = receiptResponse,
                    ActionJournals = new List<ftActionJournal>()
                    {
                        actionJournalEntry
                    }
                };
            }
            catch (Exception ex) when (ex.GetType().Name == RETRYPOLICYEXCEPTION_NAME)
            {
                _logger.LogDebug(ex, "TSE not reachable.");
                return await ProcessFailedReceiptRequest(queueItem, request, queueME).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "An exception occured while processing this request.");
                return await ProcessFailedReceiptRequest(queueItem, request, queueME).ConfigureAwait(false);
            }
        }
    }
}
