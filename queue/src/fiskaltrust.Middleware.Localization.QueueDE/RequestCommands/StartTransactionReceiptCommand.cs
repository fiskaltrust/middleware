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
using static fiskaltrust.Exports.DSFinVK.Constants.DSFinVKConstants;

namespace fiskaltrust.Middleware.Localization.QueueDE.RequestCommands
{
    public class StartTransactionReceiptCommand : RequestCommand
    {
        public override string ReceiptName => "Start-transaction receipt";
        public StartTransactionReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryDE signatureFactory, IDESSCDProvider deSSCDProvider, 
            ITransactionPayloadFactory transactionPayloadFactory, IReadOnlyQueueItemRepository queueItemRepository, IConfigurationRepository configurationRepository, 
            IJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration, IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo, 
            IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo, IPersistentTransactionRepository<OpenTransaction> openTransactionRepo, ITarFileCleanupService tarFileCleanupService, QueueDEConfiguration queueDEConfiguration, IMasterDataService masterDataService) 
            : base(logger, signatureFactory, deSSCDProvider, transactionPayloadFactory, queueItemRepository, configurationRepository, journalDERepository, 
                  middlewareConfiguration, failedStartTransactionRepo, failedFinishTransactionRepo, openTransactionRepo, tarFileCleanupService, queueDEConfiguration, masterDataService)
        {
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueDE queueDE, ReceiptRequest request, ftQueueItem queueItem)
        {
            _logger.LogTrace("StartTransactionReceiptCommand.ExecuteAsync [enter].");
            ThrowIfImplicitFlow(request);

            if (await _openTransactionRepo.ExistsAsync(request.cbReceiptReference).ConfigureAwait(false))
            {
                var opentrans = await _openTransactionRepo.GetAsync(request.cbReceiptReference).ConfigureAwait(false);
                throw new ArgumentException($"Transactionnumber {opentrans.TransactionNumber} was already started using cbReceiptReference '{request.cbReceiptReference}'.");
            }

            var (processType, payload) = _transactionPayloadFactory.CreateReceiptPayload(request);

            var receiptResponse = CreateReceiptResponse(request, queueItem, queueDE);

            try
            {
                (var transactionNumber, var signatures) = await ProcessStartTransactionRequestAsync(request.cbReceiptReference, queueItem, queueDE, processType, payload).ConfigureAwait(false);

                receiptResponse.ftReceiptIdentification = request.GetReceiptIdentification(queue.ftReceiptNumerator, transactionNumber);

                if (request.IsTraining())
                {
                    signatures.Add(_signatureFactory.GetSignatureForTraining());
                }

                receiptResponse.ftSignatures = signatures.ToArray();
                return new RequestCommandResponse()
                {
                    ReceiptResponse = receiptResponse,
                    Signatures = signatures,
                    TransactionNumber = transactionNumber
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
            finally
            {
                _logger.LogTrace("StartTransactionReceiptCommand.ExecuteAsync [exit].");
            }
        }

        private async Task<(ulong transactionNumber, List<SignaturItem> signatures)> ProcessStartTransactionRequestAsync(string transactionIdentifier, ftQueueItem queueItem, ftQueueDE queueDE, string processType, string payload)
        {
            _logger.LogTrace("StartTransactionReceiptCommand.ProcessStartTransactionRequestAsync [enter].");
            var startTransactionResult = await _transactionFactory.PerformStartTransactionRequestAsync(processType, payload, queueItem.ftQueueItemId, queueDE.CashBoxIdentification).ConfigureAwait(false);
            await _openTransactionRepo.InsertOrUpdateTransactionAsync(new OpenTransaction
            {
                cbReceiptReference = transactionIdentifier,
                StartTransactionSignatureBase64 = startTransactionResult.SignatureData.SignatureBase64,
                StartMoment = startTransactionResult.TimeStamp,
                TransactionNumber = (long) startTransactionResult.TransactionNumber
            }).ConfigureAwait(false);
            _logger.LogTrace("StartTransactionReceiptCommand.ProcessStartTransactionRequestAsync [exit].");
            return (startTransactionResult.TransactionNumber, new List<SignaturItem> { _signatureFactory.GetSignaturForStartTransaction(startTransactionResult) });
        }
    }
}