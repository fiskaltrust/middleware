using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using fiskaltrust.Middleware.Localization.QueueDE.Models;
using fiskaltrust.Middleware.Localization.QueueDE.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueDE.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueDE
{
    public class SignProcessorDE : IMarketSpecificSignProcessor
    {
        private readonly IConfigurationRepository _configurationRepository;
        private readonly ITransactionPayloadFactory _transactionPayloadFactory;
        private readonly IRequestCommandFactory _requestCommandFactory;
        private readonly ILogger<SignProcessorDE> _logger;

        public SignProcessorDE(
            IConfigurationRepository configurationRepository,
            ITransactionPayloadFactory transactionPayloadFactory,
            IRequestCommandFactory requestCommandFactory,
            ILogger<SignProcessorDE> logger)
        {
            _configurationRepository = configurationRepository;
            _transactionPayloadFactory = transactionPayloadFactory;
            _requestCommandFactory = requestCommandFactory;
            _logger = logger;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            _logger.LogTrace("SignProcessorDE.ProcessAsync called.");
            if (request.IsFailTransactionReceipt() && string.IsNullOrEmpty(request.cbReceiptReference) && !request.ftReceiptCaseData.Contains("CurrentStartedTransactionNumbers"))
            {
                throw new ArgumentException($"CbReceiptReference must be set for one transaction! If you want to close multiple transactions, pass an array value for 'CurrentStartedTransactionNumbers' via ftReceiptCaseData.");
            }

            _logger.LogTrace("SignProcessorDE.ProcessAsync: Getting QueueDE from database.");
            var queueDE = await _configurationRepository.GetQueueDEAsync(queueItem.ftQueueId).ConfigureAwait(false);

            if (!queueDE.ftSignaturCreationUnitDEId.HasValue && !queue.IsActive())
            {
                throw new NullReferenceException(nameof(queueDE.ftSignaturCreationUnitDEId));
            }

            var requestCommandResponse = await PerformReceiptRequest(request, queueItem, queue, queueDE).ConfigureAwait(false);

            await _configurationRepository.InsertOrUpdateQueueDEAsync(queueDE).ConfigureAwait(false);

            return (requestCommandResponse.ReceiptResponse, requestCommandResponse.ActionJournals);
        }

        private async Task<RequestCommandResponse> PerformReceiptRequest(ReceiptRequest request, ftQueueItem queueItem, ftQueue queue, ftQueueDE queueDE)
        {
            _logger.LogTrace("SignProcessorDE.PerformReceiptRequest called.");
            var (processType, payload) = _transactionPayloadFactory.CreateReceiptPayload(request);

            RequestCommand command;
            try
            {
                command = _requestCommandFactory.Create(queue, queueDE, request);
            }
            catch (NotImplementedException ex)
            {
                throw new ArgumentException($"ReceiptCase {request.ftReceiptCase:X} unknown. ProcessType {processType}, ProcessData {payload}", ex);
            }

            _logger.LogTrace("SignProcessorDE.PerformReceiptRequest: Executing command {CommandName}.", command.ReceiptName);
            return await command.ExecuteAsync(queue, queueDE, request, queueItem);
        }
    }
}
