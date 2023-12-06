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

namespace fiskaltrust.Middleware.Localization.QueueDE.RequestCommands
{
    public class UsedFailedReceiptCommand : RequestCommand
    {
        private const long LATE_SIGNING_MODE_FLAG = 0x0000_0000_0000_0008;

        public override string ReceiptName => "Failed receipt";

        public UsedFailedReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryDE signatureFactory, IDESSCDProvider deSSCDProvider,
            ITransactionPayloadFactory transactionPayloadFactory, IReadOnlyQueueItemRepository queueItemRepository, IConfigurationRepository configurationRepository,
            IJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration, IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo,
            IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo, IPersistentTransactionRepository<OpenTransaction> openTransactionRepo, ITarFileCleanupService tarFileCleanupService, QueueDEConfiguration queueDEConfiguration, IMasterDataService masterDataService)
            : base(logger, signatureFactory, deSSCDProvider, transactionPayloadFactory, queueItemRepository, configurationRepository, journalDERepository,
                  middlewareConfiguration, failedStartTransactionRepo, failedFinishTransactionRepo, openTransactionRepo, tarFileCleanupService, queueDEConfiguration, masterDataService)
        { }

        public override Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueDE queueDE, ReceiptRequest request, ftQueueItem queueItem)
        {
            var receiptResponse = CreateReceiptResponse(request, queueItem, queueDE);

            if (!queueDE.UsedFailedQueueItemId.HasValue)
            {
                queueDE.UsedFailedQueueItemId = queueItem.ftQueueItemId;
            }
            if (!queueDE.UsedFailedMomentMin.HasValue || queueDE.UsedFailedMomentMin.Value > request.cbReceiptMoment)
            {
                queueDE.UsedFailedMomentMin = request.cbReceiptMoment;
            }
            if (!queueDE.UsedFailedMomentMax.HasValue || queueDE.UsedFailedMomentMax.Value < request.cbReceiptMoment)
            {
                queueDE.UsedFailedMomentMax = request.cbReceiptMoment;
            }
            queueDE.UsedFailedCount++;

            var actionJournals = new List<ftActionJournal>();

            receiptResponse.ftState += LATE_SIGNING_MODE_FLAG;

            receiptResponse.ftReceiptIdentification = request.GetReceiptIdentification(queue.ftReceiptNumerator, null);

            var signatures = new List<SignaturItem>()
            {
                new SignaturItem
                {
                    ftSignatureType = (long) SignatureTypesDE.ArchivingRequired | 0x4445_0000_0000_0000,
                    ftSignatureFormat =(long) SignaturItem.Formats.Text,
                    Caption = $"Ausfallsnacherfassung vom {request.cbReceiptMoment:G}",
                    Data = "Elektronisches Aufzeichnungssystem ausgefallen"
                }
            };
            receiptResponse.ftSignatures = signatures.ToArray();
            return Task.FromResult(new RequestCommandResponse()
            {
                ActionJournals = actionJournals,
                ReceiptResponse = receiptResponse,
                Signatures = signatures,
            });
        }
    }
}