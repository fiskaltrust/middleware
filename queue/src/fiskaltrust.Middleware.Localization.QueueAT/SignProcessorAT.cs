using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Models;
using fiskaltrust.Middleware.Localization.QueueAT.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueAT.RequestCommands.Factories;
using fiskaltrust.Middleware.Queue;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Queue.Extensions;

namespace fiskaltrust.Middleware.Localization.QueueDE
{
    public class SignProcessorAT : IMarketSpecificSignProcessor
    {
        private readonly MiddlewareConfiguration _middlewareConfiguration;
        private readonly SignatureFactory _signatureFactory;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IJournalATRepository _journalATRepository;
        private readonly IRequestCommandFactory _requestCommandFactory;


        public SignProcessorAT(MiddlewareConfiguration middlewareConfiguration, SignatureFactory signatureFactory, IConfigurationRepository configurationRepository,
            IJournalATRepository journalATRepository, IRequestCommandFactory requestCommandFactory)
        {
            _middlewareConfiguration = middlewareConfiguration;
            _signatureFactory = signatureFactory;
            _configurationRepository = configurationRepository;
            _journalATRepository = journalATRepository;
            _requestCommandFactory = requestCommandFactory;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            var queueAT = await _configurationRepository.GetQueueATAsync(queueItem.ftQueueId).ConfigureAwait(false);

            //if (sscd == null || sscd.Count == 0)
            //{
            // TODO throw
            //    _logger.LogInformation($"No SCU connected.");
            //    journalAT = null;
            //    signed = false;
            //    return false;
            //}

            //if (!queueAT .ftSignaturCreationUnitDEId.HasValue && !queue.IsActive())
            //{
            //    throw new NullReferenceException(nameof(queueDE.ftSignaturCreationUnitDEId));
            //}

            //if (!string.IsNullOrEmpty(queueAT.ClosedSystemKind))
            //{
            //    throw new NotImplementedException();
            //}
            //if (string.IsNullOrWhiteSpace(queueAT.LastSignatureZDA) || string.IsNullOrWhiteSpace(queueAT.LastSignatureCertificateSerialNumber))
            //{
            //    _logger.LogInformation($"No signatur-creation-unit and previous certificate-serialnumber invalid ({queueAT.LastSignatureZDA} 0x{queueAT.LastSignatureCertificateSerialNumber})", "", queueItem.ftQueueItemId, true, false);
            //    journalAT = null;
            //    signed = false;
            //    return false;
            //}

            //if (string.IsNullOrWhiteSpace(queueAT.CashBoxIdentification) || queueAT.CashBoxIdentification.Contains("_"))
            //{
            //    parrentWorker.Log($"Cashboxidentification invalid ({queueAT.CashBoxIdentification})", "", queueItem.ftQueueItemId, true, false);
            //    journalAT = null;
            //    signed = false;
            //    return false;
            //}

            ////check for sscdfailed state
            //CheckSSCDFailedAT(ref Queue, ref QueueAT, ref QueueItem, ref JournalAT, ref localActionJournal, ref ReceiptRequest, ref ReceiptResponse);

            ////check for usedfailed receipt and state
            //CheckUsedFailedAT(ref Queue, ref QueueAT, ref QueueItem, ref JournalAT, ref localActionJournal, ref ReceiptRequest, ref ReceiptResponse);

            ////check for usedmobile receipt and state
            //CheckUsedMobileAT(ref Queue, ref QueueAT, ref QueueItem, ref JournalAT, ref localActionJournal, ref ReceiptRequest, ref ReceiptResponse);

            ////check for last settlement
            //CheckNewMonthAT(ref Queue, ref QueueAT, ref QueueItem, ref JournalAT, ref localActionJournal, ref ReceiptRequest, ref ReceiptResponse);

            var requestCommandResponse = await PerformReceiptRequest(request, queueItem, queue, queueAT).ConfigureAwait(false);
            if (_middlewareConfiguration.IsSandbox)
            {
                requestCommandResponse.ReceiptResponse.ftSignatures = requestCommandResponse.ReceiptResponse.ftSignatures.Concat(_signatureFactory.CreateSandboxSignature(queueAT.ftQueueATId));
            }
            if (requestCommandResponse.JournalAT != null)
            {
                await _journalATRepository.InsertAsync(requestCommandResponse.JournalAT);
            }
            await _configurationRepository.InsertOrUpdateQueueATAsync(queueAT).ConfigureAwait(false);

            return (requestCommandResponse.ReceiptResponse, requestCommandResponse.ActionJournals);
        }

        private async Task<RequestCommandResponse> PerformReceiptRequest(ReceiptRequest request, ftQueueItem queueItem, ftQueue queue, ftQueueAT queueAT)
        {
            RequestCommand command;
            try
            {
                command = _requestCommandFactory.Create(queue, queueAT, request);
            }
            catch
            {
                throw new ArgumentException($"ReceiptCase {request.ftReceiptCase:X} unknown.");
            }

            return await command.ExecuteAsync(queue, queueAT, request, queueItem);
        }
    }
}
