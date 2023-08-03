using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.Factories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands
{
    public class PosReceiptCommand : RequestCommand
    {
        private readonly long _countryBaseState;
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;
        private readonly SignatureItemFactoryDEFAULT _signatureItemFactory;

        public PosReceiptCommand(ICountrySpecificSettings countrySpecificSettings)
        {
            _countrySpecificQueueRepository = countrySpecificSettings.CountrySpecificQueueRepository;
            _countryBaseState = countrySpecificSettings.CountryBaseState;
            _signatureItemFactory = new SignatureItemFactoryDEFAULT();
        }
        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, bool isBeingResent = false)
        {
            var countrySpecificQueue = await _countrySpecificQueueRepository.GetQueueAsync(queue.ftQueueId);

            var response = CreateReceiptResponse(queue, request, queueItem, countrySpecificQueue.CashBoxIdentification, _countryBaseState);

            var sumOfPayItems = request.cbPayItems?.Sum(item => item.Amount) ?? 0;
            var previousHash = queue.ftReceiptHash;
            var signatures = _signatureItemFactory.GetSignaturesForPosReceiptTransaction(queue.ftQueueId, queueItem.ftQueueItemId, sumOfPayItems, previousHash, request.ftReceiptCase);
            response.ftSignatures = signatures.ToArray();

            return await Task.FromResult(new RequestCommandResponse
            {
                ReceiptResponse = response,
                ActionJournals = new List<ftActionJournal>()
            });
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) 
        {
            return Task.FromResult(false);
        }
    }
}
