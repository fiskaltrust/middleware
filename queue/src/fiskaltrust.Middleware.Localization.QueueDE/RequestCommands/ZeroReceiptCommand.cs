using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueDE.Models;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.ifPOS.v1.de;
using System.Linq;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Contracts.Data;

namespace fiskaltrust.Middleware.Localization.QueueDE.RequestCommands
{
    internal class ZeroReceiptCommand : RequestCommand
    {
        public override string ReceiptName => "Zero receipt";
        public ZeroReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryDE signatureFactory, IDESSCDProvider deSSCDProvider, 
            ITransactionPayloadFactory transactionPayloadFactory, IReadOnlyQueueItemRepository queueItemRepository,
            IConfigurationRepository configurationRepository, IJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration,
            IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo, IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo,
            IPersistentTransactionRepository<OpenTransaction> openTransactionRepo, ITarFileCleanupService tarFileCleanupService, QueueDEConfiguration queueDEConfiguration)
            : base(logger, signatureFactory, deSSCDProvider, transactionPayloadFactory, queueItemRepository, configurationRepository, journalDERepository,
                  middlewareConfiguration, failedStartTransactionRepo, failedFinishTransactionRepo, openTransactionRepo, tarFileCleanupService, queueDEConfiguration)
        {
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueDE queueDE, ReceiptRequest request, ftQueueItem queueItem)
        {
            ThrowIfNoImplicitFlow(request);

            var (processType, payload) = _transactionPayloadFactory.CreateReceiptPayload(request);
            var receiptResponse = CreateReceiptResponse(request, queueItem, queueDE);

            try
            {
                var processReceiptResponse = await ProcessReceiptAsync(request.cbReceiptReference, processType, payload, queueItem, queueDE).ConfigureAwait(false);
                receiptResponse.ftReceiptIdentification = request.GetReceiptIdentification(queue.ftReceiptNumerator, processReceiptResponse.TransactionNumber);

                var actionJournals = new List<ftActionJournal>();

                if (queueDE.SSCDFailCount > 0)
                {
                    _logger.LogDebug($"Closing SSCDFail-Mode");
                    var failedFinishTransactions = (await _failedFinishTransactionRepo.GetAsync().ConfigureAwait(false)).ToList();

                    if (request.IsRemoveOpenTransactionsWhichAreNotOnTse())
                    {
                        var tseInfo = await _deSSCDProvider.Instance.GetTseInfoAsync().ConfigureAwait(false);
                        var failedFinishTransactionsNotExistingOnTse = tseInfo.CurrentStartedTransactionNumbers != null
                            ? failedFinishTransactions.Where(ft => ft != null && !tseInfo.CurrentStartedTransactionNumbers.Contains((ulong) ((FailedFinishTransaction) ft).TransactionNumber))
                            : failedFinishTransactions;
                        foreach (var finishTransaction in failedFinishTransactionsNotExistingOnTse)
                        {
                            _logger.LogWarning($"The Middleware database contained the failed finish-transaction {finishTransaction.TransactionNumber}, which is not marked as open on the TSE. As the ftReceiptCaseFlag '0x0000000020000000' was set, the reference to the failed finish-transaction was removed from the database.");
                            await _failedFinishTransactionRepo.RemoveAsync(finishTransaction.cbReceiptReference).ConfigureAwait(false);

                            actionJournals.Add(
                                new ftActionJournal
                                {
                                    ftActionJournalId = Guid.NewGuid(),
                                    ftQueueId = queueItem.ftQueueId,
                                    ftQueueItemId = queueItem.ftQueueItemId,
                                    Moment = DateTime.UtcNow,
                                    Priority = -1,
                                    TimeStamp = 0,
                                    Message = $"Removed failed finish-transaction {finishTransaction.TransactionNumber} from the database, which was not open on the TSE anymore.",
                                    Type = $"{0x4445_0000_2000_0000:X}-{nameof(OpenTransaction)}",
                                    DataJson = JsonConvert.SerializeObject(finishTransaction)
                                }
                            );
                        }
                    }

                    await ProcessFailedFinishTransactionAsync(processReceiptResponse.Signatures).ConfigureAwait(false);
                    await ProcessFailedStartTransactionsAsync(processReceiptResponse.Signatures).ConfigureAwait(false);
                    _logger.LogDebug($"Closed SSCDFail-Mode, " +
                        $"FailedStartTransactionsCount: {(await _failedStartTransactionRepo.GetAsync().ConfigureAwait(false)).Count()}, " +
                        $"FailedFinishTransactionsCount: {(await _failedFinishTransactionRepo.GetAsync().ConfigureAwait(false)).Count()}, " +
                        $"OpenTransactionsCount: {(await _openTransactionRepo.GetAsync().ConfigureAwait(false)).Count()}");

                    var caption = $"TSE Kommunikation wiederhergestellt am {DateTime.UtcNow:G}.";
                    var data = $"{queueDE.SSCDFailCount} Aktionen ohne TSE Kommunikation im Zeitraum von {queueDE?.SSCDFailMoment:G} bis {DateTime.UtcNow:G}";

                    processReceiptResponse.Signatures.Add(
                            new SignaturItem()
                            {
                                ftSignatureType = 0x4445_0000_0000_0002,
                                ftSignatureFormat = (long) ifPOS.v0.SignaturItem.Formats.Text,
                                Caption = caption,
                                Data = data
                            }
                        );

                    actionJournals.Add(
                            new ftActionJournal()
                            {
                                ftActionJournalId = Guid.NewGuid(),
                                ftQueueId = queueItem.ftQueueId,
                                ftQueueItemId = queueItem.ftQueueItemId,
                                Moment = DateTime.UtcNow,
                                Priority = -1,
                                TimeStamp = 0,
                                Message = caption + data,
                                Type = $"{0x4445_0000_0000_0002:X}-{nameof(System.String)}",
                                DataJson = JsonConvert.SerializeObject(caption + data)
                            }
                        );

                    queueDE.SSCDFailCount = 0;
                    queueDE.SSCDFailMoment = null;
                    queueDE.SSCDFailQueueItemId = null;
                }

                if (queueDE.UsedFailedCount > 0)
                {
                    var caption = $"Ausfallsnacherfassung abgeschlossen am {DateTime.UtcNow:G}.";
                    var data = $"{queueDE.UsedFailedCount} Aktionen im Zeitraum von {queueDE.UsedFailedMomentMin:G} bis {queueDE.UsedFailedMomentMax:G}";

                    processReceiptResponse.Signatures.Add(
                            new SignaturItem()
                            {
                                ftSignatureType = 0x4445_0000_0000_0002,
                                ftSignatureFormat = (long) ifPOS.v0.SignaturItem.Formats.Text,
                                Caption = caption,
                                Data = data
                            }
                        );

                    actionJournals.Add(
                            new ftActionJournal()
                            {
                                ftActionJournalId = Guid.NewGuid(),
                                ftQueueId = queueItem.ftQueueId,
                                ftQueueItemId = queueItem.ftQueueItemId,
                                Moment = DateTime.UtcNow,
                                Priority = -1,
                                TimeStamp = 0,
                                Message = caption + data,
                                Type = $"{0x4445_0000_0000_0002:X}-{nameof(String)}",
                                DataJson = JsonConvert.SerializeObject(caption + data)
                            }
                        );

                    queueDE.UsedFailedCount = 0;
                    queueDE.UsedFailedMomentMin = null;
                    queueDE.UsedFailedMomentMax = null;
                    queueDE.UsedFailedQueueItemId = null;
                }

                if (request.IsTseInfoRequest())
                {
                    await UpdateTseInfoAsync(queueDE.ftSignaturCreationUnitDEId.GetValueOrDefault()).ConfigureAwait(false);
                    receiptResponse.ftStateData = await StateDataFactory.AppendTseInfoAsync(_deSSCDProvider.Instance, receiptResponse.ftStateData).ConfigureAwait(false);
                    receiptResponse.ftStateData = StateDataFactory.ApendOpenTransactionState(await _openTransactionRepo.GetAsync().ConfigureAwait(false), receiptResponse.ftStateData);
                }

                if (request.IsTseSelftestRequest())
                {
                    await _deSSCDProvider.Instance.ExecuteSelfTestAsync().ConfigureAwait(false);
                }

                if (request.IsTseTarDownloadRequest())
                {
                    await PerformTarFileExportAsync(queueItem, queue, queueDE, erase: true).ConfigureAwait(false);
                }

                if (request.IsTraining())
                {
                    processReceiptResponse.Signatures.Add(_signatureFactory.GetSignatureForTraining());
                }

                receiptResponse.ftSignatures = processReceiptResponse.Signatures.ToArray();
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

        protected async Task ProcessFailedFinishTransactionAsync(List<SignaturItem> signatures)
        {
            foreach (var failedFinishTransaction in (await _failedFinishTransactionRepo.GetAsync().ConfigureAwait(false)).ToList())
            {
                _logger.LogDebug($"Finishing SSCDFail-FinishTransaction: {failedFinishTransaction.cbReceiptReference}");
                try
                {
                    var request = failedFinishTransaction.Request ?? (await _queueItemRepository.GetAsync(failedFinishTransaction.ftQueueItemId).ConfigureAwait(false)).request;

                    var failedRequest = JsonConvert.DeserializeObject<ReceiptRequest>(request);
                    (var failedProcessType, var failedPayload) = _transactionPayloadFactory.CreateReceiptPayload(failedRequest);

                    _logger.LogDebug(JsonConvert.SerializeObject(failedFinishTransaction));
                    if (await _openTransactionRepo.ExistsAsync(failedFinishTransaction.cbReceiptReference).ConfigureAwait(false))
                    {
                        var openTransaction = (OpenTransaction) await _openTransactionRepo.GetAsync(failedFinishTransaction.cbReceiptReference).ConfigureAwait(false);
                        var finishTransactionResult = await _transactionFactory.PerformFinishTransactionRequestAsync(failedProcessType, failedPayload, failedFinishTransaction.ftQueueItemId, failedFinishTransaction.CashBoxIdentification, (ulong) openTransaction.TransactionNumber, true).ConfigureAwait(false);
                        await _openTransactionRepo.RemoveAsync(failedFinishTransaction.cbReceiptReference).ConfigureAwait(false);
                        signatures.AddRange(_signatureFactory.GetSignaturesForTransaction(openTransaction.StartTransactionSignatureBase64, finishTransactionResult, await GetCertificationIdentificationAsync().ConfigureAwait(false)));
                        _logger.LogDebug($"Finished TSE-Transaction: {finishTransactionResult.TransactionNumber}, TSE-Signature: {finishTransactionResult.SignatureData.SignatureBase64}");
                    }
                    _logger.LogDebug($"Finished SSCDFail-Transaction: {failedFinishTransaction.cbReceiptReference}, ProcessType: {failedProcessType}, Payload: {failedPayload}");
                }
                catch (Exception x)
                {
                    _logger.LogDebug(x, "Trying to finish TSE-Transaction");
                    throw;
                }
                await _failedFinishTransactionRepo.RemoveAsync(failedFinishTransaction.cbReceiptReference).ConfigureAwait(false);
            }
        }

        protected async Task ProcessFailedStartTransactionsAsync(List<SignaturItem> signatures)
        {
            foreach (var failedStartTransaction in (await _failedStartTransactionRepo.GetAsync().ConfigureAwait(false)).ToList())
            {
                _logger.LogDebug($"Starting SSCDFail-StartTransaction: {failedStartTransaction.cbReceiptReference}");
                try
                {
                    var startTransactionResult = await _transactionFactory.PerformStartTransactionRequestAsync(failedStartTransaction.ftQueueItemId, failedStartTransaction.CashBoxIdentification, true).ConfigureAwait(false);
                    await _openTransactionRepo.InsertOrUpdateTransactionAsync(new OpenTransaction
                    {
                        cbReceiptReference = failedStartTransaction.cbReceiptReference,
                        StartMoment = startTransactionResult.TimeStamp,
                        TransactionNumber = (long) startTransactionResult.TransactionNumber,
                        StartTransactionSignatureBase64 = startTransactionResult.SignatureData.SignatureBase64
                    }).ConfigureAwait(false);

                    signatures.Add(_signatureFactory.GetSignaturForStartTransaction(startTransactionResult));
                    _logger.LogDebug($"Started SSCDFail-Transaction: {failedStartTransaction.cbReceiptReference}, TransactionNumber: {startTransactionResult.TransactionNumber}");
                }
                catch (Exception x)
                {
                    _logger.LogDebug(x, "Exception while processing strated transactions");
                    throw;
                }

                await _failedStartTransactionRepo.RemoveAsync(failedStartTransaction.cbReceiptReference).ConfigureAwait(false);
            }
        }
    }
}
