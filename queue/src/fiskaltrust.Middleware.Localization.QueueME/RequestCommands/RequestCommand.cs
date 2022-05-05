﻿using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using System;
using fiskaltrust.ifPOS.v1.me;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public abstract class RequestCommand
    {
        protected const string RETRYPOLICYEXCEPTION_NAME = "RetryPolicyException";
        protected readonly ILogger<RequestCommand> _logger;
        protected readonly SignatureFactoryME _signatureFactory;
        protected readonly IConfigurationRepository _configurationRepository;
        protected readonly IJournalMERepository _journalMERepository;
        protected readonly IQueueItemRepository _queueItemRepository;

        public RequestCommand(ILogger<RequestCommand> logger, SignatureFactoryME signatureFactory, IConfigurationRepository configurationRepository, IJournalMERepository journalMERepository, IQueueItemRepository queueItemRepository)
        {
            _logger = logger;
            _signatureFactory = signatureFactory;
            _configurationRepository = configurationRepository;
            _journalMERepository = journalMERepository;
            _queueItemRepository = queueItemRepository;
        }

        public abstract Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem);

        protected static ReceiptResponse CreateReceiptResponse(ReceiptRequest request, ftQueueItem queueItem)
        {
            return new ReceiptResponse
            {
                ftCashBoxID = request.ftCashBoxID,
                ftQueueID = queueItem.ftQueueId.ToString(),
                ftQueueItemID = queueItem.ftQueueItemId.ToString(),
                ftQueueRow = queueItem.ftQueueRow,
                cbTerminalID = request.cbTerminalID,
                cbReceiptReference = request.cbReceiptReference,
                ftReceiptMoment = DateTime.UtcNow,
                ftState = 0x44D5000000000000
            };
        }

    }
}