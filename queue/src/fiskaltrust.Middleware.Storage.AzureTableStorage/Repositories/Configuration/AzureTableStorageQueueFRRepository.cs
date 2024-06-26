﻿using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageQueueFRRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtQueueFR, ftQueueFR>
    {
        public AzureTableStorageQueueFRRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "QueueFR";

        protected override void EntityUpdated(ftQueueFR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueFR entity) => entity.ftQueueFRId;

        protected override AzureTableStorageFtQueueFR MapToAzureEntity(ftQueueFR src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtQueueFR
            {
                PartitionKey = src.ftQueueFRId.ToString(),
                RowKey = src.ftQueueFRId.ToString(),
                ftQueueFRId = src.ftQueueFRId,
                ftSignaturCreationUnitFRId = src.ftSignaturCreationUnitFRId,
                BCITotalReducedS = Convert.ToDouble(src.BCITotalReducedS),
                BCITotalReduced2 = Convert.ToDouble(src.BCITotalReduced2),
                BCITotalReduced1 = Convert.ToDouble(src.BCITotalReduced1),
                BCITotalNormal = Convert.ToDouble(src.BCITotalNormal),
                BTotalizer = Convert.ToDouble(src.BTotalizer),
                BNumerator = src.BNumerator,
                GLastYearQueueItemId = src.GLastYearQueueItemId,
                GLastYearMoment = src.GLastYearMoment,
                GYearPITotalUnknown = Convert.ToDouble(src.GYearPITotalUnknown),
                GYearPITotalInternal = Convert.ToDouble(src.GYearPITotalInternal),
                GYearPITotalNonCash = Convert.ToDouble(src.GYearPITotalNonCash),
                GYearPITotalCash = Convert.ToDouble(src.GYearPITotalCash),
                GYearCITotalUnknown = Convert.ToDouble(src.GYearCITotalUnknown),
                GYearCITotalZero = Convert.ToDouble(src.GYearCITotalZero),
                GYearCITotalReducedS = Convert.ToDouble(src.GYearCITotalReducedS),
                GYearCITotalReduced2 = Convert.ToDouble(src.GYearCITotalReduced2),
                GYearCITotalReduced1 = Convert.ToDouble(src.GYearCITotalReduced1),
                GYearCITotalNormal = Convert.ToDouble(src.GYearCITotalNormal),
                GYearTotalizer = Convert.ToDouble(src.GYearTotalizer),
                GLastMonthQueueItemId = src.GLastMonthQueueItemId,
                GLastMonthMoment = src.GLastMonthMoment,
                GMonthPITotalUnknown = Convert.ToDouble(src.GMonthPITotalUnknown),
                GMonthPITotalInternal = Convert.ToDouble(src.GMonthPITotalInternal),
                GMonthPITotalNonCash = Convert.ToDouble(src.GMonthPITotalNonCash),
                GMonthPITotalCash = Convert.ToDouble(src.GMonthPITotalCash),
                GMonthCITotalUnknown = Convert.ToDouble(src.GMonthCITotalUnknown),
                GMonthCITotalZero = Convert.ToDouble(src.GMonthCITotalZero),
                GMonthCITotalReducedS = Convert.ToDouble(src.GMonthCITotalReducedS),
                GMonthCITotalReduced2 = Convert.ToDouble(src.GMonthCITotalReduced2),
                BCITotalZero = Convert.ToDouble(src.BCITotalZero),
                BCITotalUnknown = Convert.ToDouble(src.BCITotalUnknown),
                BPITotalCash = Convert.ToDouble(src.BPITotalCash),
                BPITotalNonCash = Convert.ToDouble(src.BPITotalNonCash),
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                UsedFailedMomentMax = src.UsedFailedMomentMax,
                UsedFailedMomentMin = src.UsedFailedMomentMin,
                UsedFailedCount = src.UsedFailedCount,
                CLastHash = src.CLastHash,
                CTotalizer = Convert.ToDouble(src.CTotalizer),
                CNumerator = src.CNumerator,
                XLastHash = src.XLastHash,
                XTotalizer = Convert.ToDouble(src.XTotalizer),
                XNumerator = src.XNumerator,
                ALastQueueItemId = src.ALastQueueItemId,
                ALastMoment = src.ALastMoment,
                APITotalUnknown = Convert.ToDouble(src.APITotalUnknown),
                APITotalInternal = Convert.ToDouble(src.APITotalInternal),
                GMonthCITotalReduced1 = Convert.ToDouble(src.GMonthCITotalReduced1),
                APITotalNonCash = Convert.ToDouble(src.APITotalNonCash),
                ACITotalUnknown = Convert.ToDouble(src.ACITotalUnknown),
                ACITotalZero = Convert.ToDouble(src.ACITotalZero),
                ACITotalReducedS = Convert.ToDouble(src.ACITotalReducedS),
                ACITotalReduced2 = Convert.ToDouble(src.ACITotalReduced2),
                ACITotalReduced1 = Convert.ToDouble(src.ACITotalReduced1),
                ACITotalNormal = Convert.ToDouble(src.ACITotalNormal),
                ATotalizer = Convert.ToDouble(src.ATotalizer),
                ALastHash = src.ALastHash,
                ANumerator = src.ANumerator,
                LLastHash = src.LLastHash,
                LNumerator = src.LNumerator,
                BLastHash = src.BLastHash,
                BPITotalUnknown = Convert.ToDouble(src.BPITotalUnknown),
                BPITotalInternal = Convert.ToDouble(src.BPITotalInternal),
                APITotalCash = Convert.ToDouble(src.APITotalCash),
                GMonthCITotalNormal = Convert.ToDouble(src.GMonthCITotalNormal),
                GMonthTotalizer = Convert.ToDouble(src.GMonthTotalizer),
                GLastDayQueueItemId = src.GLastDayQueueItemId,
                ICITotalReducedS = Convert.ToDouble(src.ICITotalReducedS),
                ICITotalReduced2 = Convert.ToDouble(src.ICITotalReduced2),
                ICITotalReduced1 = Convert.ToDouble(src.ICITotalReduced1),
                ICITotalNormal = Convert.ToDouble(src.ICITotalNormal),
                ITotalizer = Convert.ToDouble(src.ITotalizer),
                INumerator = src.INumerator,
                PLastHash = src.PLastHash,
                PPITotalUnknown = Convert.ToDouble(src.PPITotalUnknown),
                PPITotalInternal = Convert.ToDouble(src.PPITotalInternal),
                PPITotalNonCash = Convert.ToDouble(src.PPITotalNonCash),
                PPITotalCash = Convert.ToDouble(src.PPITotalCash),
                PTotalizer = Convert.ToDouble(src.PTotalizer),
                PNumerator = src.PNumerator,
                TLastHash = src.TLastHash,
                ICITotalZero = Convert.ToDouble(src.ICITotalZero),
                TPITotalUnknown = Convert.ToDouble(src.TPITotalUnknown),
                TPITotalNonCash = Convert.ToDouble(src.TPITotalNonCash),
                TPITotalCash = Convert.ToDouble(src.TPITotalCash),
                TCITotalUnknown = Convert.ToDouble(src.TCITotalUnknown),
                TCITotalZero = Convert.ToDouble(src.TCITotalZero),
                TCITotalReducedS = Convert.ToDouble(src.TCITotalReducedS),
                TCITotalReduced2 = Convert.ToDouble(src.TCITotalReduced2),
                TCITotalReduced1 = Convert.ToDouble(src.TCITotalReduced1),
                TCITotalNormal = Convert.ToDouble(src.TCITotalNormal),
                TTotalizer = Convert.ToDouble(src.TTotalizer),
                TNumerator = src.TNumerator,
                CashBoxIdentification = src.CashBoxIdentification,
                Siret = src.Siret,
                TPITotalInternal = Convert.ToDouble(src.TPITotalInternal),
                MessageCount = src.MessageCount,
                ICITotalUnknown = Convert.ToDouble(src.ICITotalUnknown),
                IPITotalNonCash = Convert.ToDouble(src.IPITotalNonCash),
                GLastDayMoment = src.GLastDayMoment,
                GDayPITotalUnknown = Convert.ToDouble(src.GDayPITotalUnknown),
                GDayPITotalInternal = Convert.ToDouble(src.GDayPITotalInternal),
                GDayPITotalNonCash = Convert.ToDouble(src.GDayPITotalNonCash),
                GDayPITotalCash = Convert.ToDouble(src.GDayPITotalCash),
                GDayCITotalUnknown = Convert.ToDouble(src.GDayCITotalUnknown),
                GDayCITotalZero = Convert.ToDouble(src.GDayCITotalZero),
                GDayCITotalReducedS = Convert.ToDouble(src.GDayCITotalReducedS),
                GDayCITotalReduced2 = Convert.ToDouble(src.GDayCITotalReduced2),
                GDayCITotalReduced1 = Convert.ToDouble(src.GDayCITotalReduced1),
                GDayCITotalNormal = Convert.ToDouble(src.GDayCITotalNormal),
                GDayTotalizer = Convert.ToDouble(src.GDayTotalizer),
                GLastShiftQueueItemId = src.GLastShiftQueueItemId,
                GLastShiftMoment = src.GLastShiftMoment,
                IPITotalCash = Convert.ToDouble(src.IPITotalCash),
                GShiftPITotalNonCash = Convert.ToDouble(src.GShiftPITotalNonCash),
                GShiftPITotalUnknown = Convert.ToDouble(src.GShiftPITotalUnknown),
                GShiftPITotalCash = Convert.ToDouble(src.GShiftPITotalCash),
                GShiftCITotalUnknown = Convert.ToDouble(src.GShiftCITotalUnknown),
                GShiftCITotalReducedS = Convert.ToDouble(src.GShiftCITotalReducedS),
                GShiftCITotalZero = Convert.ToDouble(src.GShiftCITotalZero),
                GShiftCITotalReduced2 = Convert.ToDouble(src.GShiftCITotalReduced2),
                GShiftCITotalReduced1 = Convert.ToDouble(src.GShiftCITotalReduced1),
                GShiftCITotalNormal = Convert.ToDouble(src.GShiftCITotalNormal),
                GShiftTotalizer = Convert.ToDouble(src.GShiftTotalizer),
                GLastHash = src.GLastHash,
                GNumerator = src.GNumerator,
                ILastHash = src.ILastHash,
                IPITotalUnknown = Convert.ToDouble(src.IPITotalUnknown),
                IPITotalInternal = Convert.ToDouble(src.IPITotalInternal),
                GShiftPITotalInternal = Convert.ToDouble(src.GShiftPITotalInternal),
                MessageMoment = src.MessageMoment,
                TimeStamp = src.TimeStamp
            };
        }

        protected override ftQueueFR MapToStorageEntity(AzureTableStorageFtQueueFR src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftQueueFR
            {
                ftQueueFRId = src.ftQueueFRId,
                ftSignaturCreationUnitFRId = src.ftSignaturCreationUnitFRId,
                BCITotalReducedS = Convert.ToDecimal(src.BCITotalReducedS),
                BCITotalReduced2 = Convert.ToDecimal(src.BCITotalReduced2),
                BCITotalReduced1 = Convert.ToDecimal(src.BCITotalReduced1),
                BCITotalNormal = Convert.ToDecimal(src.BCITotalNormal),
                BTotalizer = Convert.ToDecimal(src.BTotalizer),
                BNumerator = src.BNumerator,
                GLastYearQueueItemId = src.GLastYearQueueItemId,
                GLastYearMoment = src.GLastYearMoment,
                GYearPITotalUnknown = Convert.ToDecimal(src.GYearPITotalUnknown),
                GYearPITotalInternal = Convert.ToDecimal(src.GYearPITotalInternal),
                GYearPITotalNonCash = Convert.ToDecimal(src.GYearPITotalNonCash),
                GYearPITotalCash = Convert.ToDecimal(src.GYearPITotalCash),
                GYearCITotalUnknown = Convert.ToDecimal(src.GYearCITotalUnknown),
                GYearCITotalZero = Convert.ToDecimal(src.GYearCITotalZero),
                GYearCITotalReducedS = Convert.ToDecimal(src.GYearCITotalReducedS),
                GYearCITotalReduced2 = Convert.ToDecimal(src.GYearCITotalReduced2),
                GYearCITotalReduced1 = Convert.ToDecimal(src.GYearCITotalReduced1),
                GYearCITotalNormal = Convert.ToDecimal(src.GYearCITotalNormal),
                GYearTotalizer = Convert.ToDecimal(src.GYearTotalizer),
                GLastMonthQueueItemId = src.GLastMonthQueueItemId,
                GLastMonthMoment = src.GLastMonthMoment,
                GMonthPITotalUnknown = Convert.ToDecimal(src.GMonthPITotalUnknown),
                GMonthPITotalInternal = Convert.ToDecimal(src.GMonthPITotalInternal),
                GMonthPITotalNonCash = Convert.ToDecimal(src.GMonthPITotalNonCash),
                GMonthPITotalCash = Convert.ToDecimal(src.GMonthPITotalCash),
                GMonthCITotalUnknown = Convert.ToDecimal(src.GMonthCITotalUnknown),
                GMonthCITotalZero = Convert.ToDecimal(src.GMonthCITotalZero),
                GMonthCITotalReducedS = Convert.ToDecimal(src.GMonthCITotalReducedS),
                GMonthCITotalReduced2 = Convert.ToDecimal(src.GMonthCITotalReduced2),
                BCITotalZero = Convert.ToDecimal(src.BCITotalZero),
                BCITotalUnknown = Convert.ToDecimal(src.BCITotalUnknown),
                BPITotalCash = Convert.ToDecimal(src.BPITotalCash),
                BPITotalNonCash = Convert.ToDecimal(src.BPITotalNonCash),
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                UsedFailedMomentMax = src.UsedFailedMomentMax,
                UsedFailedMomentMin = src.UsedFailedMomentMin,
                UsedFailedCount = src.UsedFailedCount,
                CLastHash = src.CLastHash,
                CTotalizer = Convert.ToDecimal(src.CTotalizer),
                CNumerator = src.CNumerator,
                XLastHash = src.XLastHash,
                XTotalizer = Convert.ToDecimal(src.XTotalizer),
                XNumerator = src.XNumerator,
                ALastQueueItemId = src.ALastQueueItemId,
                ALastMoment = src.ALastMoment,
                APITotalUnknown = Convert.ToDecimal(src.APITotalUnknown),
                APITotalInternal = Convert.ToDecimal(src.APITotalInternal),
                GMonthCITotalReduced1 = Convert.ToDecimal(src.GMonthCITotalReduced1),
                APITotalNonCash = Convert.ToDecimal(src.APITotalNonCash),
                ACITotalUnknown = Convert.ToDecimal(src.ACITotalUnknown),
                ACITotalZero = Convert.ToDecimal(src.ACITotalZero),
                ACITotalReducedS = Convert.ToDecimal(src.ACITotalReducedS),
                ACITotalReduced2 = Convert.ToDecimal(src.ACITotalReduced2),
                ACITotalReduced1 = Convert.ToDecimal(src.ACITotalReduced1),
                ACITotalNormal = Convert.ToDecimal(src.ACITotalNormal),
                ATotalizer = Convert.ToDecimal(src.ATotalizer),
                ALastHash = src.ALastHash,
                ANumerator = src.ANumerator,
                LLastHash = src.LLastHash,
                LNumerator = src.LNumerator,
                BLastHash = src.BLastHash,
                BPITotalUnknown = Convert.ToDecimal(src.BPITotalUnknown),
                BPITotalInternal = Convert.ToDecimal(src.BPITotalInternal),
                APITotalCash = Convert.ToDecimal(src.APITotalCash),
                GMonthCITotalNormal = Convert.ToDecimal(src.GMonthCITotalNormal),
                GMonthTotalizer = Convert.ToDecimal(src.GMonthTotalizer),
                GLastDayQueueItemId = src.GLastDayQueueItemId,
                ICITotalReducedS = Convert.ToDecimal(src.ICITotalReducedS),
                ICITotalReduced2 = Convert.ToDecimal(src.ICITotalReduced2),
                ICITotalReduced1 = Convert.ToDecimal(src.ICITotalReduced1),
                ICITotalNormal = Convert.ToDecimal(src.ICITotalNormal),
                ITotalizer = Convert.ToDecimal(src.ITotalizer),
                INumerator = src.INumerator,
                PLastHash = src.PLastHash,
                PPITotalUnknown = Convert.ToDecimal(src.PPITotalUnknown),
                PPITotalInternal = Convert.ToDecimal(src.PPITotalInternal),
                PPITotalNonCash = Convert.ToDecimal(src.PPITotalNonCash),
                PPITotalCash = Convert.ToDecimal(src.PPITotalCash),
                PTotalizer = Convert.ToDecimal(src.PTotalizer),
                PNumerator = src.PNumerator,
                TLastHash = src.TLastHash,
                ICITotalZero = Convert.ToDecimal(src.ICITotalZero),
                TPITotalUnknown = Convert.ToDecimal(src.TPITotalUnknown),
                TPITotalNonCash = Convert.ToDecimal(src.TPITotalNonCash),
                TPITotalCash = Convert.ToDecimal(src.TPITotalCash),
                TCITotalUnknown = Convert.ToDecimal(src.TCITotalUnknown),
                TCITotalZero = Convert.ToDecimal(src.TCITotalZero),
                TCITotalReducedS = Convert.ToDecimal(src.TCITotalReducedS),
                TCITotalReduced2 = Convert.ToDecimal(src.TCITotalReduced2),
                TCITotalReduced1 = Convert.ToDecimal(src.TCITotalReduced1),
                TCITotalNormal = Convert.ToDecimal(src.TCITotalNormal),
                TTotalizer = Convert.ToDecimal(src.TTotalizer),
                TNumerator = src.TNumerator,
                CashBoxIdentification = src.CashBoxIdentification,
                Siret = src.Siret,
                TPITotalInternal = Convert.ToDecimal(src.TPITotalInternal),
                MessageCount = src.MessageCount,
                ICITotalUnknown = Convert.ToDecimal(src.ICITotalUnknown),
                IPITotalNonCash = Convert.ToDecimal(src.IPITotalNonCash),
                GLastDayMoment = src.GLastDayMoment,
                GDayPITotalUnknown = Convert.ToDecimal(src.GDayPITotalUnknown),
                GDayPITotalInternal = Convert.ToDecimal(src.GDayPITotalInternal),
                GDayPITotalNonCash = Convert.ToDecimal(src.GDayPITotalNonCash),
                GDayPITotalCash = Convert.ToDecimal(src.GDayPITotalCash),
                GDayCITotalUnknown = Convert.ToDecimal(src.GDayCITotalUnknown),
                GDayCITotalZero = Convert.ToDecimal(src.GDayCITotalZero),
                GDayCITotalReducedS = Convert.ToDecimal(src.GDayCITotalReducedS),
                GDayCITotalReduced2 = Convert.ToDecimal(src.GDayCITotalReduced2),
                GDayCITotalReduced1 = Convert.ToDecimal(src.GDayCITotalReduced1),
                GDayCITotalNormal = Convert.ToDecimal(src.GDayCITotalNormal),
                GDayTotalizer = Convert.ToDecimal(src.GDayTotalizer),
                GLastShiftQueueItemId = src.GLastShiftQueueItemId,
                GLastShiftMoment = src.GLastShiftMoment,
                IPITotalCash = Convert.ToDecimal(src.IPITotalCash),
                GShiftPITotalNonCash = Convert.ToDecimal(src.GShiftPITotalNonCash),
                GShiftPITotalUnknown = Convert.ToDecimal(src.GShiftPITotalUnknown),
                GShiftPITotalCash = Convert.ToDecimal(src.GShiftPITotalCash),
                GShiftCITotalUnknown = Convert.ToDecimal(src.GShiftCITotalUnknown),
                GShiftCITotalReducedS = Convert.ToDecimal(src.GShiftCITotalReducedS),
                GShiftCITotalZero = Convert.ToDecimal(src.GShiftCITotalZero),
                GShiftCITotalReduced2 = Convert.ToDecimal(src.GShiftCITotalReduced2),
                GShiftCITotalReduced1 = Convert.ToDecimal(src.GShiftCITotalReduced1),
                GShiftCITotalNormal = Convert.ToDecimal(src.GShiftCITotalNormal),
                GShiftTotalizer = Convert.ToDecimal(src.GShiftTotalizer),
                GLastHash = src.GLastHash,
                GNumerator = src.GNumerator,
                ILastHash = src.ILastHash,
                IPITotalUnknown = Convert.ToDecimal(src.IPITotalUnknown),
                IPITotalInternal = Convert.ToDecimal(src.IPITotalInternal),
                GShiftPITotalInternal = Convert.ToDecimal(src.GShiftPITotalInternal),
                MessageMoment = src.MessageMoment,
                TimeStamp = src.TimeStamp
            };
        }
    }
}

