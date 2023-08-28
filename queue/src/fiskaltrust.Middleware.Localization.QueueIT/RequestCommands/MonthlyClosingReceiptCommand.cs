using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class MonthlyClosingReceiptCommand : RequestCommand
    {
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;

        private readonly long _countryBaseState;

        public MonthlyClosingReceiptCommand(ICountrySpecificSettings countrySpecificSettings)
        {
            _countrySpecificQueueRepository = countrySpecificSettings.CountrySpecificQueueRepository;
            _countryBaseState = countrySpecificSettings.CountryBaseState;
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, bool isBeingResent = false)
        {
            var queueIt = await _countrySpecificQueueRepository.GetQueueAsync(queue.ftQueueId).ConfigureAwait(false);
            var ftReceiptCaseHex = request.ftReceiptCase.ToString("X");
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIt.CashBoxIdentification, _countryBaseState);
            var actionJournalEntry = CreateActionJournal(queue.ftQueueId, ftReceiptCaseHex, queueItem.ftQueueItemId, $"Monthly-Closing receipt was processed.", JsonConvert.SerializeObject(new { ftReceiptNumerator = queue.ftReceiptNumerator + 1 }));
            var requestCommandResponse = new RequestCommandResponse
            {
                ReceiptResponse = receiptResponse,
                ActionJournals = new List<ftActionJournal> { actionJournalEntry }
            };
            return requestCommandResponse;
        }
    }
}
