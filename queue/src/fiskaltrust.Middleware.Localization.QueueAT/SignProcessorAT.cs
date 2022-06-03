using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Localization.QueueAT.Models;
using fiskaltrust.Middleware.Localization.QueueAT.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueAT.RequestCommands.Factories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueDE
{
    public class SignProcessorAT : IMarketSpecificSignProcessor
    {
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IRequestCommandFactory _requestCommandFactory;
        

        public SignProcessorAT(IConfigurationRepository configurationRepository, IRequestCommandFactory requestCommandFactory)
        {
            _configurationRepository = configurationRepository;
            _requestCommandFactory = requestCommandFactory;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            var queueAT = await _configurationRepository.GetQueueATAsync(queueItem.ftQueueId).ConfigureAwait(false);

            //if (!queueAT .ftSignaturCreationUnitDEId.HasValue && !queue.IsActive())
            //{
            //    throw new NullReferenceException(nameof(queueDE.ftSignaturCreationUnitDEId));
            //}

            var requestCommandResponse = await PerformReceiptRequest(request, queueItem, queue, queueAT).ConfigureAwait(false);

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
