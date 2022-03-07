using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using fiskaltrust.Middleware.Localization.QueueDE.Models;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE.RequestCommands
{
    public class FailTransactionReceiptCommand : RequestCommand
    {
        public override string ReceiptName => "Fail-transaction receipt";

        public FailTransactionReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryDE signatureFactory, IDESSCDProvider deSSCDProvider, ITransactionPayloadFactory transactionPayloadFactory, IReadOnlyQueueItemRepository queueItemRepository, IConfigurationRepository configurationRepository, IJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration, IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo, IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo, IPersistentTransactionRepository<OpenTransaction> openTransactionRepo, ITarFileCleanupService tarFileCleanupService, QueueDEConfiguration queueDEConfiguration) : base(logger, signatureFactory, deSSCDProvider, transactionPayloadFactory, queueItemRepository, configurationRepository, journalDERepository, middlewareConfiguration, failedStartTransactionRepo, failedFinishTransactionRepo, openTransactionRepo, tarFileCleanupService, queueDEConfiguration)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueDE queueDE, IDESSCD client, ReceiptRequest request, ftQueueItem queueItem)
        {
            var closeSingleTransaction = !string.IsNullOrEmpty(request.cbReceiptReference);
            if (closeSingleTransaction && request.IsImplictFlow())
            {
                throw new ArgumentException($"ReceiptCase {request.ftReceiptCase:X} (fail-transaction-receipt) cannot use implicit-flow flag when a single transaction should be failed.");
            }
            if (!closeSingleTransaction && !request.IsImplictFlow())
            {
                throw new ArgumentException($"ReceiptCase {request.ftReceiptCase:X} (fail-transaction-receipt) must use implicit-flow flag when multiple transactions should be failed.");
            }
            if (closeSingleTransaction && !await _openTransactionRepo.ExistsAsync(request.cbReceiptReference).ConfigureAwait(false))
            {
                throw new ArgumentException($"No open transaction found for cbReceiptReference '{request.cbReceiptReference}'. If you want to close multiple transactions, pass an array value for 'CurrentStartedTransactionNumbers' via ftReceiptCaseData.");
            }

            var (processType, payload) = _transactionPayloadFactory.CreateReceiptPayload(request);
            var receiptResponse = CreateReceiptResponse(request, queueItem, queueDE);

            try
            {
                ulong transactionNumber;
                var signatures = new List<SignaturItem>();
                if (closeSingleTransaction)
                {
                    (transactionNumber, signatures) = await ProcessReceiptStartTransSignAsync(request.cbReceiptReference, processType, payload, queueItem, queueDE, request.IsImplictFlow()).ConfigureAwait(false);
                }
                else
                {
                    var openSignatures = new List<SignaturItem>();
                    var openTransactions = (await _openTransactionRepo.GetAsync().ConfigureAwait(false)).ToList();
                    var transactionsToClose = JsonConvert.DeserializeObject<TseInfo>(request.ftReceiptCaseData);
                    foreach (var openTransactionNumber in transactionsToClose.CurrentStartedTransactionNumbers)
                    {
                        (var openProcessType, var openPayload) = _transactionPayloadFactory.CreateAutomaticallyCanceledReceiptPayload();
                        var finishResult = await _transactionFactory.PerformFinishTransactionRequestAsync(openProcessType, openPayload, queueItem.ftQueueItemId, queueDE.CashBoxIdentification, openTransactionNumber).ConfigureAwait(false);
                        openSignatures.AddRange(_signatureFactory.GetSignaturesForFinishTransaction(finishResult));
                        var openTransaction = openTransactions.FirstOrDefault(x => (ulong) x.TransactionNumber == openTransactionNumber);
                        if (openTransaction != null)
                        {
                            await _openTransactionRepo.RemoveAsync(openTransaction.cbReceiptReference).ConfigureAwait(false);
                        }
                    }

                    (transactionNumber, signatures) = await ProcessReceiptStartTransSignAsync(request.cbReceiptReference, processType, payload, queueItem, queueDE, request.IsImplictFlow()).ConfigureAwait(false);
                    signatures.AddRange(openSignatures);
                }
                receiptResponse.ftReceiptIdentification = request.GetReceiptIdentification(queue.ftReceiptNumerator, transactionNumber);

                if (request.IsTraining())
                {
                    signatures.Add(_signatureFactory.GetSignatureForTraining());
                }

                receiptResponse.ftSignatures = signatures.ToArray();
                return await Task.FromResult(new RequestCommandResponse()
                {
                    ReceiptResponse = receiptResponse,
                    Signatures = signatures,
                    TransactionNumber = transactionNumber,
                }).ConfigureAwait(false);
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