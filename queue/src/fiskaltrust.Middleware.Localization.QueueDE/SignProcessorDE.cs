using System;
using System.Collections.Generic;
using System.Threading;
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
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueDE
{
    public class SignProcessorDE : IMarketSpecificSignProcessor
    {
        private readonly ILogger<SignProcessorDE> _logger;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly ITransactionPayloadFactory _transactionPayloadFactory;
        private readonly IDESSCDProvider _deSSCDProvider;
        private readonly IRequestCommandFactory _requestCommandFactory;
        private readonly TaskCompletionSource<object> _startUpTasks;
        private readonly CancellationTokenSource _startUpTasksCancellation;
        // TODO: use IHostApplicationLifetime in MW 2.0 instead of TaskCompletionSource and CancellationTokenSource

        public SignProcessorDE(
            ILogger<SignProcessorDE> logger,
            IConfigurationRepository configurationRepository,
            IDESSCDProvider dESSCDProvider,
            ITransactionPayloadFactory transactionPayloadFactory,
            IRequestCommandFactory requestCommandFactory,
            ITarFileCleanupService tarFileCleanupService)
        {
            _logger = logger;
            _configurationRepository = configurationRepository;
            _deSSCDProvider = dESSCDProvider;
            _transactionPayloadFactory = transactionPayloadFactory;
            _requestCommandFactory = requestCommandFactory;

            _startUpTasks = new TaskCompletionSource<object>();
            _startUpTasksCancellation = new CancellationTokenSource();

            var _ = Task.Run(async () =>
            {
                try
                {
                    await tarFileCleanupService.CleanupAllTarFilesAsync(_startUpTasksCancellation.Token);
                }
                catch (Exception e)
                {
                    _startUpTasks.SetException(e);
                    return;
                }
                _startUpTasks.SetResult(null);
            });
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            if(_startUpTasks.Task.Status == TaskStatus.Running)
            {
                _startUpTasksCancellation.Cancel();
                _logger.LogInformation("StartUp tasks are not yet finished. Request will return shortly.");
            }
            await _startUpTasks.Task;

            if (string.IsNullOrEmpty(request.cbReceiptReference) && !request.IsFailTransactionReceipt() && !string.IsNullOrEmpty(request.ftReceiptCaseData) && !request.ftReceiptCaseData.Contains("CurrentStartedTransactionNumbers"))
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
    }
}
