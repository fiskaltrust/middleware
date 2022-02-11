using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using fiskaltrust.Middleware.Localization.QueueDE.Models;
using fiskaltrust.Middleware.Localization.QueueDE.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueDE.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueDE
{
    public class SignProcessorDE : IMarketSpecificSignProcessor
    {
        private readonly IConfigurationRepository _configurationRepository;
        private readonly ITransactionPayloadFactory _transactionPayloadFactory;
        private readonly IDESSCDProvider _deSSCDProvider;
        private readonly IRequestCommandFactory _requestCommandFactory;

        public SignProcessorDE(
            IConfigurationRepository configurationRepository,
            IDESSCDProvider dESSCDProvider,
            ITransactionPayloadFactory transactionPayloadFactory,
            IRequestCommandFactory requestCommandFactory)
        {
            _configurationRepository = configurationRepository;
            _deSSCDProvider = dESSCDProvider;
            _transactionPayloadFactory = transactionPayloadFactory;
            _requestCommandFactory = requestCommandFactory;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            if (string.IsNullOrEmpty(request.cbReceiptReference) && !request.IsFailTransactionReceipt() &&  !string.IsNullOrEmpty(request.ftReceiptCaseData) && !request.ftReceiptCaseData.Contains("CurrentStartedTransactionNumbers"))
            {
                throw new ArgumentException($"CbReceiptReference must be set for one transaction! If you want to close multiple transactions, pass an array value for 'CurrentStartedTransactionNumbers' via ftReceiptCaseData.");
            }

            var queueDE = await _configurationRepository.GetQueueDEAsync(queueItem.ftQueueId).ConfigureAwait(false);

            if (!queueDE.ftSignaturCreationUnitDEId.HasValue && !queue.IsActive())
            {
                throw new NullReferenceException(nameof(queueDE.ftSignaturCreationUnitDEId));
            }

            var requestCommandResponse = await PerformReceiptRequest(request, queueItem, queue, queueDE, _deSSCDProvider.Instance).ConfigureAwait(false);

            await _configurationRepository.InsertOrUpdateQueueDEAsync(queueDE).ConfigureAwait(false);

            return (requestCommandResponse.ReceiptResponse, requestCommandResponse.ActionJournals);
        }

        private async Task<RequestCommandResponse> PerformReceiptRequest(ReceiptRequest request, ftQueueItem queueItem, ftQueue queue, ftQueueDE queueDE, IDESSCD client)
        {
            var (processType, payload) = _transactionPayloadFactory.CreateReceiptPayload(request);

            RequestCommand command;
            try
            {
                command = _requestCommandFactory.Create(queue, queueDE, request);
            }
            catch
            {
                throw new ArgumentException($"ReceiptCase {request.ftReceiptCase:X} unknown. ProcessType {processType}, ProcessData {payload}");
            }

            return await command.ExecuteAsync(queue, queueDE, client, request, queueItem);
        }


        public static byte[] Compress(string sourcePath)
        {
            if (sourcePath == null)
            {
                throw new ArgumentNullException(nameof(sourcePath));
            }

            using (var ms = new MemoryStream())
            {
                using (var arch = new ZipArchive(ms, ZipArchiveMode.Create))
                {
                    arch.CreateEntryFromFile(sourcePath, Path.GetFileName(sourcePath), CompressionLevel.Optimal);
                }

                return ms.ToArray();
            }
        }
    }
}
