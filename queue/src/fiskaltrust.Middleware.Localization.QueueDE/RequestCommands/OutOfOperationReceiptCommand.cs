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
    internal class OutOfOperationReceiptCommand : RequestCommand
    {
        public override string ReceiptName => "Out-of-operation receipt";

        public OutOfOperationReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryDE signatureFactory, IDESSCDProvider deSSCDProvider, 
            ITransactionPayloadFactory transactionPayloadFactory, IReadOnlyQueueItemRepository queueItemRepository, IConfigurationRepository configurationRepository, 
            IJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration, IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo, 
            IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo, IPersistentTransactionRepository<OpenTransaction> openTransactionRepo, ITarFileCleanupService tarFileCleanupService, QueueDEConfiguration queueDEConfiguration, IMasterDataService masterDataService)
            : base(logger, signatureFactory, deSSCDProvider, transactionPayloadFactory, queueItemRepository, configurationRepository, journalDERepository,
                  middlewareConfiguration, failedStartTransactionRepo, failedFinishTransactionRepo, openTransactionRepo, tarFileCleanupService, queueDEConfiguration, masterDataService)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueDE queueDE, ReceiptRequest request, ftQueueItem queueItem)
        {
            _logger.LogTrace("OutOfOperationReceiptCommand.ExecuteAsync [enter].");
            ThrowIfNoImplicitFlow(request);
            ThrowIfTraining(request);


            if (!request.IsTseTarDownloadBypass())
            {
                await PerformTarFileExportAsync(queueItem, queue, queueDE, erase: true).ConfigureAwait(false);
            }

            var (processType, payload) = _transactionPayloadFactory.CreateReceiptPayload(request);
            var receiptResponse = CreateReceiptResponse(request, queueItem, queueDE);


            var actionJournals = new List<ftActionJournal>();
            var typeNumber = 0x4445000000000004;
            DeactivateQueueSCU notification;
            List<SignaturItem> signatures;

            ulong? transactionNumber;
            string serialnumberOctet;

            try
            {
                (var returnedTransactionNumber, var returnedSignatures, var clientId, var signatureAlgorithm, var publicKeyBase64, var returnedSerialnumberOctet) = await ProcessOutOfOperationReceiptAsync(request.cbReceiptReference, processType, payload, queueItem, queueDE, request.IsModifyClientIdOnlyRequest()).ConfigureAwait(false);
                signatures = returnedSignatures;
                transactionNumber = returnedTransactionNumber;
                serialnumberOctet = returnedSerialnumberOctet;

                notification = new DeactivateQueueSCU()
                {
                    CashBoxId = Guid.Parse(request.ftCashBoxID),
                    QueueId = queueItem.ftQueueId,
                    Moment = DateTime.UtcNow,
                    CashBoxIdentification = clientId,
                    SCUId = queueDE.ftSignaturCreationUnitDEId.GetValueOrDefault(),
                    SCUSignatureAlgorithm = signatureAlgorithm,
                    SCUPublicKeyBase64 = publicKeyBase64,
                    SCUSerialNumberBase64 = serialnumberOctet,
                    IsStopReceipt = true,
                    Version = "V0"
                };

                signatures.Add(_signatureFactory.CreateOutOfOperationSignature(queue.ftQueueId, clientId, serialnumberOctet));
                signatures.Add(
                    new SignaturItem()
                    {
                        ftSignatureType = typeNumber,
                        ftSignatureFormat = (long) ifPOS.v0.SignaturItem.Formats.AZTEC,
                        Caption = $"Außer-Betriebnahme-Beleg. Kassenseriennummer: {clientId}, TSE-Seriennummer: {serialnumberOctet}, Queue-ID: {queue.ftQueueId}",
                        Data = JsonConvert.SerializeObject(notification)
                    }
                );

                receiptResponse.ftStateData = await StateDataFactory.AppendTseInfoAsync(_deSSCDProvider.Instance, receiptResponse.ftStateData).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "TSE not reachable.");
                _logger.LogWarning("The TSE was not reachable. The Queue was deactivated by using the data from the last access.");

                var signatureAlgorithm = "";
                var publicKeyBase64 = "";
                try
                {
                    var scuDE = await _configurationRepository.GetSignaturCreationUnitDEAsync(queueDE.ftSignaturCreationUnitDEId.Value).ConfigureAwait(false);
                    var tseInfo = JsonConvert.DeserializeObject<TseInfo>(scuDE.TseInfoJson);

                    serialnumberOctet = tseInfo.SerialNumberOctet;
                    signatureAlgorithm = tseInfo.SignatureAlgorithm;
                    publicKeyBase64 = tseInfo.PublicKeyBase64;

                }
                catch
                {
                    serialnumberOctet = "";
                }

                notification = new DeactivateQueueSCU()
                {
                    CashBoxId = Guid.Parse(request.ftCashBoxID),
                    QueueId = queueItem.ftQueueId,
                    Moment = DateTime.UtcNow,
                    CashBoxIdentification = queueDE.CashBoxIdentification,
                    SCUId = queueDE.ftSignaturCreationUnitDEId.GetValueOrDefault(),
                    SCUSignatureAlgorithm = signatureAlgorithm,
                    SCUPublicKeyBase64 = publicKeyBase64,
                    SCUSerialNumberBase64 = serialnumberOctet,
                    IsStopReceipt = true,
                    Version = "V0"
                };

                signatures = new List<SignaturItem>();
                transactionNumber = null;
            }

            actionJournals.Add(
                new ftActionJournal()
                {
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueId = queueItem.ftQueueId,
                    ftQueueItemId = queueItem.ftQueueItemId,
                    Moment = DateTime.UtcNow,
                    Priority = -1,
                    TimeStamp = 0,
                    Message = $"Außer-Betriebnahme-Beleg. Kassenseriennummer: {queueDE.CashBoxIdentification}, {(serialnumberOctet != null ? $"TSE-Seriennummer: {serialnumberOctet}, " : "")}Queue-ID: {queue.ftQueueId}",
                    Type = $"{typeNumber:X}-{nameof(DeactivateQueueSCU)}",
                    DataJson = JsonConvert.SerializeObject(notification)
                }
            );


            receiptResponse.ftReceiptIdentification = request.GetReceiptIdentification(queue.ftReceiptNumerator, transactionNumber);
            receiptResponse.ftSignatures = signatures.ToArray();

            queue.StopMoment = DateTime.UtcNow;
            _logger.LogTrace("OutOfOperationReceiptCommand.ExecuteAsync [exit].");
            return new RequestCommandResponse()
            {
                ActionJournals = actionJournals,
                ReceiptResponse = receiptResponse,
                Signatures = signatures,
                TransactionNumber = transactionNumber
            };
        }
    }
}
