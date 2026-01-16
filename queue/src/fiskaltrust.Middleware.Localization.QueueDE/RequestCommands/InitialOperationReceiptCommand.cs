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
using Newtonsoft.Json;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.storage.serialization.DE.V0;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueDE.RequestCommands
{
    internal class InitialOperationReceiptCommand : RequestCommand
    {
        public override string ReceiptName => "Initial-operation receipt";

        public InitialOperationReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryDE signatureFactory, IDESSCDProvider deSSCDProvider, ITransactionPayloadFactory transactionPayloadFactory, IReadOnlyQueueItemRepository queueItemRepository, IConfigurationRepository configurationRepository, IJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration, IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo, IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo, IPersistentTransactionRepository<OpenTransaction> openTransactionRepo, ITarFileCleanupService tarFileCleanupService, QueueDEConfiguration queueDEConfiguration, IMasterDataService masterDataService) : base(logger, signatureFactory, deSSCDProvider, transactionPayloadFactory, queueItemRepository, configurationRepository, journalDERepository, middlewareConfiguration, failedStartTransactionRepo, failedFinishTransactionRepo, openTransactionRepo, tarFileCleanupService, queueDEConfiguration, masterDataService)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueDE queueDE, ReceiptRequest request, ftQueueItem queueItem)
        {
            _logger.LogTrace("InitialOperationReceiptCommand.ExecuteAsync [enter].");
            ThrowIfNoImplicitFlow(request);
            ThrowIfTraining(request);

            var (processType, payload) = _transactionPayloadFactory.CreateReceiptPayload(request);
            var receiptResponse = CreateReceiptResponse(request, queueItem, queueDE);

            try
            {
                var actionJournals = new List<ftActionJournal>();

                if (queue.IsNew())
                {

                    (var transactionNumber, var signatures, var clientId, var signatureAlgorithm, var publicKeyBase64, var serialnumberOctet) = await ProcessInitialOperationReceiptAsync(request.cbReceiptReference, processType, payload, queueItem, queueDE, request.IsModifyClientIdOnlyRequest()).ConfigureAwait(false);

                    var typeNumber = 0x4445000000000003;

                    var notification = new ActivateQueueSCU()
                    {
                        CashBoxId = Guid.Parse(request.ftCashBoxID),
                        QueueId = queueItem.ftQueueId,
                        Moment = DateTime.UtcNow,
                        CashBoxIdentification = clientId,
                        SCUId = queueDE.ftSignaturCreationUnitDEId.GetValueOrDefault(),
                        SCUSignatureAlgorithm = signatureAlgorithm,
                        SCUPublicKeyBase64 = publicKeyBase64,
                        SCUSerialNumberBase64 = serialnumberOctet,
                        IsStartReceipt = true,
                        Version = "V0"
                    };

                    signatures.Add(_signatureFactory.CreateInitialOperationSignature(queue.ftQueueId, clientId, serialnumberOctet));
                    signatures.Add(
                            new SignaturItem()
                            {
                                ftSignatureType = typeNumber,
                                ftSignatureFormat = (long) fiskaltrust.ifPOS.v0.SignaturItem.Formats.AZTEC,
                                Caption = $"In-Betriebnahme-Beleg. Kassenseriennummer: {clientId}, TSE-Seriennummer: {serialnumberOctet}, Queue-ID: {queue.ftQueueId}",
                                Data = JsonConvert.SerializeObject(notification)
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
                                Message = $"In-Betriebnahme-Beleg. Kassenseriennummer: {clientId}, TSE-Seriennummer: {serialnumberOctet}, Queue-ID: {queue.ftQueueId}",
                                Type = $"{typeNumber:X}-{nameof(ActivateQueueSCU)}",
                                DataJson = JsonConvert.SerializeObject(notification)
                            }
                        );

                    receiptResponse.ftReceiptIdentification = request.GetReceiptIdentification(queue.ftReceiptNumerator, transactionNumber);
                    receiptResponse.ftSignatures = signatures.ToArray();
                    (receiptResponse.ftStateData,_) = await StateDataFactory.AppendTseInfoAsync(_deSSCDProvider.Instance, receiptResponse.ftStateData).ConfigureAwait(false);
                    queue.StartMoment = DateTime.UtcNow;
                    return new RequestCommandResponse()
                    {
                        ActionJournals = actionJournals,
                        ReceiptResponse = receiptResponse,
                        Signatures = signatures,
                        TransactionNumber = transactionNumber
                    };
                }
                else
                {
                    var actionJournalEntry = new ftActionJournal()
                    {
                        ftActionJournalId = Guid.NewGuid(),
                        ftQueueId = queueItem.ftQueueId,
                        ftQueueItemId = queueItem.ftQueueItemId,
                        Moment = DateTime.UtcNow,
                        Message = $"Queue {queue.ftQueueId} is activated, initial-operations-receipt can not be executed."
                    };
                    if (queue.IsDeactivated())
                    {
                        actionJournalEntry.Message = $"Queue {queue.ftQueueId} is de-activated, initial-operations-receipt can not be executed.";
                        actionJournals.Add(actionJournalEntry);
                        return await ProcessSSCDFailedReceiptRequest(request, queueItem, queue, queueDE, actionJournals).ConfigureAwait(false);
                    }
                    actionJournals.Add(actionJournalEntry);
                    var processReceiptResponse = await ProcessReceiptAsync(request.cbReceiptReference, processType, payload, queueItem, queueDE).ConfigureAwait(false);
                    receiptResponse.ftReceiptIdentification = request.GetReceiptIdentification(queue.ftReceiptNumerator, processReceiptResponse.TransactionNumber);
                    receiptResponse.ftSignatures = processReceiptResponse.Signatures.ToArray();
                    return new RequestCommandResponse()
                    {
                        ActionJournals = actionJournals,
                        ReceiptResponse = receiptResponse,
                        Signatures = processReceiptResponse.Signatures,
                        TransactionNumber = processReceiptResponse.TransactionNumber
                    };
                }
            }
            catch (Exception ex) when (ex.GetType().Name == RETRYPOLICYEXCEPTION_NAME)
            {
                _logger.LogDebug(ex, "TSE not reachable.");
                throw;
            }
            finally
            {
                _logger.LogTrace("InitialOperationReceiptCommand.ExecuteAsync [exit].");
            }
        }
    }
}
