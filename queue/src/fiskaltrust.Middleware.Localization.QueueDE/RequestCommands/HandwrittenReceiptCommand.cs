using System;
using System.Collections.Generic;
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

namespace fiskaltrust.Middleware.Localization.QueueDE.RequestCommands
{
    public class HandwrittenReceiptCommand : RequestCommand
    {
        public override string ReceiptName => "Handwritten receipt";

        public HandwrittenReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryDE signatureFactory, IDESSCDProvider deSSCDProvider, ITransactionPayloadFactory transactionPayloadFactory, IReadOnlyQueueItemRepository queueItemRepository, IConfigurationRepository configurationRepository, IJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration, IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo, IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo, IPersistentTransactionRepository<OpenTransaction> openTransactionRepo, ITarFileCleanupService tarFileCleanupService) : base(logger, signatureFactory, deSSCDProvider, transactionPayloadFactory, queueItemRepository, configurationRepository, journalDERepository, middlewareConfiguration, failedStartTransactionRepo, failedFinishTransactionRepo, openTransactionRepo, tarFileCleanupService)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueDE queueDE, IDESSCD client, ReceiptRequest request, ftQueueItem queueItem)
        {
            var receiptResponse = CreateReceiptResponse(request, queueItem, queueDE);

            var actionJournals = new List<ftActionJournal>();

            receiptResponse.ftReceiptIdentification = request.GetReceiptIdentification(queue.ftReceiptNumerator, null);

            var signatures = new List<SignaturItem>()
            {
                new SignaturItem
                {
                    ftSignatureType = (long) SignatureTypesDE.ArchivingRequired | 0x4445_0000_0000_0000,
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    Caption = $"Handschriftbelegerfassung vom {request.cbReceiptMoment:G}",
                    Data = ""
                }
            };
            receiptResponse.ftSignatures = signatures.ToArray();
            return await Task.FromResult(new RequestCommandResponse()
            {
                ReceiptResponse = receiptResponse,
                Signatures = signatures,
                ActionJournals = actionJournals,
            }).ConfigureAwait(false);
        }
    }
}