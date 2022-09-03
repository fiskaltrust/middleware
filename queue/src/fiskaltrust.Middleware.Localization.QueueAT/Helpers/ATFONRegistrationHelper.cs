using System;
using fiskaltrust.storage.serialization.AT.V0;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueAT.Helpers
{
    public static class ATFONRegistrationHelper
    {
        private static readonly string _fonSignatureType = $"0x{ifPOS.v0.SignaturItem.Types.AT_FinanzOnline:x}";

        public static ftActionJournal CreateQueueActivationJournal(ftQueue queue, ftQueueAT queueAT, ftQueueItem queueItem, ftJournalAT journalAT)
        {
            var fonActivateQueue = new FonActivateQueue()
            {
                CashBoxId = queue.ftCashBoxId,
                QueueId = queue.ftQueueId,
                Moment = DateTime.UtcNow,
                CashBoxIdentification = queueAT.CashBoxIdentification,
                CashBoxKeyBase64 = queueAT.EncryptionKeyBase64,
                DEPValue = $"{journalAT?.JWSHeaderBase64url}.{journalAT?.JWSPayloadBase64url}.{journalAT?.JWSSignatureBase64url}",
                ClosedSystemKind = queueAT.ClosedSystemKind,
                ClosedSystemValue = queueAT.ClosedSystemValue,
                IsStartReceipt = true
            };

            return new ftActionJournal
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = (queueItem?.ftQueueItemId).GetValueOrDefault(),
                Moment = DateTime.UtcNow,
                TimeStamp = 0,
                Priority = -1,
                Type = $"{_fonSignatureType}-{nameof(FonActivateQueue)}",
                Message = $"Aktivierung (Inbetriebnahme) einer Sicherheitseinrichtung {queue.ftQueueId} nach RKSV. (Queue)",
                DataJson = JsonConvert.SerializeObject(fonActivateQueue),
                DataBase64 = "jws"
            };
        }
        public static ftActionJournal CreateQueueDeactivationJournal(ftQueue queue, ftQueueAT queueAT, ftQueueItem queueItem, ftJournalAT journalAT, bool isStopReceipt = true)
        {
            var fonDeactivateQueue = new FonDeactivateQueue()
            {
                CashBoxId = queue.ftCashBoxId,
                QueueId = queue.ftQueueId,
                Moment = DateTime.UtcNow,
                CashBoxIdentification = queueAT.CashBoxIdentification,
                DEPValue = $"{journalAT?.JWSHeaderBase64url}.{journalAT?.JWSPayloadBase64url}.{journalAT?.JWSSignatureBase64url}",
                ClosedSystemKind = queueAT.ClosedSystemKind,
                ClosedSystemValue = queueAT.ClosedSystemValue,
                IsStopReceipt = isStopReceipt
            };

            return new ftActionJournal
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = (queueItem?.ftQueueItemId).GetValueOrDefault(),
                Moment = DateTime.UtcNow,
                TimeStamp = 0,
                Priority = -1,
                Type = $"{_fonSignatureType}-{nameof(FonDeactivateQueue)}",
                Message = $"De-Aktivierung (Ausserbetriebnahme) einer Sicherheitseinrichtung {queue.ftQueueId} nach RKSV. (Queue)",
                DataJson = JsonConvert.SerializeObject(fonDeactivateQueue),
                DataBase64 = "jws"
            };
        }

        public static ftActionJournal CreateQueueVerificationJournal(ftQueue queue, ftQueueAT queueAT, ftQueueItem queueItem, ftJournalAT journalAT)
        {
            var fonVerifySignature = new FonVerifySignature()
            {
                CashBoxId = queue.ftCashBoxId,
                QueueId = queue.ftQueueId,
                DEPValue = $"{journalAT?.JWSHeaderBase64url}.{journalAT?.JWSPayloadBase64url}.{journalAT?.JWSSignatureBase64url}",
                ClosedSystemKind = queueAT.ClosedSystemKind,
                ClosedSystemValue = queueAT.ClosedSystemValue,
                CashBoxIdentification = queueAT.CashBoxIdentification,
                CashBoxKeyBase64 = queueAT.EncryptionKeyBase64,
            };

            return new ftActionJournal
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = (queueItem?.ftQueueItemId).GetValueOrDefault(),
                Moment = DateTime.UtcNow,
                TimeStamp = 0,
                Priority = -1,
                Type = $"{_fonSignatureType}-{nameof(FonVerifySignature)}",
                Message = $"Prüfen der Signatur {fonVerifySignature.DEPValue} mittels FON.",
                DataJson = JsonConvert.SerializeObject(fonVerifySignature),
                DataBase64 = "jws"
            };
        }
    }
}
