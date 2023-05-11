using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Exceptions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class DailyClosingReceiptCommand : Contracts.RequestCommands.DailyClosingReceiptCommand
    {
        private readonly SignatureItemFactoryIT _signatureItemFactoryIT;
        protected override ICountrySpecificQueueRepository CountrySpecificQueueRepository => _countrySpecificQueueRepository;
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;
        private readonly IMiddlewareJournalITRepository _journalITRepository;
        private readonly IITSSCD _client;

        public override long CountryBaseState => Constants.Cases.BASE_STATE;

        public DailyClosingReceiptCommand(SignatureItemFactoryIT signatureItemFactoryIT, ICountrySpecificQueueRepository countrySpecificQueueRepository, IITSSCDProvider itIsscdProvider, IMiddlewareJournalITRepository journalITRepository)
        {
            _countrySpecificQueueRepository = countrySpecificQueueRepository;
            _client = itIsscdProvider.Instance;
            _journalITRepository = journalITRepository;
            _signatureItemFactoryIT = signatureItemFactoryIT;
        }

        protected override async Task<string> GetCashboxIdentificationAsync(Guid ftQueueId)
        {
            var queueIt = await _countrySpecificQueueRepository.GetQueueAsync(ftQueueId).ConfigureAwait(false);
            return queueIt.CashBoxIdentification;
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);

        protected override async Task<RequestCommandResponse> SpecializeAsync(RequestCommandResponse requestCommandResponse, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            var response = await _client.ExecuteDailyClosingAsync(new DailyClosingRequest() ).ConfigureAwait(false);

            if (!response.Success)
            {
                if (Errors.IsConnectionError(response.ErrorInfo))
                {
                    return await ProcessFailedReceiptRequest(queue, queueItem, request).ConfigureAwait(false);
                }
                else
                {
                    throw new Exception(response.ErrorInfo);
                }
            }
            else
            {
                requestCommandResponse.ReceiptResponse.ftReceiptIdentification += $"Z{response.ZRepNumber}";
                requestCommandResponse.ReceiptResponse.ftSignatures = _signatureItemFactoryIT.CreatePosReceiptSignatures(response);
                var queueIt = await _countrySpecificQueueRepository.GetQueueAsync(queue.ftQueueId).ConfigureAwait(false);
                var journalIT = new ftJournalIT().FromResponse(queueIt, queueItem, new ScuResponse()
                {
                    DataJson = response.ReportDataJson,
                    ftReceiptCase = request.ftReceiptCase,
                    ZRepNumber = response.ZRepNumber
                });
                await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);
                return requestCommandResponse;
            }
        }
    }
}
