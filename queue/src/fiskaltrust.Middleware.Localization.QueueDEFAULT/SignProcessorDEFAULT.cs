using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT
{
    public class SignProcessorDEFAULT : IMarketSpecificSignProcessor
    {
        private readonly ICountrySpecificSettings _countrySpecificSettings;
        private readonly IRequestCommandFactory _requestCommandFactory;
        private readonly ILogger<SignProcessorDEFAULT> _logger;
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;

        public SignProcessorDEFAULT(ICountrySpecificQueueRepository countrySpecificQueueRepository, ICountrySpecificSettings countrySpecificSettings, IRequestCommandFactory requestCommandFactory, ILogger<SignProcessorDEFAULT> logger)
        {
            _requestCommandFactory = requestCommandFactory;
            _countrySpecificSettings = countrySpecificSettings;
            _logger = logger;
            _countrySpecificQueueRepository = countrySpecificQueueRepository;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            var requestCommand = _requestCommandFactory.Create(request);
            var response = await requestCommand.ExecuteAsync(queue, request, queueItem).ConfigureAwait(false);
            return (response.ReceiptResponse, response.ActionJournals.ToList());
        }

        public async Task<string> GetFtCashBoxIdentificationAsync(ftQueue queue) => (await _countrySpecificQueueRepository.GetQueueAsync(queue.ftQueueId)).CashBoxIdentification;
        public Task FinalTaskAsync(ftQueue queue, ftQueueItem queueItem, ReceiptRequest request, IMiddlewareActionJournalRepository actionJournalRepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareReceiptJournalRepository receiptJournalRepositor) { return Task.CompletedTask; }
        public Task FirstTaskAsync() { return Task.CompletedTask; }
    }
}
