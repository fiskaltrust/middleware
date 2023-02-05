using System;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.Extensions
{
    public static class QueueFRExtensions
    {
        public static void AddReceiptTotalsToTicketTotals(this ftQueueFR queueFr, Totals totals)
        {
            if (totals.Totalizer.HasValue)
            {
                queueFr.TTotalizer += totals.Totalizer.Value;
            }

            if (totals.CITotalNormal.HasValue)
            {
                queueFr.TCITotalNormal += totals.CITotalNormal.Value;
            }

            if (totals.CITotalReduced1.HasValue)
            {
                queueFr.TCITotalReduced1 += totals.CITotalReduced1.Value;
            }

            if (totals.CITotalReduced2.HasValue)
            {
                queueFr.TCITotalReduced2 += totals.CITotalReduced2.Value;
            }

            if (totals.CITotalReducedS.HasValue)
            {
                queueFr.TCITotalReducedS += totals.CITotalReducedS.Value;
            }

            if (totals.CITotalZero.HasValue)
            {
                queueFr.TCITotalZero += totals.CITotalZero.Value;
            }

            if (totals.CITotalUnknown.HasValue)
            {
                queueFr.TCITotalUnknown += totals.CITotalUnknown.Value;
            }

            if (totals.PITotalCash.HasValue)
            {
                queueFr.TPITotalCash += totals.PITotalCash.Value;
            }

            if (totals.PITotalNonCash.HasValue)
            {
                queueFr.TPITotalNonCash += totals.PITotalNonCash.Value;
            }

            if (totals.PITotalInternal.HasValue)
            {
                queueFr.TPITotalInternal += totals.PITotalInternal.Value;
            }

            if (totals.PITotalUnknown.HasValue)
            {
                queueFr.TPITotalUnknown += totals.PITotalUnknown.Value;
            }
        }

        public static void AddReceiptTotalsToInvoiceTotals(this ftQueueFR queueFr, Totals totals)
        {
            if (totals.Totalizer.HasValue)
            {
                queueFr.ITotalizer += totals.Totalizer.Value;
            }

            if (totals.CITotalNormal.HasValue)
            {
                queueFr.ICITotalNormal += totals.CITotalNormal.Value;
            }

            if (totals.CITotalReduced1.HasValue)
            {
                queueFr.ICITotalReduced1 += totals.CITotalReduced1.Value;
            }

            if (totals.CITotalReduced2.HasValue)
            {
                queueFr.ICITotalReduced2 += totals.CITotalReduced2.Value;
            }

            if (totals.CITotalReducedS.HasValue)
            {
                queueFr.ICITotalReducedS += totals.CITotalReducedS.Value;
            }

            if (totals.CITotalZero.HasValue)
            {
                queueFr.ICITotalZero += totals.CITotalZero.Value;
            }

            if (totals.CITotalUnknown.HasValue)
            {
                queueFr.ICITotalUnknown += totals.CITotalUnknown.Value;
            }

            if (totals.PITotalCash.HasValue)
            {
                queueFr.IPITotalCash += totals.PITotalCash.Value;
            }

            if (totals.PITotalNonCash.HasValue)
            {
                queueFr.IPITotalNonCash += totals.PITotalNonCash.Value;
            }

            if (totals.PITotalInternal.HasValue)
            {
                queueFr.IPITotalInternal += totals.PITotalInternal.Value;
            }

            if (totals.PITotalUnknown.HasValue)
            {
                queueFr.IPITotalUnknown += totals.PITotalUnknown.Value;
            }
        }

        public static void AddReceiptTotalsToBillTotals(this ftQueueFR queueFr, Totals totals)
        {
            if (totals.Totalizer.HasValue)
            {
                queueFr.BTotalizer += totals.Totalizer.Value;
            }

            if (totals.CITotalNormal.HasValue)
            {
                queueFr.BCITotalNormal += totals.CITotalNormal.Value;
            }

            if (totals.CITotalReduced1.HasValue)
            {
                queueFr.BCITotalReduced1 += totals.CITotalReduced1.Value;
            }

            if (totals.CITotalReduced2.HasValue)
            {
                queueFr.BCITotalReduced2 += totals.CITotalReduced2.Value;
            }

            if (totals.CITotalReducedS.HasValue)
            {
                queueFr.BCITotalReducedS += totals.CITotalReducedS.Value;
            }

            if (totals.CITotalZero.HasValue)
            {
                queueFr.BCITotalZero += totals.CITotalZero.Value;
            }

            if (totals.CITotalUnknown.HasValue)
            {
                queueFr.BCITotalUnknown += totals.CITotalUnknown.Value;
            }

            if (totals.PITotalCash.HasValue)
            {
                queueFr.BPITotalCash += totals.PITotalCash.Value;
            }

            if (totals.PITotalNonCash.HasValue)
            {
                queueFr.BPITotalNonCash += totals.PITotalNonCash.Value;
            }

            if (totals.PITotalInternal.HasValue)
            {
                queueFr.BPITotalInternal += totals.PITotalInternal.Value;
            }

            if (totals.PITotalUnknown.HasValue)
            {
                queueFr.BPITotalUnknown += totals.PITotalUnknown.Value;
            }
        }

        public static void AddReceiptTotalsToPaymentProveTotals(this ftQueueFR queueFr, Totals totals)
        {
            if (totals.Totalizer.HasValue)
            {
                queueFr.PTotalizer += totals.Totalizer.Value;
            }

            if (totals.PITotalCash.HasValue)
            {
                queueFr.PPITotalCash += totals.PITotalCash.Value;
            }

            if (totals.PITotalNonCash.HasValue)
            {
                queueFr.PPITotalNonCash += totals.PITotalNonCash.Value;
            }

            if (totals.PITotalInternal.HasValue)
            {
                queueFr.PPITotalInternal += totals.PITotalInternal.Value;
            }

            if (totals.PITotalUnknown.HasValue)
            {
                queueFr.PPITotalUnknown += totals.PITotalUnknown.Value;
            }
        }

        public static void AddReceiptTotalsToGrandTotals(this ftQueueFR queueFr, Totals totals)
        {
            if (totals.Totalizer.HasValue)
            {
                queueFr.GShiftTotalizer += totals.Totalizer.Value;
            }

            if (totals.CITotalNormal.HasValue)
            {
                queueFr.GShiftCITotalNormal += totals.CITotalNormal.Value;
            }

            if (totals.CITotalReduced1.HasValue)
            {
                queueFr.GShiftCITotalReduced1 += totals.CITotalReduced1.Value;
            }

            if (totals.CITotalReduced2.HasValue)
            {
                queueFr.GShiftCITotalReduced2 += totals.CITotalReduced2.Value;
            }

            if (totals.CITotalReducedS.HasValue)
            {
                queueFr.GShiftCITotalReducedS += totals.CITotalReducedS.Value;
            }

            if (totals.CITotalZero.HasValue)
            {
                queueFr.GShiftCITotalZero += totals.CITotalZero.Value;
            }

            if (totals.CITotalUnknown.HasValue)
            {
                queueFr.GShiftCITotalUnknown += totals.CITotalUnknown.Value;
            }

            if (totals.PITotalCash.HasValue)
            {
                queueFr.GShiftPITotalCash += totals.PITotalCash.Value;
            }

            if (totals.PITotalNonCash.HasValue)
            {
                queueFr.GShiftPITotalNonCash += totals.PITotalNonCash.Value;
            }

            if (totals.PITotalInternal.HasValue)
            {
                queueFr.GShiftPITotalInternal += totals.PITotalInternal.Value;
            }

            if (totals.PITotalUnknown.HasValue)
            {
                queueFr.GShiftPITotalUnknown += totals.PITotalUnknown.Value;
            }

            if (totals.Totalizer.HasValue)
            {
                queueFr.GDayTotalizer += totals.Totalizer.Value;
            }

            if (totals.CITotalNormal.HasValue)
            {
                queueFr.GDayCITotalNormal += totals.CITotalNormal.Value;
            }

            if (totals.CITotalReduced1.HasValue)
            {
                queueFr.GDayCITotalReduced1 += totals.CITotalReduced1.Value;
            }

            if (totals.CITotalReduced2.HasValue)
            {
                queueFr.GDayCITotalReduced2 += totals.CITotalReduced2.Value;
            }

            if (totals.CITotalReducedS.HasValue)
            {
                queueFr.GDayCITotalReducedS += totals.CITotalReducedS.Value;
            }

            if (totals.CITotalZero.HasValue)
            {
                queueFr.GDayCITotalZero += totals.CITotalZero.Value;
            }

            if (totals.CITotalUnknown.HasValue)
            {
                queueFr.GDayCITotalUnknown += totals.CITotalUnknown.Value;
            }

            if (totals.PITotalCash.HasValue)
            {
                queueFr.GDayPITotalCash += totals.PITotalCash.Value;
            }

            if (totals.PITotalNonCash.HasValue)
            {
                queueFr.GDayPITotalNonCash += totals.PITotalNonCash.Value;
            }

            if (totals.PITotalInternal.HasValue)
            {
                queueFr.GDayPITotalInternal += totals.PITotalInternal.Value;
            }

            if (totals.PITotalUnknown.HasValue)
            {
                queueFr.GDayPITotalUnknown += totals.PITotalUnknown.Value;
            }
        }

        public static void AddReceiptTotalsToArchiveTotals(this ftQueueFR queueFr, Totals totals)
        {
            if (totals.Totalizer.HasValue)
            {
                queueFr.ATotalizer += totals.Totalizer.Value;
            }

            if (totals.CITotalNormal.HasValue)
            {
                queueFr.ACITotalNormal += totals.CITotalNormal.Value;
            }

            if (totals.CITotalReduced1.HasValue)
            {
                queueFr.ACITotalReduced1 += totals.CITotalReduced1.Value;
            }

            if (totals.CITotalReduced2.HasValue)
            {
                queueFr.ACITotalReduced2 += totals.CITotalReduced2.Value;
            }

            if (totals.CITotalReducedS.HasValue)
            {
                queueFr.ACITotalReducedS += totals.CITotalReducedS.Value;
            }

            if (totals.CITotalZero.HasValue)
            {
                queueFr.ACITotalZero += totals.CITotalZero.Value;
            }

            if (totals.CITotalUnknown.HasValue)
            {
                queueFr.ACITotalUnknown += totals.CITotalUnknown.Value;
            }

            if (totals.PITotalCash.HasValue)
            {
                queueFr.APITotalCash += totals.PITotalCash.Value;
            }

            if (totals.PITotalNonCash.HasValue)
            {
                queueFr.APITotalNonCash += totals.PITotalNonCash.Value;
            }

            if (totals.PITotalInternal.HasValue)
            {
                queueFr.APITotalInternal += totals.PITotalInternal.Value;
            }

            if (totals.PITotalUnknown.HasValue)
            {
                queueFr.APITotalUnknown += totals.PITotalUnknown.Value;
            }
        }

        public static void ResetShiftTotalizer(this ftQueueFR queueFr, ftQueueItem queueItem)
        {
            queueFr.GShiftTotalizer = 0;
            queueFr.GShiftCITotalNormal = 0;
            queueFr.GShiftCITotalReduced1 = 0;
            queueFr.GShiftCITotalReduced2 = 0;
            queueFr.GShiftCITotalReducedS = 0;
            queueFr.GShiftCITotalZero = 0;
            queueFr.GShiftCITotalUnknown = 0;
            queueFr.GShiftPITotalCash = 0;
            queueFr.GShiftPITotalNonCash = 0;
            queueFr.GShiftPITotalInternal = 0;
            queueFr.GShiftPITotalUnknown = 0;
            queueFr.GLastShiftMoment = DateTime.UtcNow;
            queueFr.GLastShiftQueueItemId = queueItem.ftQueueItemId;
        }

        public static void ResetYearTotalizers(this ftQueueFR queueFr, ftQueueItem queueItem)
        {
            queueFr.GYearTotalizer = 0;
            queueFr.GYearCITotalNormal = 0;
            queueFr.GYearCITotalReduced1 = 0;
            queueFr.GYearCITotalReduced2 = 0;
            queueFr.GYearCITotalReducedS = 0;
            queueFr.GYearCITotalZero = 0;
            queueFr.GYearCITotalUnknown = 0;
            queueFr.GYearPITotalCash = 0;
            queueFr.GYearPITotalNonCash = 0;
            queueFr.GYearPITotalInternal = 0;
            queueFr.GYearPITotalUnknown = 0;
            queueFr.GLastYearMoment = DateTime.UtcNow;
            queueFr.GLastYearQueueItemId = queueItem.ftQueueItemId;
        }

        public static void ResetMonthlyTotalizers(this ftQueueFR queueFr, ftQueueItem queueItem)
        {
            queueFr.GYearTotalizer += queueFr.GMonthTotalizer;
            queueFr.GYearCITotalNormal += queueFr.GMonthCITotalNormal;
            queueFr.GYearCITotalReduced1 += queueFr.GMonthCITotalReduced1;
            queueFr.GYearCITotalReduced2 += queueFr.GMonthCITotalReduced2;
            queueFr.GYearCITotalReducedS += queueFr.GMonthCITotalReducedS;
            queueFr.GYearCITotalZero += queueFr.GMonthCITotalZero;
            queueFr.GYearCITotalUnknown += queueFr.GMonthCITotalUnknown;
            queueFr.GYearPITotalCash += queueFr.GMonthPITotalCash;
            queueFr.GYearPITotalNonCash += queueFr.GMonthPITotalNonCash;
            queueFr.GYearPITotalInternal += queueFr.GMonthPITotalInternal;
            queueFr.GYearPITotalUnknown += queueFr.GMonthPITotalUnknown;

            queueFr.GMonthTotalizer = 0;
            queueFr.GMonthCITotalNormal = 0;
            queueFr.GMonthCITotalReduced1 = 0;
            queueFr.GMonthCITotalReduced2 = 0;
            queueFr.GMonthCITotalReducedS = 0;
            queueFr.GMonthCITotalZero = 0;
            queueFr.GMonthCITotalUnknown = 0;
            queueFr.GMonthPITotalCash = 0;
            queueFr.GMonthPITotalNonCash = 0;
            queueFr.GMonthPITotalInternal = 0;
            queueFr.GMonthPITotalUnknown = 0;

            queueFr.GLastMonthMoment = DateTime.UtcNow;
            queueFr.GLastMonthQueueItemId = queueItem.ftQueueItemId;
        }

        public static void ResetDailyTotalizers(this ftQueueFR queueFr, ftQueueItem queueItem)
        {
            queueFr.GMonthTotalizer += queueFr.GDayTotalizer;
            queueFr.GMonthCITotalNormal += queueFr.GDayCITotalNormal;
            queueFr.GMonthCITotalReduced1 += queueFr.GDayCITotalReduced1;
            queueFr.GMonthCITotalReduced2 += queueFr.GDayCITotalReduced2;
            queueFr.GMonthCITotalReducedS += queueFr.GDayCITotalReducedS;
            queueFr.GMonthCITotalZero += queueFr.GDayCITotalZero;
            queueFr.GMonthCITotalUnknown += queueFr.GDayCITotalUnknown;
            queueFr.GMonthPITotalCash += queueFr.GDayPITotalCash;
            queueFr.GMonthPITotalNonCash += queueFr.GDayPITotalNonCash;
            queueFr.GMonthPITotalInternal += queueFr.GDayPITotalInternal;
            queueFr.GMonthPITotalUnknown += queueFr.GDayPITotalUnknown;

            queueFr.GDayTotalizer = 0;
            queueFr.GDayCITotalNormal = 0;
            queueFr.GDayCITotalReduced1 = 0;
            queueFr.GDayCITotalReduced2 = 0;
            queueFr.GDayCITotalReducedS = 0;
            queueFr.GDayCITotalZero = 0;
            queueFr.GDayCITotalUnknown = 0;
            queueFr.GDayPITotalCash = 0;
            queueFr.GDayPITotalNonCash = 0;
            queueFr.GDayPITotalInternal = 0;
            queueFr.GDayPITotalUnknown = 0;

            queueFr.GLastDayMoment = DateTime.UtcNow;
            queueFr.GLastDayQueueItemId = queueItem.ftQueueItemId;
        }

        public static void ResetArchiveTotalizers(this ftQueueFR queueFr, ftQueueItem queueItem)
        {
            queueFr.ATotalizer = 0;
            queueFr.ACITotalNormal = 0;
            queueFr.ACITotalReduced1 = 0;
            queueFr.ACITotalReduced2 = 0;
            queueFr.ACITotalReducedS = 0;
            queueFr.ACITotalZero = 0;
            queueFr.ACITotalUnknown = 0;
            queueFr.APITotalCash = 0;
            queueFr.APITotalNonCash = 0;
            queueFr.APITotalInternal = 0;
            queueFr.APITotalUnknown = 0;

            queueFr.ALastMoment = DateTime.UtcNow;
            queueFr.ALastQueueItemId = queueItem.ftQueueItemId;
        }

        public static Totals GetShiftTotals(this ftQueueFR queueFr)
        {
            return new Totals()
            {
                Totalizer = queueFr.GShiftTotalizer,
                CITotalNormal = queueFr.GShiftCITotalNormal,
                CITotalReduced1 = queueFr.GShiftCITotalReduced1,
                CITotalReduced2 = queueFr.GShiftCITotalReduced2,
                CITotalReducedS = queueFr.GShiftCITotalReducedS,
                CITotalZero = queueFr.GShiftCITotalZero,
                CITotalUnknown = queueFr.GShiftCITotalUnknown,
                PITotalCash = queueFr.GShiftPITotalCash,
                PITotalNonCash = queueFr.GShiftPITotalNonCash,
                PITotalInternal = queueFr.GShiftPITotalInternal,
                PITotalUnknown = queueFr.GShiftPITotalUnknown
            };
        }

        public static Totals GetYearTotals(this ftQueueFR queueFr)
        {
            return new Totals()
            {
                Totalizer = queueFr.GYearTotalizer,
                CITotalNormal = queueFr.GYearCITotalNormal,
                CITotalReduced1 = queueFr.GYearCITotalReduced1,
                CITotalReduced2 = queueFr.GYearCITotalReduced2,
                CITotalReducedS = queueFr.GYearCITotalReducedS,
                CITotalZero = queueFr.GYearCITotalZero,
                CITotalUnknown = queueFr.GYearCITotalUnknown,
                PITotalCash = queueFr.GYearPITotalCash,
                PITotalNonCash = queueFr.GYearPITotalNonCash,
                PITotalInternal = queueFr.GYearPITotalInternal,
                PITotalUnknown = queueFr.GYearPITotalUnknown
            };
        }

        public static Totals GetMonthTotals(this ftQueueFR queueFr)
        {
            return new Totals()
            {
                Totalizer = queueFr.GMonthTotalizer,
                CITotalNormal = queueFr.GMonthCITotalNormal,
                CITotalReduced1 = queueFr.GMonthCITotalReduced1,
                CITotalReduced2 = queueFr.GMonthCITotalReduced2,
                CITotalReducedS = queueFr.GMonthCITotalReducedS,
                CITotalZero = queueFr.GMonthCITotalZero,
                CITotalUnknown = queueFr.GMonthCITotalUnknown,
                PITotalCash = queueFr.GMonthPITotalCash,
                PITotalNonCash = queueFr.GMonthPITotalNonCash,
                PITotalInternal = queueFr.GMonthPITotalInternal,
                PITotalUnknown = queueFr.GMonthPITotalUnknown
            };
        }

        public static Totals GetDayTotals(this ftQueueFR queueFr)
        {
            return new Totals()
            {
                Totalizer = queueFr.GDayTotalizer,
                CITotalNormal = queueFr.GDayCITotalNormal,
                CITotalReduced1 = queueFr.GDayCITotalReduced1,
                CITotalReduced2 = queueFr.GDayCITotalReduced2,
                CITotalReducedS = queueFr.GDayCITotalReducedS,
                CITotalZero = queueFr.GDayCITotalZero,
                CITotalUnknown = queueFr.GDayCITotalUnknown,
                PITotalCash = queueFr.GDayPITotalCash,
                PITotalNonCash = queueFr.GDayPITotalNonCash,
                PITotalInternal = queueFr.GDayPITotalInternal,
                PITotalUnknown = queueFr.GDayPITotalUnknown
            };
        }

        public static Totals GetArchiveTotals(this ftQueueFR queueFr)
        {
            return new Totals()
            {
                Totalizer = queueFr.ATotalizer,
                CITotalNormal = queueFr.ACITotalNormal,
                CITotalReduced1 = queueFr.ACITotalReduced1,
                CITotalReduced2 = queueFr.ACITotalReduced2,
                CITotalReducedS = queueFr.ACITotalReducedS,
                CITotalZero = queueFr.ACITotalZero,
                CITotalUnknown = queueFr.ACITotalUnknown,
                PITotalCash = queueFr.APITotalCash,
                PITotalNonCash = queueFr.APITotalNonCash,
                PITotalInternal = queueFr.APITotalInternal,
                PITotalUnknown = queueFr.APITotalUnknown
            };
        }
    }
}
