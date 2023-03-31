using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public abstract class ClosingReceiptCommand : RequestCommand
    {
        private readonly IReadOnlyConfigurationRepository _configurationRepository;

        protected abstract string ClosingReceiptName { get; }

        public ClosingReceiptCommand(IReadOnlyConfigurationRepository configurationRepository)
        {
            _configurationRepository = configurationRepository;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            var queueIt = await _configurationRepository.GetQueueITAsync(queue.ftQueueId);

            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIt.CashBoxIdentification, CountryBaseState);
            var actionJournalEntry = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}", queueItem.ftQueueItemId, $"{ClosingReceiptName} receipt was processed.",
                JsonConvert.SerializeObject(new { ftReceiptNumerator = queue.ftReceiptNumerator + 1 }));
            var requestCommandResponse = new RequestCommandResponse
            {
                ReceiptResponse = receiptResponse,
                ActionJournals = new List<ftActionJournal> { actionJournalEntry }
            };
            return await SpecializeAsync(requestCommandResponse);
        }

        // This is overkill for now ... just to demonstrate how we could maybe add some country specific funtionality
        protected virtual Task<RequestCommandResponse> SpecializeAsync(RequestCommandResponse requestCommandResponse) => Task.FromResult(requestCommandResponse);
    }
}
