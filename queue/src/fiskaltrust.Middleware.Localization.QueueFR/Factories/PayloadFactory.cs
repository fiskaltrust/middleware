using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueFR.Factories
{
    public static class PayloadFactory
    {
        public static string GetTicketPayload(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, ftSignaturCreationUnitFR signaturCreationUnitFR, Totals totals, string lastHash)
        {
            var payload = new TicketPayload()
            {
                QueueId = Guid.Parse(receiptResponse.ftQueueID),
                CashBoxIdentification = receiptResponse.ftCashBoxIdentification,
                Siret = signaturCreationUnitFR.Siret,
                ReceiptId = receiptResponse.ftReceiptIdentification,
                ReceiptMoment = receiptResponse.ftReceiptMoment,
                ReceiptCase = receiptRequest.ftReceiptCase,
                QueueItemId = Guid.Parse(receiptResponse.ftQueueItemID),
                Totalizer = totals.Totalizer,
                CINormal = totals.CITotalNormal,
                CIReduced1 = totals.CITotalReduced1,
                CIReduced2 = totals.CITotalReduced2,
                CIReducedS = totals.CITotalReducedS,
                CIZero = totals.CITotalZero,
                CIUnknown = totals.CITotalUnknown,
                PICash = totals.PITotalCash,
                PINonCash = totals.PITotalNonCash,
                PIInternal = totals.PITotalInternal,
                PIUnknown = totals.PITotalUnknown,
                LastHash = lastHash,
                CertificateSerialNumber = signaturCreationUnitFR.CertificateSerialNumber
            };

            return JsonConvert.SerializeObject(payload);
        }

        public static string GetGrandTotalPayload(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, string lastHash)
        {
            var payload = new GrandTotalPayload()
            {
                QueueId = Guid.Parse(receiptResponse.ftQueueID),
                CashBoxIdentification = receiptResponse.ftCashBoxIdentification,
                Siret = signaturCreationUnitFR.Siret,
                ReceiptId = receiptResponse.ftReceiptIdentification,
                ReceiptMoment = receiptResponse.ftReceiptMoment,
                ReceiptCase = receiptRequest.ftReceiptCase,
                QueueItemId = Guid.Parse(receiptResponse.ftQueueItemID),
                DTotalizer = queueFr.GDayTotalizer,
                DCINormal = queueFr.GDayCITotalNormal,
                DCIReduced1 = queueFr.GDayCITotalReduced1,
                DCIReduced2 = queueFr.GDayCITotalReduced2,
                DCIReducedS = queueFr.GDayCITotalReducedS,
                DCIZero = queueFr.GDayCITotalZero,
                DCIUnknown = queueFr.GDayCITotalUnknown,
                DPICash = queueFr.GDayPITotalCash,
                DPINonCash = queueFr.GDayPITotalNonCash,
                DPIInternal = queueFr.GDayPITotalInternal,
                DPIUnknown = queueFr.GDayPITotalUnknown,
                MTotalizer = queueFr.GMonthTotalizer,
                MCINormal = queueFr.GMonthCITotalNormal,
                MCIReduced1 = queueFr.GMonthCITotalReduced1,
                MCIReduced2 = queueFr.GMonthCITotalReduced2,
                MCIReducedS = queueFr.GMonthCITotalReducedS,
                MCIZero = queueFr.GMonthCITotalZero,
                MCIUnknown = queueFr.GMonthCITotalUnknown,
                MPICash = queueFr.GMonthPITotalCash,
                MPINonCash = queueFr.GMonthPITotalNonCash,
                MPIInternal = queueFr.GMonthPITotalInternal,
                MPIUnknown = queueFr.GMonthPITotalUnknown,
                YTotalizer = queueFr.GYearTotalizer,
                YCINormal = queueFr.GYearCITotalNormal,
                YCIReduced1 = queueFr.GYearCITotalReduced1,
                YCIReduced2 = queueFr.GYearCITotalReduced2,
                YCIReducedS = queueFr.GYearCITotalReducedS,
                YCIZero = queueFr.GYearCITotalZero,
                YCIUnknown = queueFr.GYearCITotalUnknown,
                YPICash = queueFr.GYearPITotalCash,
                YPINonCash = queueFr.GYearPITotalNonCash,
                YPIInternal = queueFr.GYearPITotalInternal,
                YPIUnknown = queueFr.GYearPITotalUnknown,
                STotalizer = queueFr.GShiftTotalizer,
                SCINormal = queueFr.GShiftCITotalNormal,
                SCIReduced1 = queueFr.GShiftCITotalReduced1,
                SCIReduced2 = queueFr.GShiftCITotalReduced2,
                SCIReducedS = queueFr.GShiftCITotalReducedS,
                SCIZero = queueFr.GShiftCITotalZero,
                SCIUnknown = queueFr.GShiftCITotalUnknown,
                SPICash = queueFr.GShiftPITotalCash,
                SPINonCash = queueFr.GShiftPITotalNonCash,
                SPIInternal = queueFr.GShiftPITotalInternal,
                SPIUnknown = queueFr.GShiftPITotalUnknown,
                LastHash = lastHash,
                CertificateSerialNumber = signaturCreationUnitFR.CertificateSerialNumber
            };

            return JsonConvert.SerializeObject(payload);
        }

        public static CopyPayload GetCopyPayload(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, ftSignaturCreationUnitFR signaturCreationUnitFR, string lastHash)
        {
            return new CopyPayload()
            {
                QueueId = Guid.Parse(receiptResponse.ftQueueID),
                CashBoxIdentification = receiptResponse.ftCashBoxIdentification,
                Siret = signaturCreationUnitFR.Siret,
                ReceiptId = receiptResponse.ftReceiptIdentification,
                ReceiptMoment = receiptResponse.ftReceiptMoment,
                ReceiptCase = receiptRequest.ftReceiptCase,
                QueueItemId = Guid.Parse(receiptResponse.ftQueueItemID),
                CopiedReceiptReference = receiptRequest.cbPreviousReceiptReference,
                LastHash = lastHash,
                CertificateSerialNumber = signaturCreationUnitFR.CertificateSerialNumber
            };
        }

        public static string GetArchivePayload(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR,
            string lastHash, Guid? lastActionJournalId, Guid? lastJournalFRId, Guid? lastReceiptJournalId, DateTime? firstContainedReceiptMoment,
            Guid? firstContainedReceiptQueueItemId, DateTime? lastContainedReceiptMoment, Guid? lastContainedReceiptQueueItemId)
        {
            var payload = new ArchivePayload
            {
                QueueId = Guid.Parse(receiptResponse.ftQueueID),
                CashBoxIdentification = receiptResponse.ftCashBoxIdentification,
                Siret = signaturCreationUnitFR.Siret,
                ReceiptId = receiptResponse.ftReceiptIdentification,
                ReceiptMoment = receiptResponse.ftReceiptMoment,
                ReceiptCase = receiptRequest.ftReceiptCase,
                QueueItemId = Guid.Parse(receiptResponse.ftQueueItemID),
                DTotalizer = queueFr.GDayTotalizer,
                DCINormal = queueFr.GDayCITotalNormal,
                DCIReduced1 = queueFr.GDayCITotalReduced1,
                DCIReduced2 = queueFr.GDayCITotalReduced2,
                DCIReducedS = queueFr.GDayCITotalReducedS,
                DCIZero = queueFr.GDayCITotalZero,
                DCIUnknown = queueFr.GDayCITotalUnknown,
                DPICash = queueFr.GDayPITotalCash,
                DPINonCash = queueFr.GDayPITotalNonCash,
                DPIInternal = queueFr.GDayPITotalInternal,
                DPIUnknown = queueFr.GDayPITotalUnknown,
                MTotalizer = queueFr.GMonthTotalizer,
                MCINormal = queueFr.GMonthCITotalNormal,
                MCIReduced1 = queueFr.GMonthCITotalReduced1,
                MCIReduced2 = queueFr.GMonthCITotalReduced2,
                MCIReducedS = queueFr.GMonthCITotalReducedS,
                MCIZero = queueFr.GMonthCITotalZero,
                MCIUnknown = queueFr.GMonthCITotalUnknown,
                MPICash = queueFr.GMonthPITotalCash,
                MPINonCash = queueFr.GMonthPITotalNonCash,
                MPIInternal = queueFr.GMonthPITotalInternal,
                MPIUnknown = queueFr.GMonthPITotalUnknown,
                YTotalizer = queueFr.GYearTotalizer,
                YCINormal = queueFr.GYearCITotalNormal,
                YCIReduced1 = queueFr.GYearCITotalReduced1,
                YCIReduced2 = queueFr.GYearCITotalReduced2,
                YCIReducedS = queueFr.GYearCITotalReducedS,
                YCIZero = queueFr.GYearCITotalZero,
                YCIUnknown = queueFr.GYearCITotalUnknown,
                YPICash = queueFr.GYearPITotalCash,
                YPINonCash = queueFr.GYearPITotalNonCash,
                YPIInternal = queueFr.GYearPITotalInternal,
                YPIUnknown = queueFr.GYearPITotalUnknown,
                ATotalizer = queueFr.ATotalizer,
                ACINormal = queueFr.ACITotalNormal,
                ACIReduced1 = queueFr.ACITotalReduced1,
                ACIReduced2 = queueFr.ACITotalReduced2,
                ACIReducedS = queueFr.ACITotalReducedS,
                ACIZero = queueFr.ACITotalZero,
                ACIUnknown = queueFr.ACITotalUnknown,
                APICash = queueFr.APITotalCash,
                APINonCash = queueFr.APITotalNonCash,
                APIInternal = queueFr.APITotalInternal,
                APIUnknown = queueFr.APITotalUnknown,
                LastActionJournalId = lastActionJournalId ?? Guid.Empty,
                LastJournalFRId = lastJournalFRId ?? Guid.Empty,
                LastReceiptJournalId = lastReceiptJournalId ?? Guid.Empty,
                PreviousArchiveQueueItemId = queueFr.ALastQueueItemId,
                FirstContainedReceiptMoment = firstContainedReceiptMoment,
                FirstContainedReceiptQueueItemId = firstContainedReceiptQueueItemId,
                LastContainedReceiptMoment = lastContainedReceiptMoment,
                LastContainedReceiptQueueItemId = lastContainedReceiptQueueItemId,
                LastHash = lastHash,
                CertificateSerialNumber = signaturCreationUnitFR.CertificateSerialNumber
            };

            return JsonConvert.SerializeObject(payload);
        }
    }
}
