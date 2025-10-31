using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueDE.Models;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using System.Linq;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Contracts.Data;

namespace fiskaltrust.Middleware.Localization.QueueDE.RequestCommands
{
    internal class DailyClosingReceiptCommand : ClosingReceiptCommand
    {
        public override string ReceiptName => "Daily-closing receipt";
        private readonly QueueDEConfiguration _queueDEConfiguration;

        public DailyClosingReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryDE signatureFactory, IDESSCDProvider deSSCDProvider, ITransactionPayloadFactory transactionPayloadFactory, IReadOnlyQueueItemRepository queueItemRepository, IConfigurationRepository configurationRepository, IJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration, IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo, IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo, IPersistentTransactionRepository<OpenTransaction> openTransactionRepo, ITarFileCleanupService tarFileCleanupService, QueueDEConfiguration queueDEConfiguration, IMasterDataService masterDataService) : base(logger, signatureFactory, deSSCDProvider, transactionPayloadFactory, queueItemRepository, configurationRepository, journalDERepository, middlewareConfiguration, failedStartTransactionRepo, failedFinishTransactionRepo, openTransactionRepo, tarFileCleanupService, queueDEConfiguration, masterDataService)
        {
            _queueDEConfiguration = queueDEConfiguration;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueDE queueDE, ReceiptRequest request, ftQueueItem queueItem)
        {
            _logger.LogTrace("DailyClosingReceiptCommand.ExecuteAsync [enter].");
            ThrowIfNoImplicitFlow(request);
            ThrowIfTraining(request);

            var (processType, payload) = _transactionPayloadFactory.CreateReceiptPayload(request);
            var receiptResponse = CreateReceiptResponse(request, queueItem, queueDE);

            var actionJournals = new List<ftActionJournal>();

            try
            {
                var openTransactions = (await _openTransactionRepo.GetAsync().ConfigureAwait(false)).ToList();

                _logger.LogTrace("DailyClosingReceiptCommand.ExecuteAsync Section RemoveOpenTransactionsWhichAreNotOnTse [enter].");
                var tseInfo = await _deSSCDProvider.Instance.GetTseInfoAsync().ConfigureAwait(false);
                var openTransactionsNotExistingOnTse = tseInfo.CurrentStartedTransactionNumbers != null
                    ? openTransactions.Where(ot => ot != null && !tseInfo.CurrentStartedTransactionNumbers.Contains((ulong) ((OpenTransaction) ot).TransactionNumber))
                    : openTransactions;
                foreach (var openTransaction in openTransactionsNotExistingOnTse)
                {
                    _logger.LogWarning($"The Middleware database contained the started transaction {openTransaction.TransactionNumber}, which is not marked as open on the TSE. The reference to the open transaction was removed from the database.");
                    await _openTransactionRepo.RemoveAsync(openTransaction.cbReceiptReference).ConfigureAwait(false);

                    actionJournals.Add(
                        new ftActionJournal
                        {
                            ftActionJournalId = Guid.NewGuid(),
                            ftQueueId = queueItem.ftQueueId,
                            ftQueueItemId = queueItem.ftQueueItemId,
                            Moment = DateTime.UtcNow,
                            Priority = -1,
                            TimeStamp = 0,
                            Message = $"Removed open transaction {openTransaction.TransactionNumber} from the database, which was not open on the TSE anymore.",
                            Type = $"{0x4445_0000_2000_0000:X}-{nameof(OpenTransaction)}",
                            DataJson = JsonConvert.SerializeObject(openTransaction)
                        }
                    );
                }

                openTransactions = (await _openTransactionRepo.GetAsync().ConfigureAwait(false)).ToList();
                _logger.LogTrace("DailyClosingReceiptCommand.ExecuteAsync Section RemoveOpenTransactionsWhichAreNotOnTse [exit].");

                if (request.HasFailOnOpenTransactionsFlag() && openTransactions.Any())
                {
                    throw new ArgumentException($"The ftReceiptCaseFlag '0x0000000010000000' was set, and {openTransactions.Count()} open transactions exist. If you want these transactions to be closed automatically, omit this flag.");
                }

                _logger.LogTrace("DailyClosingReceiptCommand.ExecuteAsync Section openTransactions [enter].");
                var openSignatures = new List<SignaturItem>();
                foreach (var openTransaction in openTransactions)
                {
                    (var openProcessType, var openPayload) = _transactionPayloadFactory.CreateAutomaticallyCanceledReceiptPayload();
                    var finishResult = await _transactionFactory.PerformFinishTransactionRequestAsync(openProcessType, openPayload, queueItem.ftQueueItemId, queueDE.CashBoxIdentification, (ulong) openTransaction.TransactionNumber).ConfigureAwait(false);
                    openSignatures.AddRange(_signatureFactory.GetSignaturesForFinishTransaction(finishResult));
                    await _openTransactionRepo.RemoveAsync(openTransaction.cbReceiptReference).ConfigureAwait(false);
                }
                _logger.LogTrace("DailyClosingReceiptCommand.ExecuteAsync Section openTransactions [exit].");


                if (_queueDEConfiguration.CloseOpenTSETransactionsOnDailyClosing)
                {
                    //remove all transactions on the tse to enable full tar deletion
                    var tseinfo = await _deSSCDProvider.Instance.GetTseInfoAsync().ConfigureAwait(false);
                    if (tseinfo.CurrentStartedTransactionNumbers != null)
                    {
                        foreach (var openTransactionNumber in tseinfo.CurrentStartedTransactionNumbers)
                        {
                            (var openProcessType, var openPayload) = _transactionPayloadFactory.CreateAutomaticallyCanceledReceiptPayload();
                            var finishResult = await _transactionFactory.PerformFinishTransactionRequestAsync(openProcessType, openPayload, queueItem.ftQueueItemId, queueDE.CashBoxIdentification, openTransactionNumber).ConfigureAwait(false);
                            openSignatures.AddRange(_signatureFactory.GetSignaturesForFinishTransaction(finishResult));
                        }
                    }
                }

                var processReceiptResponse = await ProcessReceiptAsync(request.cbReceiptReference, processType, payload, queueItem, queueDE).ConfigureAwait(false);
                queueDE.DailyClosingNumber++;

                receiptResponse.ftStateData = await StateDataFactory.AppendMasterDataAsync(_masterDataService, receiptResponse.ftStateData).ConfigureAwait(false);
                receiptResponse.ftStateData = StateDataFactory.AppendDailyClosingNumber(queueDE.DailyClosingNumber, receiptResponse.ftStateData);
                receiptResponse.ftReceiptIdentification = request.GetReceiptIdentification(queue.ftReceiptNumerator, processReceiptResponse.TransactionNumber);
                receiptResponse.ftSignatures = processReceiptResponse.Signatures.Concat(openSignatures).ToArray();

                if (!request.IsTseTarDownloadBypass())
                {
                    await PerformTarFileExportAsync(queueItem, queue, queueDE, erase: true).ConfigureAwait(false);
                }

                await UpdateTseInfoAsync(queueDE.ftSignaturCreationUnitDEId.GetValueOrDefault()).ConfigureAwait(false);
                (var masterDataChanged, var message, var type) = await UpdateMasterData(request);
                actionJournals.AddRange(CreateClosingActionJournals(queueItem, queue, processReceiptResponse.TransactionNumber, masterDataChanged, message, type, queueDE.DailyClosingNumber));

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
                return await ProcessSSCDFailedReceiptRequest(request, queueItem, queue, queueDE, actionJournals).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "An exception occured while processing this request.");
                return await ProcessSSCDFailedReceiptRequest(request, queueItem, queue, queueDE, actionJournals).ConfigureAwait(false);
            }
            finally
            {
                _logger.LogTrace("DailyClosingReceiptCommand.ExecuteAsync [exit].");
            }
        }
    }
}
