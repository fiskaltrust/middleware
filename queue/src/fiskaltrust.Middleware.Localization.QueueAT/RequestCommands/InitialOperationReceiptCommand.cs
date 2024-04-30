using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Extensions;
using fiskaltrust.Middleware.Localization.QueueAT.Helpers;
using fiskaltrust.Middleware.Localization.QueueAT.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Services;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueAT.RequestCommands
{
    public class InitialOperationReceiptCommand : RequestCommand
    {
        public override string ReceiptName => "Initial-operation receipt";

        public InitialOperationReceiptCommand(IATSSCDProvider sscdProvider, MiddlewareConfiguration middlewareConfiguration, QueueATConfiguration queueATConfiguration, ILogger<RequestCommand> logger)
            : base(sscdProvider, middlewareConfiguration, queueATConfiguration, logger) { }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueAT queueAT, ReceiptRequest request, ftQueueItem queueItem, ReceiptResponse response)
        {
            ThrowIfTraining(request);

            if ((request.cbChargeItems != null && request.cbChargeItems?.Count() != 0) || (request.cbPayItems != null && request.cbPayItems?.Count() != 0))
            {
                var notZeroReceiptActionJournal = CreateActionJournal(queue, queueItem, $"Tried to activate {queue.ftQueueId}, but the incoming receipt is not a zero receipt.");
                _logger.LogInformation(notZeroReceiptActionJournal.Message);

                return new RequestCommandResponse
                {
                    ReceiptResponse = response,
                    ActionJournals = new() { notZeroReceiptActionJournal }
                };
            }

            if (queue.StartMoment.HasValue)
            {
                var alreadyActiveActionJournal = CreateActionJournal(queue, queueItem, $"Queue {queue.ftQueueId} is already activated.");
                _logger.LogInformation(alreadyActiveActionJournal.Message);
                var actionJournal = new List<ftActionJournal> { alreadyActiveActionJournal };

                var (receiptIdentification, ftStateData, isBackupScuUsed, signatureItems, journalAt) = await SignReceiptAsync(queueAT, request, response.ftReceiptIdentification, response.ftReceiptMoment, queueItem.ftQueueItemId, isZeroReceipt: true);
                response.ftSignatures = response.ftSignatures.Concat(signatureItems).ToArray();
                response.ftReceiptIdentification = receiptIdentification;
                response.ftStateData = ftStateData;
                if (isBackupScuUsed)
                {
                    response.ftState |= 0x80;
                }
                response.ftSignatures = signatureItems.ToArray();

                return new RequestCommandResponse
                {
                    ReceiptResponse = response,
                    ActionJournals = actionJournal,
                    JournalAT = journalAt
                };
            }

            var aj = CreateActionJournal(queue, queueItem, $"QueueItem {queueItem.ftQueueItemId} used to activate Queue {queueAT.ftQueueATId}.");

            var (actionJournals, journalAT) = await SignReceiptAndEnableQueueAsync(queue, queueAT, queueItem, request, response);
            actionJournals.Add(aj);

            var notificationSignatures = CreateNotificationSignatures(actionJournals);
            response.ftSignatures = response.ftSignatures.Concat(notificationSignatures).ToArray();

            return new RequestCommandResponse
            {
                ReceiptResponse = response,
                ActionJournals = actionJournals,
                JournalAT = journalAT
            };
        }

        internal async Task<(List<ftActionJournal> actionJournals, ftJournalAT journalAT)> SignReceiptAndEnableQueueAsync(ftQueue queue, ftQueueAT queueAT, ftQueueItem queueItem, ReceiptRequest request, ReceiptResponse response)
        {
            var actionJournals = new List<ftActionJournal>();

            if (string.IsNullOrEmpty(queueAT.EncryptionKeyBase64))
            {
                using (var sha256 = SHA256.Create())
                {
                    var rawKey = Encoding.UTF8.GetBytes($"{queueAT.CashBoxIdentification} {DateTime.UtcNow:G} {Guid.NewGuid()}");
                    queueAT.EncryptionKeyBase64 = Convert.ToBase64String(sha256.ComputeHash(rawKey));
                }

                var ajEncKey = CreateActionJournal(queue, queueItem, $"Initial operation receipt: Generated a new encryption key for {queueAT.CashBoxIdentification}.");
                _logger.LogInformation(ajEncKey.Message);
                actionJournals.Add(ajEncKey);
            }
            else
            {
                try
                {
                    ThrowIfEncryptionKeyIsInvalid(queueAT);

                    var ajEncKey = CreateActionJournal(queue, queueItem, $"Initial operation receipt: Read encryption key for {queueAT.CashBoxIdentification} from configuration.");
                    _logger.LogInformation(ajEncKey.Message);
                    actionJournals.Add(ajEncKey);
                }
                catch
                {
                    var ajEncKey = CreateActionJournal(queue, queueItem, $"Initial operation receipt failed: The specified encryption key for {queueAT.CashBoxIdentification} cannot be used.");
                    _logger.LogInformation(ajEncKey.Message);
                    actionJournals.Add(ajEncKey);

                    throw;
                }
            }

            queueAT.LastSignatureHash = CreateLastReceiptSignature(queueAT.CashBoxIdentification);
            queueAT.LastSettlementMoment = new DateTime(request.cbReceiptMoment.Year, request.cbReceiptMoment.Month, 1);
            queueAT.LastSettlementMonth = request.cbReceiptMoment.Month;

            var aj = CreateActionJournal(queue, queueItem, $"Initial operation receipt: Set LastSignature to \"{queueAT.LastSignatureHash}\" and LastSettlementMoment to \"{queueAT.LastSettlementMoment}\".",
                JsonConvert.SerializeObject(new { queueAT.LastSignatureHash, queueAT.LastSettlementMoment }));
            _logger.LogInformation(aj.Message);
            actionJournals.Add(aj);


            var (receiptIdentification, ftStateData, isBackupScuUsed, signatureItems, journalAT) = await SignReceiptAsync(queueAT, request, response.ftReceiptIdentification, response.ftReceiptMoment, queueItem.ftQueueItemId, true);
            response.ftSignatures = response.ftSignatures.Concat(signatureItems).ToArray();
            response.ftReceiptIdentification = receiptIdentification;
            response.ftStateData = ftStateData;
            if (isBackupScuUsed)
            {
                response.ftState |= 0x80;
            }

            if (journalAT != null)
            {
                // Receipt successfully signed, activate Queue
                queue.StartMoment = DateTime.UtcNow;
                response.ftSignatures = response.ftSignatures.Extend(new SignaturItem
                {
                    Caption = "Startbeleg",
                    Data = "Startbeleg",
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = (long) SignaturItem.Types.AT_StorageObligation
                });

                var fonActivateQueueActionJournal = ATFONRegistrationHelper.CreateQueueActivationJournal(queue, queueAT, queueItem, journalAT);
                _logger.LogInformation(fonActivateQueueActionJournal.Message);
                actionJournals.Add(fonActivateQueueActionJournal);

                IncrementMessageCount(ref queueAT);

                var fonVerifyQueueActionJournal = ATFONRegistrationHelper.CreateQueueVerificationJournal(queue, queueAT, queueItem, journalAT);
                _logger.LogInformation(fonVerifyQueueActionJournal.Message);
                actionJournals.Add(fonVerifyQueueActionJournal);

                IncrementMessageCount(ref queueAT);

                return (actionJournals, journalAT);
            }
            else
            {
                var signingFailedActionJournal = CreateActionJournal(queue, queueItem, $"Initial operation receipt failed: Could not sign start receipt.");
                _logger.LogInformation(signingFailedActionJournal.Message);
                actionJournals.Add(signingFailedActionJournal);

                return (actionJournals, null);
            }
        }

        private void ThrowIfEncryptionKeyIsInvalid(ftQueueAT queueAT)
        {
            var receiptId = Guid.NewGuid().ToString();
            var receiptTotalizer = 7.56m;
            var check = TotalizerEncryptionHelper.EncryptTotalizer(queueAT.CashBoxIdentification, receiptId, queueAT.EncryptionKeyBase64, receiptTotalizer);

            if (receiptTotalizer != TotalizerEncryptionHelper.DecryptTotalizer(queueAT.CashBoxIdentification, receiptId, queueAT.EncryptionKeyBase64, check))
            {
                throw new ArgumentException("The specified encryption key cannot be used.");
            }
        }
    }
}
