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
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;

namespace fiskaltrust.Middleware.Localization.QueueDE.RequestCommands
{
    internal class InitiateScuSwitchReceiptCommand : RequestCommand
    {
        private readonly IActionJournalRepository _actionJournalRepository;

        public override string ReceiptName => "Initiate-SCU-switch receipt";

        public InitiateScuSwitchReceiptCommand(IActionJournalRepository actionJournalRepository, ILogger<RequestCommand> logger, SignatureFactoryDE signatureFactory,
            IDESSCDProvider deSSCDProvider, ITransactionPayloadFactory transactionPayloadFactory, IReadOnlyQueueItemRepository queueItemRepository,
            IConfigurationRepository configurationRepository, IJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration,
            IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo, IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo,
            IPersistentTransactionRepository<OpenTransaction> openTransactionRepo, ITarFileCleanupService tarFileCleanupService, QueueDEConfiguration queueDEConfiguration)
            : base(logger, signatureFactory, deSSCDProvider, transactionPayloadFactory, queueItemRepository, configurationRepository, journalDERepository,
                  middlewareConfiguration, failedStartTransactionRepo, failedFinishTransactionRepo, openTransactionRepo, tarFileCleanupService, queueDEConfiguration)
        {
            _actionJournalRepository = actionJournalRepository;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueDE queueDE, ReceiptRequest request, ftQueueItem queueItem)
        {
            ThrowIfNoImplicitFlow(request);
            ThrowIfTraining(request);

            var (processType, payload) = _transactionPayloadFactory.CreateReceiptPayload(request);
            var receiptResponse = CreateReceiptResponse(request, queueItem, queueDE);

            var ajs = await _actionJournalRepository.GetAsync().ConfigureAwait(false);
            var lastDailyClosingJournal = ajs.OrderBy(x => x.Moment).LastOrDefault(x => x.Type == "4445000000000007" || x.Type == "4445000008000007");
            var lastDailyClosingNumerator = lastDailyClosingJournal != null
                ? JsonConvert.DeserializeAnonymousType(lastDailyClosingJournal.DataJson, new { ftReceiptNumerator = 0L }).ftReceiptNumerator
                : -1;

            if (!request.IsInitiateScuSwitchReceiptForce() && ( lastDailyClosingJournal == null || lastDailyClosingNumerator != queue.ftReceiptNumerator))
            {
                var reachable = false;
                try
                {
                    await _deSSCDProvider.Instance.GetTseInfoAsync().ConfigureAwait(false);
                    reachable = true;
                }
                catch { }

                if (reachable)
                {
                    throw new Exception($"ReceiptCase {request.ftReceiptCase:X} (initiate-scu-switch-receipt) can only be called right after a daily-closing receipt." +
                        $"If a daily-closing receipt can not be done use the Initiate-ScuSwitch-Force-Flag. See https://link.fiskaltrust.cloud/market-de/force-scu-switch-flag for more details. ");
                }
            }

            var sourceScu = await _configurationRepository.GetSignaturCreationUnitDEAsync(queueDE.ftSignaturCreationUnitDEId.Value).ConfigureAwait(false);

            if (!sourceScu.IsSwitchSource() || string.IsNullOrEmpty(sourceScu.ModeConfigurationJson))
            {
                throw new Exception($"The source SCU is not set up correctly for an SCU switch in the local configuration. The SCU switch must be initiated properly in the fiskaltrust.Portal before sending this receipt. See https://link.fiskaltrust.cloud/market-de/scu-switch for more details. (Source SCU: {sourceScu?.ftSignaturCreationUnitDEId}, Mode: {sourceScu?.Mode}, ModeConfigurationJson: {sourceScu?.ModeConfigurationJson})");
            }

            var specifiedTargetScuId = JsonConvert.DeserializeAnonymousType(sourceScu.ModeConfigurationJson, new { TargetScuId = new Guid?() })?.TargetScuId;
            var targetScu = specifiedTargetScuId.HasValue ? await _configurationRepository.GetSignaturCreationUnitDEAsync(specifiedTargetScuId.Value).ConfigureAwait(false) : null;
            var specifiedSourceScuId = JsonConvert.DeserializeAnonymousType(targetScu?.ModeConfigurationJson, new { SourceScuId = new Guid?() })?.SourceScuId;

            if (targetScu == null || !targetScu.IsSwitchTarget() || !specifiedSourceScuId.HasValue || specifiedSourceScuId.Value != sourceScu.ftSignaturCreationUnitDEId)
            {
                throw new Exception($"The target SCU is not set up correctly for an SCU switch in the local configuration. The SCU switch must be initiated properly in the fiskaltrust.Portal before sending this receipt. See https://link.fiskaltrust.cloud/market-de/scu-switch for more details. (Source SCU: {sourceScu?.ftSignaturCreationUnitDEId}, Mode: {sourceScu?.Mode}, ModeConfigurationJson: {sourceScu?.ModeConfigurationJson}; Target SCU: {targetScu?.ftSignaturCreationUnitDEId}, Mode: {targetScu?.Mode}, ModeConfigurationJson: {targetScu?.ModeConfigurationJson})");
            }

            var actionJournals = new List<ftActionJournal>();
            var typeNumber = 0x4445000000000003;
            InitiateSCUSwitch notification;
            List<SignaturItem> signatures;

            ulong? transactionNumber;
            string serialnumberOctet;

            try
            {
                (var returnedTransactionNumber, var returnedSignatures, var clientId, var signatureAlgorithm, var publicKeyBase64, var retuenedSerialnumberOctet) = await ProcessOutOfOperationReceiptAsync(request.cbReceiptReference, processType, payload, queueItem, queueDE, request.IsModifyClientIdOnlyRequest()).ConfigureAwait(false);
                signatures = returnedSignatures;
                transactionNumber = returnedTransactionNumber;
                serialnumberOctet = retuenedSerialnumberOctet;

                notification = new InitiateSCUSwitch()
                {
                    CashBoxId = Guid.Parse(request.ftCashBoxID),
                    QueueId = queueItem.ftQueueId,
                    Moment = DateTime.UtcNow,
                    CashBoxIdentification = clientId,
                    SourceSCUId = sourceScu.ftSignaturCreationUnitDEId,
                    TargetSCUId = targetScu.ftSignaturCreationUnitDEId,
                    SourceSCUSignatureAlgorithm = signatureAlgorithm,
                    SourceSCUPublicKeyBase64 = publicKeyBase64,
                    SourceSCUSerialNumberOctet = serialnumberOctet,
                    Version = "V0"
                };

                signatures.Add(_signatureFactory.CreateInitiateScuSwitchSignature(queue.ftQueueId, clientId, serialnumberOctet));
                signatures.Add(
                    new SignaturItem()
                    {
                        ftSignatureType = typeNumber,
                        ftSignatureFormat = (long) ifPOS.v0.SignaturItem.Formats.AZTEC,
                        Caption = $"SCU von Queue getrennt. Kassenseriennummer: {clientId}, TSE-Seriennummer: {serialnumberOctet}, Queue-ID: {queue.ftQueueId}, SCU-ID: {sourceScu.ftSignaturCreationUnitDEId}",
                        Data = JsonConvert.SerializeObject(notification)
                    }
                );

                receiptResponse.ftStateData = await StateDataFactory.AppendTseInfoAsync(_deSSCDProvider.Instance, receiptResponse.ftStateData).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "TSE not reachable.");
                _logger.LogWarning("The TSE was not reachable. The SCU switch was initiated by using the data from the last access.");

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

                notification = new InitiateSCUSwitch()
                {
                    CashBoxId = Guid.Parse(request.ftCashBoxID),
                    QueueId = queueItem.ftQueueId,
                    Moment = DateTime.UtcNow,
                    CashBoxIdentification = queueDE.CashBoxIdentification,
                    SourceSCUId = sourceScu.ftSignaturCreationUnitDEId,
                    TargetSCUId = targetScu.ftSignaturCreationUnitDEId,
                    SourceSCUSignatureAlgorithm = signatureAlgorithm,
                    SourceSCUPublicKeyBase64 = publicKeyBase64,
                    SourceSCUSerialNumberOctet = serialnumberOctet,
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
                    Message = $"SCU von Queue getrennt. Kassenseriennummer: {queueDE.CashBoxIdentification}, TSE-Seriennummer: {serialnumberOctet}, Queue-ID: {queue.ftQueueId}, SCU-ID: {sourceScu.ftSignaturCreationUnitDEId}",
                    Type = $"{typeNumber:X}-{nameof(InitiateSCUSwitch)}",
                    DataJson = JsonConvert.SerializeObject(notification)
                }
            );

            receiptResponse.ftReceiptIdentification = request.GetReceiptIdentification(queue.ftReceiptNumerator, transactionNumber);
            receiptResponse.ftSignatures = signatures.ToArray();

            if (!request.IsTseTarDownloadBypass())
            {
                await PerformTarFileExportAsync(queueItem, queue, queueDE, erase: true).ConfigureAwait(false);
            }

            queueDE.ftSignaturCreationUnitDEId = null;
            await _configurationRepository.InsertOrUpdateQueueDEAsync(queueDE).ConfigureAwait(false);
            _logger.LogInformation("Disconnected from SCU with ID '{ScuId}'.", specifiedSourceScuId);
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
