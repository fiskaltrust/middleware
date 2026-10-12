using System;
using System.Linq;
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
    /// <summary>
    /// Records an aborted receipt (DSFinV-K BON_TYP "AVBelegabbruch") in the implicit flow (ftReceiptCase 0x001A).
    /// The TSE transaction is started and finished within this request, as no transaction was opened before.
    /// </summary>
    public class AbortReceiptCommand : RequestCommand
    {
        public override string ReceiptName => "Abort receipt";

        public AbortReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryDE signatureFactory, IDESSCDProvider deSSCDProvider, ITransactionPayloadFactory transactionPayloadFactory, IReadOnlyQueueItemRepository queueItemRepository, IConfigurationRepository configurationRepository, IJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration, IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo, IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo, IPersistentTransactionRepository<OpenTransaction> openTransactionRepo, ITarFileCleanupService tarFileCleanupService, QueueDEConfiguration queueDEConfiguration, IMasterDataService masterDataService) : base(logger, signatureFactory, deSSCDProvider, transactionPayloadFactory, queueItemRepository, configurationRepository, journalDERepository, middlewareConfiguration, failedStartTransactionRepo, failedFinishTransactionRepo, openTransactionRepo, tarFileCleanupService, queueDEConfiguration, masterDataService)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueDE queueDE, ReceiptRequest request, ftQueueItem queueItem)
        {
            _logger.LogTrace("AbortReceiptCommand.ExecuteAsync [enter].");
            if (!request.IsImplictFlow())
            {
                throw new ArgumentException($"ReceiptCase {request.ftReceiptCase:X} (abort-receipt) must use the implicit-flow flag.");
            }
            if (string.IsNullOrEmpty(request.cbReceiptReference))
            {
                throw new ArgumentException($"ReceiptCase {request.ftReceiptCase:X} (abort-receipt) requires a cbReceiptReference.");
            }
            if ((request.cbPayItems ?? Array.Empty<PayItem>()).Any())
            {
                // DSFinV-K Anhang B: No payment may be made in connection with the transaction type AVBelegabbruch.
                throw new ArgumentException($"ReceiptCase {request.ftReceiptCase:X} (abort-receipt) must not contain any pay items, as no payment is allowed for an aborted receipt.");
            }

            var (processType, payload) = _transactionPayloadFactory.CreateAbortReceiptPayload(request);
            var receiptResponse = CreateReceiptResponse(request, queueItem, queueDE);

            try
            {
                (var transactionNumber, var signatures) = await ProcessReceiptStartTransSignAsync(request.cbReceiptReference, processType, payload, queueItem, queueDE, request.IsImplictFlow()).ConfigureAwait(false);

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
            finally
            {
                _logger.LogTrace("AbortReceiptCommand.ExecuteAsync [exit].");
            }
        }
    }
}
