using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueDE.Models;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v1.de;
using System.Linq;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;

namespace fiskaltrust.Middleware.Localization.QueueDE.RequestCommands
{
    internal class FinishScuSwitchReceiptCommand : RequestCommand
    {
        private readonly IActionJournalRepository _actionJournalRepository;
        public override string ReceiptName => "Finish-SCU-switch receipt";

        public FinishScuSwitchReceiptCommand(IActionJournalRepository actionJournalRepository, ILogger<RequestCommand> logger,
            SignatureFactoryDE signatureFactory, IDESSCDProvider deSSCDProvider, ITransactionPayloadFactory transactionPayloadFactory, IReadOnlyQueueItemRepository queueItemRepository,
            IConfigurationRepository configurationRepository, IJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration,
            IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo, IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo,
            IPersistentTransactionRepository<OpenTransaction> openTransactionRepo, ITarFileCleanupService tarFileCleanupService, QueueDEConfiguration queueDEConfiguration, IMasterDataService masterDataService)
            : base(logger, signatureFactory, deSSCDProvider, transactionPayloadFactory, queueItemRepository, configurationRepository, journalDERepository,
                  middlewareConfiguration, failedStartTransactionRepo, failedFinishTransactionRepo, openTransactionRepo, tarFileCleanupService, queueDEConfiguration, masterDataService)
        {
            _actionJournalRepository = actionJournalRepository;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueDE queueDE, ReceiptRequest request, ftQueueItem queueItem)
        {
            var (processType, payload) = _transactionPayloadFactory.CreateReceiptPayload(request);
            var receiptResponse = CreateReceiptResponse(request, queueItem, queueDE);

            ThrowIfNoImplicitFlow(request);
            ThrowIfTraining(request);

            if (queueDE.ftSignaturCreationUnitDEId != null)
            {
                throw new Exception($"The SCU switch must be initiated with a initiate-scu-switch receipt. See https://link.fiskaltrust.cloud/market-de/scu-switch for more details.");
            }

            var ajs = await _actionJournalRepository.GetAsync().ConfigureAwait(false);
            var lastInitiateSwitchJournal = ajs.OrderBy(x => x.Moment).LastOrDefault(x => x.Type == $"{0x4445000000000003:X}-{nameof(InitiateSCUSwitch)}");
            var initiateSwitchNotification = !string.IsNullOrEmpty(lastInitiateSwitchJournal.DataJson)
                ? JsonConvert.DeserializeObject<InitiateSCUSwitch>(lastInitiateSwitchJournal.DataJson)
                : null;
            if (initiateSwitchNotification == null)
            {
                throw new Exception($"The SCU switch must be initiated with a initiate-scu-switch receipt. See https://link.fiskaltrust.cloud/market-de/scu-switch for more details.");
            }

            queueDE.ftSignaturCreationUnitDEId = initiateSwitchNotification.TargetSCUId;
            await _configurationRepository.InsertOrUpdateQueueDEAsync(queueDE).ConfigureAwait(false);
            _logger.LogInformation("Connected to SCU with ID '{ScuId}'.", queueDE.ftSignaturCreationUnitDEId);

            try
            {
                await _deSSCDProvider.RegisterCurrentScuAsync().ConfigureAwait(false);
                _certificationIdentification = null;

                (var transactionNumber, var signatures, var clientId, var signatureAlgorithm, var publicKeyBase64, var serialnumberOctet) = await ProcessInitialOperationReceiptAsync(request.cbReceiptReference, processType, payload, queueItem, queueDE, false).ConfigureAwait(false);

                var actionJournals = new List<ftActionJournal>();
                var typeNumber = 0x4445000000000003;

                var notification = new FinishSCUSwitch()
                {
                    CashBoxId = Guid.Parse(request.ftCashBoxID),
                    QueueId = queueItem.ftQueueId,
                    Moment = DateTime.UtcNow,
                    CashBoxIdentification = clientId,
                    SourceSCUId = initiateSwitchNotification.SourceSCUId,
                    TargetSCUId = initiateSwitchNotification.TargetSCUId,
                    TargetSCUSignatureAlgorithm = signatureAlgorithm,
                    TargetSCUPublicKeyBase64 = publicKeyBase64,
                    TargetSCUSerialNumberOctet = serialnumberOctet,
                    Version = "V0"
                };

                signatures.Add(_signatureFactory.CreateFinishScuSwitchSignature(queue.ftQueueId, clientId, serialnumberOctet));
                signatures.Add(
                        new SignaturItem()
                        {
                            ftSignatureType = typeNumber,
                            ftSignatureFormat = (long) ifPOS.v0.SignaturItem.Formats.AZTEC,
                            Caption = $"SCU mit Queue verbunden. Kassenseriennummer: {clientId}, TSE-Seriennummer: {serialnumberOctet}, Queue-ID: {queue.ftQueueId}, SCU-ID: {initiateSwitchNotification.TargetSCUId}",
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
                            Message = $"SCU mit Queue verbunden. Kassenseriennummer: {clientId}, TSE-Seriennummer: {serialnumberOctet}, Queue-ID: {queue.ftQueueId}, SCU-ID: {initiateSwitchNotification.TargetSCUId}",
                            Type = $"{typeNumber:X}-{nameof(FinishSCUSwitch)}",
                            DataJson = JsonConvert.SerializeObject(notification)
                        }
                    );

                receiptResponse.ftReceiptIdentification = request.GetReceiptIdentification(queue.ftReceiptNumerator, transactionNumber);
                receiptResponse.ftSignatures = signatures.ToArray();
                receiptResponse.ftStateData = await StateDataFactory.AppendTseInfoAsync(_deSSCDProvider.Instance, receiptResponse.ftStateData).ConfigureAwait(false);
                return new RequestCommandResponse()
                {
                    ActionJournals = actionJournals,
                    ReceiptResponse = receiptResponse,
                    Signatures = signatures,
                    TransactionNumber = transactionNumber
                };
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name == RETRYPOLICYEXCEPTION_NAME)
                {
                    _logger.LogDebug(ex, "TSE not reachable.");
                }

                // Reset the SCU switch so that the receipt is repeatable
                queueDE.ftSignaturCreationUnitDEId = null;
                await _configurationRepository.InsertOrUpdateQueueDEAsync(queueDE).ConfigureAwait(false);

                throw;
            }
        }
    }
}
