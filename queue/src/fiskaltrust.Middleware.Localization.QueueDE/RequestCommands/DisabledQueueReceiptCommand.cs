using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Localization.QueueDE.Models;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueDE.RequestCommands
{
    public class DisabledQueueReceiptCommand : RequestCommand
    {
        public override string ReceiptName => "Disabled-queue receipt";

        private const long SECURITY_MECHAMISN_DEACTIVATED_FLAG = 0x0000_0000_0000_0001;

        private bool _loggedDisabledQueueReceiptRequest = false;

        public DisabledQueueReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryDE signatureFactory, IDESSCDProvider deSSCDProvider, ITransactionPayloadFactory transactionPayloadFactory, IReadOnlyQueueItemRepository queueItemRepository, IConfigurationRepository configurationRepository, IJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration, IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo, IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo, IPersistentTransactionRepository<OpenTransaction> openTransactionRepo, ITarFileCleanupService tarFileCleanupService, QueueDEConfiguration queueDEConfiguration, IMasterDataService masterDataService) : base(logger, signatureFactory, deSSCDProvider, transactionPayloadFactory, queueItemRepository, configurationRepository, journalDERepository, middlewareConfiguration, failedStartTransactionRepo, failedFinishTransactionRepo, openTransactionRepo, tarFileCleanupService, queueDEConfiguration, masterDataService)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueDE queueDE,  ReceiptRequest request, ftQueueItem queueItem)
        {
            _logger.LogTrace("DisabledQueueReceiptCommand.ExecuteAsync [enter].");
            var receiptResponse = CreateReceiptResponse(request, queueItem, queueDE);

            var actionJournals = new List<ftActionJournal>();

            if (!_loggedDisabledQueueReceiptRequest)
            {
                actionJournals.Add(
                        new ftActionJournal
                        {
                            ftActionJournalId = Guid.NewGuid(),
                            ftQueueId = queueItem.ftQueueId,
                            ftQueueItemId = queueItem.ftQueueItemId,
                            Moment = DateTime.UtcNow,
                            Message = $"QueueId {queueItem.ftQueueId} was not activated or already deactivated"
                        }
                    );
                _loggedDisabledQueueReceiptRequest = true;
            }

            receiptResponse.ftState += SECURITY_MECHAMISN_DEACTIVATED_FLAG;

            receiptResponse.ftReceiptIdentification = request.GetReceiptIdentification(queue.ftReceiptNumerator, null);
            _logger.LogTrace("DisabledQueueReceiptCommand.ExecuteAsync [exit].");
            return await Task.FromResult(new RequestCommandResponse()
            {
                ReceiptResponse = receiptResponse,
                ActionJournals = actionJournals,
            }).ConfigureAwait(false);
        }
    }
}