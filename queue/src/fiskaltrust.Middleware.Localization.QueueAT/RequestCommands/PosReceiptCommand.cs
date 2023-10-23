﻿using System;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Services;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueAT.RequestCommands
{
    internal class PosReceiptCommand : RequestCommand
    {
        public override string ReceiptName => "POS receipt";

        public PosReceiptCommand(IATSSCDProvider sscdProvider, MiddlewareConfiguration middlewareConfiguration, QueueATConfiguration queueATConfiguration, ILogger<RequestCommand> logger)
            : base(sscdProvider, middlewareConfiguration, queueATConfiguration, logger) { }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueAT queueAT, ReceiptRequest request, ftQueueItem queueItem)
        {
            var response = CreateReceiptResponse(request, queueItem, queueAT, queue);

            var (receiptIdentification, ftStateData, _, signatureItems, journalAT) = await SignReceiptAsync(queueAT, request, response.ftReceiptIdentification, response.ftReceiptMoment, queueItem.ftQueueItemId);
            response.ftSignatures = response.ftSignatures.Concat(signatureItems).ToArray();
            response.ftReceiptIdentification = receiptIdentification;
            response.ftStateData = ftStateData;

            return new RequestCommandResponse
            {
                ReceiptResponse = response,
                ActionJournals = new(),
                JournalAT = journalAT
            };
        }
    }
}
