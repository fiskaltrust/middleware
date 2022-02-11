using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueDE.Models;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;

namespace fiskaltrust.Middleware.Localization.QueueDE.RequestCommands
{
    internal class YearlyClosingReceiptCommand : ClosingReceiptCommand
    {
        public override string ReceiptName => "Yearly-closing receipt";
        public YearlyClosingReceiptCommand(IMasterDataService masterDataService, ILogger<RequestCommand> logger, SignatureFactoryDE signatureFactory,
            IDESSCDProvider deSSCDProvider, ITransactionPayloadFactory transactionPayloadFactory, IReadOnlyQueueItemRepository queueItemRepository,
            IConfigurationRepository configurationRepository, IJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration,
            IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo, IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo,
            IPersistentTransactionRepository<OpenTransaction> openTransactionRepo)
            : base(masterDataService, logger, signatureFactory, deSSCDProvider, transactionPayloadFactory, queueItemRepository, configurationRepository,
                  journalDERepository, middlewareConfiguration, failedStartTransactionRepo, failedFinishTransactionRepo, openTransactionRepo)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueDE queueDE, IDESSCD client, ReceiptRequest request, ftQueueItem queueItem)
        {
            ThrowIfNoImplicitFlow(request);
            ThrowIfTraining(request);

            var (processType, payload) = _transactionPayloadFactory.CreateReceiptPayload(request);
            var receiptResponse = CreateReceiptResponse(request, queueItem, queueDE);

            try
            {
                var processReceiptResponse = await ProcessReceiptAsync(request.cbReceiptReference, processType, payload, queueItem, queueDE).ConfigureAwait(false);
                receiptResponse.ftStateData = await StateDataFactory.AppendMasterDataAsync(_masterDataService, receiptResponse.ftStateData).ConfigureAwait(false);
                receiptResponse.ftReceiptIdentification = request.GetReceiptIdentification(queue.ftReceiptNumerator, processReceiptResponse.TransactionNumber);
                receiptResponse.ftSignatures = processReceiptResponse.Signatures.ToArray();

                if (!request.IsTseTarDownloadBypass())
                {
                    await PerformTarFileExportAsync(queueItem, queue, queueDE, client, erase: true).ConfigureAwait(false);
                }

                var masterDataChanged = false;
                if (request.IsMasterDataUpdate() && await _masterDataService.HasDataChangedAsync().ConfigureAwait(false))
                {
                    await _masterDataService.PersistConfigurationAsync().ConfigureAwait(false);
                    masterDataChanged = true;
                    _logger.LogInformation("Master data was updated. The changed master data is valid from from now on, all receipts that were processed until now still refer to the old master data.");
                }

                var message = masterDataChanged
                    ? "Yearly-Closing receipt was processed, and a master data update was performed."
                    : "Yearly-Closing receipt was processed.";
                var type = masterDataChanged
                    ? 0x4445_0000_0800_0006
                    : 0x4445_0000_0000_0006;
                var actionJournals = CreateClosingActionJournals(queueItem, queue, processReceiptResponse.TransactionNumber, masterDataChanged, message, type);
                return new RequestCommandResponse()
                {
                    ActionJournals = actionJournals,
                    ReceiptResponse = receiptResponse,
                    Signatures = processReceiptResponse.Signatures,
                    TransactionNumber = processReceiptResponse.TransactionNumber
                };
            }
            catch (Exception ex) when (ex.GetType().Name == RETRYPOLICYEXCEPTION_NAME)
            {
                _logger.LogDebug(ex, "TSE not reachable.");
                return await ProcessSSCDFailedReceiptRequest(request, queueItem, queue, queueDE).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "An exception occured while processing this request.");
                return await ProcessSSCDFailedReceiptRequest(request, queueItem, queue, queueDE).ConfigureAwait(false);
            }
        }
    }
}
