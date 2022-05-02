using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueME.Services;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueME
{
    public class SignProcessorME : IMarketSpecificSignProcessor
    {
        private readonly IRequestCommandFactory _requestCommandFactory;
        private readonly IMESSCD _client;

        public SignProcessorME(
            IRequestCommandFactory requestCommandFactory,
            IMESSCDProvider mESSCDProvider)
        {
            _requestCommandFactory = requestCommandFactory;
            _client = mESSCDProvider.Instance;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            var response = await _requestCommandFactory.Create(request).ExecuteAsync(_client, queue, request, queueItem);

            return (response.ReceiptResponse, response.ActionJournals);
        }
    }
}
