using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueME.Services;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueME
{
    public class SignProcessorME : IMarketSpecificSignProcessor
    {
        private readonly IRequestCommandFactory _requestCommandFactory;
        private readonly IMESSCD _client;
        protected readonly IConfigurationRepository _configurationRepository;

        public SignProcessorME(
            IRequestCommandFactory requestCommandFactory,
            IMESSCDProvider mESSCDProvider,
            IConfigurationRepository configurationRepository)
        {
            _requestCommandFactory = requestCommandFactory;
            _client = mESSCDProvider.Instance;
            _configurationRepository = configurationRepository;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            var queueME = await _configurationRepository.GetQueueMEAsync(queue.ftQueueId).ConfigureAwait(false);
            var requestCommand = _requestCommandFactory.Create(request);

            if (queueME.SSCDFailCount > 0 && requestCommand is not ZeroReceiptCommand)
            {
                var requestCommandResponse = await requestCommand.ProcessFailedReceiptRequest(queue, queueItem, request, queueME).ConfigureAwait(false);
                return (requestCommandResponse.ReceiptResponse, requestCommandResponse.ActionJournals.ToList());
            }
            var response = await requestCommand.ExecuteAsync(_client, queue, request, queueItem, queueME).ConfigureAwait(false);
            return (response.ReceiptResponse, response.ActionJournals.ToList());
        }

        public Task<string> GetFtCashBoxIdentificationAsync(ftQueue queue) => Task.FromResult<string>(null);
        public Task FinalTask(ftQueue queue, ftQueueItem queueItem, IMiddlewareActionJournalRepository actionJournalRepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareReceiptJournalRepository receiptJournalRepositor) { return null; }
    }
}
