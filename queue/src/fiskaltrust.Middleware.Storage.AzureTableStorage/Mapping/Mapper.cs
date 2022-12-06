using System;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Azure.Data.Tables;
using Azure.Core;
using Azure;
using System.Linq;
using System.Text;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Extensions;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.MasterData;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping
{
    public static class Mapper
    {
        // 64KiB, roughly 32K characters
        private const int MAX_STRING_CHARS = 32_000;

        public static AzureFtActionJournal Map(ftActionJournal src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureFtActionJournal
            {
                PartitionKey = GetHashString(src.TimeStamp),
                RowKey = src.ftActionJournalId.ToString(),
                ftActionJournalId = src.ftActionJournalId,
                ftQueueId = src.ftQueueId,
                ftQueueItemId = src.ftQueueItemId,
                Moment = src.Moment,
                Priority = src.Priority,
                Type = src.Type,
                Message = src.Message,
                DataBase64 = src.DataBase64,
                DataJson = src.DataJson,
                TimeStamp = src.TimeStamp
            };
        }

        public static ftActionJournal Map(AzureFtActionJournal src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftActionJournal
            {
                ftActionJournalId = src.ftActionJournalId,
                ftQueueId = src.ftQueueId,
                ftQueueItemId = src.ftQueueItemId,
                Moment = src.Moment,
                Priority = src.Priority,
                Type = src.Type,
                Message = src.Message,
                DataBase64 = src.DataBase64,
                DataJson = src.DataJson,
                TimeStamp = src.TimeStamp
            };
        }

        public static AzureFtReceiptJournal Map(ftReceiptJournal src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureFtReceiptJournal
            {
                PartitionKey = GetHashString(src.TimeStamp),
                RowKey = src.ftReceiptJournalId.ToString(),
                ftReceiptJournalId = src.ftReceiptJournalId,
                ftQueueId = src.ftQueueId,
                ftQueueItemId = src.ftQueueItemId,
                ftReceiptHash = src.ftReceiptHash,
                ftReceiptMoment = src.ftReceiptMoment,
                ftReceiptNumber = src.ftReceiptNumber,
                ftReceiptTotal = Convert.ToDouble(src.ftReceiptTotal),
                TimeStamp = src.TimeStamp
            };
        }

        public static ftReceiptJournal Map(AzureFtReceiptJournal src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftReceiptJournal
            {
                ftReceiptJournalId = src.ftReceiptJournalId,
                ftQueueId = src.ftQueueId,
                ftQueueItemId = src.ftQueueItemId,
                ftReceiptHash = src.ftReceiptHash,
                ftReceiptMoment = src.ftReceiptMoment,
                ftReceiptNumber = src.ftReceiptNumber,
                ftReceiptTotal = Convert.ToDecimal(src.ftReceiptTotal),
                TimeStamp = src.TimeStamp
            };
        }

        public static AzureFtJournalAT Map(ftJournalAT src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureFtJournalAT
            {
                PartitionKey = GetHashString(src.TimeStamp),
                RowKey = src.ftJournalATId.ToString(),
                ftJournalATId = src.ftJournalATId,
                ftQueueId = src.ftQueueId,
                ftSignaturCreationUnitId = src.ftSignaturCreationUnitId,
                Number = src.Number,
                JWSHeaderBase64url = src.JWSHeaderBase64url,
                JWSPayloadBase64url = src.JWSPayloadBase64url,
                JWSSignatureBase64url = src.JWSSignatureBase64url,
                TimeStamp = src.TimeStamp
            };
        }

        public static ftJournalAT Map(AzureFtJournalAT src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftJournalAT
            {
                ftJournalATId = src.ftJournalATId,
                ftQueueId = src.ftQueueId,
                ftSignaturCreationUnitId = src.ftSignaturCreationUnitId,
                Number = src.Number,
                JWSHeaderBase64url = src.JWSHeaderBase64url,
                JWSPayloadBase64url = src.JWSPayloadBase64url,
                JWSSignatureBase64url = src.JWSSignatureBase64url,
                TimeStamp = src.TimeStamp
            };
        }

        public static AzureFtJournalDE Map(ftJournalDE src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureFtJournalDE
            {
                PartitionKey = GetHashString(src.TimeStamp),
                RowKey = src.ftJournalDEId.ToString(),
                ftJournalDEId = src.ftJournalDEId,
                ftQueueId = src.ftQueueId,
                Number = src.Number,
                ftQueueItemId = src.ftQueueItemId,
                FileContentBase64 = src.FileContentBase64,
                FileExtension = src.FileExtension,
                FileName = src.FileName,
                TimeStamp = src.TimeStamp
            };
        }

        public static ftJournalDE Map(AzureFtJournalDE src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftJournalDE
            {
                ftJournalDEId = src.ftJournalDEId,
                ftQueueId = src.ftQueueId,
                ftQueueItemId = src.ftQueueItemId,
                FileContentBase64 = src.FileContentBase64,
                FileExtension = src.FileExtension,
                FileName = src.FileName,
                Number = src.Number,
                TimeStamp = src.TimeStamp,
            };
        }

        public static AzureFtJournalFR Map(ftJournalFR src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureFtJournalFR
            {
                PartitionKey = GetHashString(src.TimeStamp),
                RowKey = src.ftJournalFRId.ToString(),
                ftJournalFRId = src.ftJournalFRId,
                ftQueueId = src.ftQueueId,
                ftQueueItemId = src.ftQueueItemId,
                Number = src.Number,
                JWT = src.JWT,
                JsonData = src.JsonData,
                ReceiptType = src.ReceiptType,
                TimeStamp = src.TimeStamp
            };
        }

        public static AzureFtJournalME Map(ftJournalME src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureFtJournalME
            {
                PartitionKey = GetHashString(src.TimeStamp),
                ftJournalMEId = src.ftJournalMEId,
                ftQueueItemId = src.ftQueueItemId,
                cbReference = src.cbReference,
                InvoiceNumber = src.InvoiceNumber,
                YearlyOrdinalNumber = src.YearlyOrdinalNumber,
                ftQueueId = src.ftQueueId,
                TimeStamp = src.TimeStamp
            };
        }

        public static ftJournalME Map(AzureFtJournalME src)
        {
            if (src == null)

            {
                return null;
            }

            return new ftJournalME
            {
                ftJournalMEId = src.ftJournalMEId,
                ftQueueItemId = src.ftQueueItemId,
                cbReference = src.cbReference,
                InvoiceNumber = src.InvoiceNumber,
                YearlyOrdinalNumber = src.YearlyOrdinalNumber,
                ftQueueId = src.ftQueueId,
                TimeStamp = src.TimeStamp
            };
        }

        public static ftJournalFR Map(AzureFtJournalFR src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftJournalFR
            {
                ftJournalFRId = src.ftJournalFRId,
                ftQueueId = src.ftQueueId,
                ftQueueItemId = src.ftQueueItemId,
                Number = src.Number,
                JWT = src.JWT,
                JsonData = src.JsonData,
                ReceiptType = src.ReceiptType,
                TimeStamp = src.TimeStamp
            };
        }

        public static AzureFtCashBox Map(ftCashBox src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureFtCashBox
            {
                PartitionKey = src.ftCashBoxId.ToString(),
                RowKey = src.ftCashBoxId.ToString(),
                ftCashBoxId = src.ftCashBoxId,
                TimeStamp = src.TimeStamp
            };
        }

        public static ftCashBox Map(AzureFtCashBox src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftCashBox
            {
                ftCashBoxId = src.ftCashBoxId,
                TimeStamp = src.TimeStamp
            };
        }

        public static AzureFtQueueAT Map(ftQueueAT src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureFtQueueAT
            {
                PartitionKey = src.ftQueueATId.ToString(),
                RowKey = src.ftQueueATId.ToString(),
                ftQueueATId = src.ftQueueATId,
                LastSignatureCertificateSerialNumber = src.LastSignatureCertificateSerialNumber,
                LastSignatureZDA = src.LastSignatureZDA,
                LastSignatureHash = src.LastSignatureHash,
                MessageMoment = src.MessageMoment,
                MessageCount = src.MessageCount,
                UsedMobileQueueItemId = src.UsedMobileQueueItemId,
                UsedMobileMoment = src.UsedMobileMoment,
                UsedMobileCount = src.UsedMobileCount,
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                UsedFailedMomentMax = src.UsedFailedMomentMax,
                UsedFailedMomentMin = src.UsedFailedMomentMin,
                UsedFailedCount = src.UsedFailedCount,
                ftCashNumerator = src.ftCashNumerator,
                SSCDFailMessageSent = src.SSCDFailMessageSent,
                SSCDFailMoment = src.SSCDFailMoment,
                SSCDFailCount = src.SSCDFailCount,
                LastSettlementQueueItemId = src.LastSettlementQueueItemId,
                LastSettlementMoment = src.LastSettlementMoment,
                LastSettlementMonth = src.LastSettlementMonth,
                ClosedSystemNote = src.ClosedSystemNote,
                ClosedSystemValue = src.ClosedSystemValue,
                ClosedSystemKind = src.ClosedSystemKind,
                SignAll = src.SignAll,
                EncryptionKeyBase64 = src.EncryptionKeyBase64,
                CashBoxIdentification = src.CashBoxIdentification,
                SSCDFailQueueItemId = src.SSCDFailQueueItemId,
                ftCashTotalizer = Convert.ToDouble(src.ftCashTotalizer),
                TimeStamp = src.TimeStamp
            };
        }

        public static ftQueueAT Map(AzureFtQueueAT src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftQueueAT
            {
                ftQueueATId = src.ftQueueATId,
                LastSignatureCertificateSerialNumber = src.LastSignatureCertificateSerialNumber,
                LastSignatureZDA = src.LastSignatureZDA,
                LastSignatureHash = src.LastSignatureHash,
                MessageMoment = src.MessageMoment,
                MessageCount = src.MessageCount,
                UsedMobileQueueItemId = src.UsedMobileQueueItemId,
                UsedMobileMoment = src.UsedMobileMoment,
                UsedMobileCount = src.UsedMobileCount,
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                UsedFailedMomentMax = src.UsedFailedMomentMax,
                UsedFailedMomentMin = src.UsedFailedMomentMin,
                UsedFailedCount = src.UsedFailedCount,
                ftCashNumerator = src.ftCashNumerator,
                SSCDFailMessageSent = src.SSCDFailMessageSent,
                SSCDFailMoment = src.SSCDFailMoment,
                SSCDFailCount = src.SSCDFailCount,
                LastSettlementQueueItemId = src.LastSettlementQueueItemId,
                LastSettlementMoment = src.LastSettlementMoment,
                LastSettlementMonth = src.LastSettlementMonth,
                ClosedSystemNote = src.ClosedSystemNote,
                ClosedSystemValue = src.ClosedSystemValue,
                ClosedSystemKind = src.ClosedSystemKind,
                SignAll = src.SignAll,
                EncryptionKeyBase64 = src.EncryptionKeyBase64,
                CashBoxIdentification = src.CashBoxIdentification,
                SSCDFailQueueItemId = src.SSCDFailQueueItemId,
                ftCashTotalizer = Convert.ToDecimal(src.ftCashTotalizer),
                TimeStamp = src.TimeStamp
            };
        }

        public static AzureFtQueueDE Map(ftQueueDE src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureFtQueueDE
            {
                PartitionKey = src.ftQueueDEId.ToString(),
                RowKey = src.ftQueueDEId.ToString(),
                ftQueueDEId = src.ftQueueDEId,
                ftSignaturCreationUnitDEId = src.ftSignaturCreationUnitDEId,
                LastHash = src.LastHash,
                CashBoxIdentification = src.CashBoxIdentification,
                SSCDFailCount = src.SSCDFailCount,
                SSCDFailMoment = src.SSCDFailMoment,
                SSCDFailQueueItemId = src.SSCDFailQueueItemId,
                UsedFailedCount = src.UsedFailedCount,
                UsedFailedMomentMin = src.UsedFailedMomentMin,
                UsedFailedMomentMax = src.UsedFailedMomentMax,
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                TimeStamp = src.TimeStamp,
                DailyClosingNumber = src.DailyClosingNumber
            };
        }

        public static ftQueueDE Map(AzureFtQueueDE src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftQueueDE
            {
                ftQueueDEId = src.ftQueueDEId,
                ftSignaturCreationUnitDEId = src.ftSignaturCreationUnitDEId,
                LastHash = src.LastHash,
                CashBoxIdentification = src.CashBoxIdentification,
                SSCDFailCount = src.SSCDFailCount,
                SSCDFailMoment = src.SSCDFailMoment,
                SSCDFailQueueItemId = src.SSCDFailQueueItemId,
                UsedFailedCount = src.UsedFailedCount,
                UsedFailedMomentMin = src.UsedFailedMomentMin,
                UsedFailedMomentMax = src.UsedFailedMomentMax,
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                TimeStamp = src.TimeStamp,
                DailyClosingNumber = src.DailyClosingNumber
            };
        }

        public static AzureFtQueueME Map(ftQueueME src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureFtQueueME
            {
                PartitionKey = src.ftQueueMEId.ToString(),
                RowKey = src.ftQueueMEId.ToString(),
                ftQueueMEId = src.ftQueueMEId,
                ftSignaturCreationUnitMEId = src.ftSignaturCreationUnitMEId,
                LastHash = src.LastHash,
                SSCDFailCount = src.SSCDFailCount,
                SSCDFailMoment = src.SSCDFailMoment,
                SSCDFailQueueItemId = src.SSCDFailQueueItemId,
                UsedFailedCount = src.UsedFailedCount,
                UsedFailedMomentMin = src.UsedFailedMomentMin,
                UsedFailedMomentMax = src.UsedFailedMomentMax,
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                TimeStamp = src.TimeStamp,
                DailyClosingNumber = src.DailyClosingNumber,
            };
        }

        public static ftQueueME Map(AzureFtQueueME src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftQueueME
            {
                ftQueueMEId = src.ftQueueMEId,
                ftSignaturCreationUnitMEId = src.ftSignaturCreationUnitMEId,
                LastHash = src.LastHash,
                SSCDFailCount = src.SSCDFailCount,
                SSCDFailMoment = src.SSCDFailMoment,
                SSCDFailQueueItemId = src.SSCDFailQueueItemId,
                UsedFailedCount = src.UsedFailedCount,
                UsedFailedMomentMin = src.UsedFailedMomentMin,
                UsedFailedMomentMax = src.UsedFailedMomentMax,
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                TimeStamp = src.TimeStamp,
                DailyClosingNumber = src.DailyClosingNumber
            };
        }

        public static AzureFtQueueFR Map(ftQueueFR src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureFtQueueFR
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

        public static ftQueueFR Map(AzureFtQueueFR src)
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

        public static AzureFtQueue Map(ftQueue src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureFtQueue
            {
                PartitionKey = src.ftQueueId.ToString(),
                RowKey = src.ftQueueId.ToString(),
                ftQueueId = src.ftQueueId,
                ftCashBoxId = src.ftCashBoxId,
                ftCurrentRow = src.ftCurrentRow,
                ftQueuedRow = src.ftQueuedRow,
                ftReceiptNumerator = src.ftReceiptNumerator,
                ftReceiptTotalizer = Convert.ToDouble(src.ftReceiptTotalizer),
                ftReceiptHash = src.ftReceiptHash,
                StartMoment = src.StartMoment,
                StopMoment = src.StopMoment,
                CountryCode = src.CountryCode,
                Timeout = src.Timeout,
                TimeStamp = src.TimeStamp
            };
        }

        public static ftQueue Map(AzureFtQueue src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftQueue
            {
                ftQueueId = src.ftQueueId,
                ftCashBoxId = src.ftCashBoxId,
                ftCurrentRow = src.ftCurrentRow,
                ftQueuedRow = src.ftQueuedRow,
                ftReceiptNumerator = src.ftReceiptNumerator,
                ftReceiptTotalizer = Convert.ToDecimal(src.ftReceiptTotalizer),
                ftReceiptHash = src.ftReceiptHash,
                StartMoment = src.StartMoment,
                StopMoment = src.StopMoment,
                CountryCode = src.CountryCode,
                Timeout = src.Timeout,
                TimeStamp = src.TimeStamp
            };
        }

        public static AzureFtSignaturCreationUnitAT Map(ftSignaturCreationUnitAT src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureFtSignaturCreationUnitAT
            {
                PartitionKey = src.ftSignaturCreationUnitATId.ToString(),
                RowKey = src.ftSignaturCreationUnitATId.ToString(),
                ftSignaturCreationUnitATId = src.ftSignaturCreationUnitATId,
                Url = src.Url,
                ZDA = src.ZDA,
                SN = src.SN,
                CertificateBase64 = src.CertificateBase64,
                Mode = src.Mode,
                TimeStamp = src.TimeStamp
            };
        }

        public static ftSignaturCreationUnitAT Map(AzureFtSignaturCreationUnitAT src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftSignaturCreationUnitAT
            {
                ftSignaturCreationUnitATId = src.ftSignaturCreationUnitATId,
                Url = src.Url,
                ZDA = src.ZDA,
                SN = src.SN,
                CertificateBase64 = src.CertificateBase64,
                Mode = src.Mode,
                TimeStamp = src.TimeStamp
            };
        }

        public static AzureFtSignaturCreationUnitDE Map(ftSignaturCreationUnitDE src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureFtSignaturCreationUnitDE
            {
                PartitionKey = src.ftSignaturCreationUnitDEId.ToString(),
                RowKey = src.ftSignaturCreationUnitDEId.ToString(),
                ftSignaturCreationUnitDEId = src.ftSignaturCreationUnitDEId,
                Url = src.Url,
                TseInfoJson = src.TseInfoJson,
                TimeStamp = src.TimeStamp,
                Mode = src.Mode,
                ModeConfigurationJson = src.ModeConfigurationJson
            };
        }

        public static ftSignaturCreationUnitDE Map(AzureFtSignaturCreationUnitDE src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftSignaturCreationUnitDE
            {
                ftSignaturCreationUnitDEId = src.ftSignaturCreationUnitDEId,
                TseInfoJson = src.TseInfoJson,
                TimeStamp = src.TimeStamp,
                Mode = src.Mode,
                ModeConfigurationJson = src.ModeConfigurationJson,
                Url = src.Url
            };
        }

        public static AzureFtSignaturCreationUnitME Map(ftSignaturCreationUnitME src)
        {
            if (src == null)
            {
                return null;
            }
            return new AzureFtSignaturCreationUnitME
            {
                PartitionKey = src.ftSignaturCreationUnitMEId.ToString(),
                RowKey = src.ftSignaturCreationUnitMEId.ToString(),
                ftSignaturCreationUnitMEId = src.ftSignaturCreationUnitMEId,
                TimeStamp = src.TimeStamp,
                IssuerTin = src.IssuerTin,
                BusinessUnitCode = src.BusinessUnitCode,
                TcrIntId = src.TcrIntId,
                SoftwareCode = src.SoftwareCode,
                MaintainerCode = src.MaintainerCode,
                ValidFrom = src.ValidFrom,
                ValidTo = src.ValidTo,
                TcrCode = src.TcrCode
            };
        }

        public static ftSignaturCreationUnitME Map(AzureFtSignaturCreationUnitME src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftSignaturCreationUnitME
            {
                ftSignaturCreationUnitMEId = src.ftSignaturCreationUnitMEId,
                TimeStamp = src.TimeStamp,
                IssuerTin = src.IssuerTin,
                BusinessUnitCode = src.BusinessUnitCode,
                TcrIntId = src.TcrIntId,
                SoftwareCode = src.SoftwareCode,
                MaintainerCode = src.MaintainerCode,
                ValidFrom = src.ValidFrom,
                ValidTo = src.ValidTo,
                TcrCode = src.TcrCode
            };
        }

        public static AzureFtSignaturCreationUnitFR Map(ftSignaturCreationUnitFR src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureFtSignaturCreationUnitFR
            {
                PartitionKey = src.ftSignaturCreationUnitFRId.ToString(),
                RowKey = src.ftSignaturCreationUnitFRId.ToString(),
                ftSignaturCreationUnitFRId = src.ftSignaturCreationUnitFRId,
                Siret = src.Siret,
                PrivateKey = src.PrivateKey,
                CertificateBase64 = src.CertificateBase64,
                CertificateSerialNumber = src.CertificateSerialNumber,
                TimeStamp = src.TimeStamp
            };
        }

        public static ftSignaturCreationUnitFR Map(AzureFtSignaturCreationUnitFR src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftSignaturCreationUnitFR
            {
                ftSignaturCreationUnitFRId = src.ftSignaturCreationUnitFRId,
                Siret = src.Siret,
                PrivateKey = src.PrivateKey,
                CertificateBase64 = src.CertificateBase64,
                CertificateSerialNumber = src.CertificateSerialNumber,
                TimeStamp = src.TimeStamp
            };
        }

        public static TableEntity Map(ftQueueItem src)
        {
            if (src == null)
            {
                return null;
            }

            var entity = new TableEntity(GetHashString(src.TimeStamp), src.ftQueueItemId.ToString())
            {
                { nameof(ftQueueItem.ftQueueItemId), src.ftQueueItemId },
                { nameof(ftQueueItem.requestHash), src.requestHash },
                { nameof(ftQueueItem.responseHash), src.responseHash },
                { nameof(ftQueueItem.version), src.version },
                { nameof(ftQueueItem.country), src.country },
                { nameof(ftQueueItem.cbReceiptReference), src.cbReceiptReference },
                { nameof(ftQueueItem.cbTerminalID), src.cbTerminalID },
                { nameof(ftQueueItem.cbReceiptMoment), src.cbReceiptMoment },
                { nameof(ftQueueItem.ftDoneMoment), src.ftDoneMoment },
                { nameof(ftQueueItem.ftWorkMoment), src.ftWorkMoment },
                { nameof(ftQueueItem.ftQueueTimeout), src.ftQueueTimeout },
                { nameof(ftQueueItem.ftQueueMoment), src.ftQueueMoment },
                { nameof(ftQueueItem.ftQueueRow), src.ftQueueRow },
                { nameof(ftQueueItem.ftQueueId), src.ftQueueId },
                { nameof(ftQueueItem.TimeStamp), src.TimeStamp }
            };

            var currentRequestChunk = 0;
            foreach (var chunk in src.request.Chunk(MAX_STRING_CHARS))
            {
                entity.Add($"{nameof(ftQueueItem.request)}_{currentRequestChunk}", chunk);
                currentRequestChunk++;
            }

            var currentResponseChunk = 0;
            foreach (var chunk in src.response.Chunk(MAX_STRING_CHARS))
            {
                entity.Add($"{nameof(ftQueueItem.response)}_{currentResponseChunk}", chunk);
                currentResponseChunk++;
            }

            return entity;
        }

        public static ftQueueItem Map(TableEntity src)
        {
            if (src == null)
            {
                return null;
            }

            var queueItem = new ftQueueItem
            {
                ftQueueItemId = src.GetGuid(nameof(ftQueueItem.ftQueueItemId)).GetValueOrDefault(),
                requestHash = src.GetString(nameof(ftQueueItem.requestHash)),
                responseHash = src.GetString(nameof(ftQueueItem.responseHash)),
                version = src.GetString(nameof(ftQueueItem.version)),
                country = src.GetString(nameof(ftQueueItem.country)),
                cbReceiptReference = src.GetString(nameof(ftQueueItem.cbReceiptReference)),
                cbTerminalID = src.GetString(nameof(ftQueueItem.cbTerminalID)),
                cbReceiptMoment = src.GetDateTime(nameof(ftQueueItem.cbReceiptMoment)).GetValueOrDefault(),
                ftDoneMoment = src.GetDateTime(nameof(ftQueueItem.ftDoneMoment)),
                ftWorkMoment = src.GetDateTime(nameof(ftQueueItem.ftWorkMoment)),
                ftQueueTimeout = src.GetInt32(nameof(ftQueueItem.ftQueueTimeout)).GetValueOrDefault(),
                ftQueueMoment = src.GetDateTime(nameof(ftQueueItem.ftQueueMoment)).GetValueOrDefault(),
                ftQueueRow = src.GetInt64(nameof(ftQueueItem.ftQueueRow)).GetValueOrDefault(),
                ftQueueId = src.GetGuid(nameof(ftQueueItem.ftQueueId)).GetValueOrDefault(),
                TimeStamp = src.GetInt64(nameof(ftQueueItem.TimeStamp)).GetValueOrDefault()
            };

            var reqSb = new StringBuilder();
            foreach (var key in src.Keys.Where(x => x.StartsWith($"{nameof(ftQueueItem.request)}_")))
            {
                reqSb.Append(src[key]);
            }
            queueItem.request = reqSb.ToString();

            var resSb = new StringBuilder();
            foreach (var key in src.Keys.Where(x => x.StartsWith($"{nameof(ftQueueItem.response)}_")))
            {
                resSb.Append(src[key]);
            }
            queueItem.response = resSb.ToString();

            return queueItem;
        }

        public static FailedFinishTransaction Map(AzureFailedFinishTransaction src)
        {
            if (src == null)
            {
                return null;
            }

            return new FailedFinishTransaction
            {
                cbReceiptReference = src.cbReceiptReference,
                CashBoxIdentification = src.CashBoxIdentification,
                FinishMoment = src.FinishMoment,
                ftQueueItemId = src.ftQueueItemId,
                Request = src.Request,
                TransactionNumber = src.TransactionNumber == null ?  null : Convert.ToInt64(src.TransactionNumber)
            };
        }

        public static AzureFailedFinishTransaction Map(FailedFinishTransaction src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureFailedFinishTransaction
            {
                PartitionKey = GetHashString(src.FinishMoment.Ticks),
                RowKey = src.cbReceiptReference,
                cbReceiptReference = src.cbReceiptReference,
                CashBoxIdentification = src.CashBoxIdentification,
                FinishMoment = src.FinishMoment,
                ftQueueItemId = src.ftQueueItemId,
                Request = src.Request,
                TransactionNumber = src.TransactionNumber?.ToString()
            };
        }

        public static FailedStartTransaction Map(AzureFailedStartTransaction src)
        {
            if (src == null)
            {
                return null;
            }

            return new FailedStartTransaction
            {
                cbReceiptReference = src.cbReceiptReference,
                CashBoxIdentification = src.CashBoxIdentification,
                ftQueueItemId = src.ftQueueItemId,
                Request = src.Request,
                StartMoment = src.StartMoment
            };
        }

        public static AzureFailedStartTransaction Map(FailedStartTransaction src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureFailedStartTransaction
            {
                PartitionKey = GetHashString(src.StartMoment.Ticks),
                RowKey = src.cbReceiptReference,
                cbReceiptReference = src.cbReceiptReference,
                CashBoxIdentification = src.CashBoxIdentification,
                ftQueueItemId = src.ftQueueItemId,
                Request = src.Request,
                StartMoment = src.StartMoment
            };
        }

        public static OpenTransaction Map(AzureOpenTransaction src)
        {
            if (src == null)
            {
                return null;
            }

            return new OpenTransaction
            {
                cbReceiptReference = src.cbReceiptReference,
                StartMoment = src.StartMoment,
                StartTransactionSignatureBase64 = src.StartTransactionSignatureBase64,
                TransactionNumber = Convert.ToInt64(src.TransactionNumber)
            };
        }

        public static AzureOpenTransaction Map(OpenTransaction src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureOpenTransaction
            {
                PartitionKey = GetHashString(src.StartMoment.Ticks),
                RowKey = src.cbReceiptReference,
                cbReceiptReference = src.cbReceiptReference,
                StartMoment = src.StartMoment,
                StartTransactionSignatureBase64 = src.StartTransactionSignatureBase64,
                TransactionNumber = src.TransactionNumber.ToString()
            };
        }

        public static AzureAccountMasterData Map(AccountMasterData src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureAccountMasterData
            {
                PartitionKey = src.AccountId.ToString(),
                RowKey = src.AccountId.ToString(),
                AccountId = src.AccountId,
                AccountName = src.AccountName,
                Street = src.Street,
                Zip = src.Zip,
                City = src.City,
                Country = src.Country,
                TaxId = src.TaxId,
                VatId = src.VatId
            };
        }

        public static AccountMasterData Map(AzureAccountMasterData src)
        {
            if (src == null)
            {
                return null;
            }

            return new AccountMasterData
            {
                AccountId = src.AccountId,
                AccountName = src.AccountName,
                Street = src.Street,
                Zip = src.Zip,
                City = src.City,
                Country = src.Country,
                TaxId = src.TaxId,
                VatId = src.VatId
            };
        }

        public static AzureOutletMasterData Map(OutletMasterData src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureOutletMasterData
            {
                PartitionKey = src.OutletId.ToString(),
                RowKey = src.OutletId.ToString(),
                OutletId = src.OutletId,
                OutletName = src.OutletName,
                Street = src.Street,
                Zip = src.Zip,
                City = src.City,
                Country = src.Country,
                VatId = src.VatId,
                LocationId = src.LocationId
            };
        }

        public static OutletMasterData Map(AzureOutletMasterData src)
        {
            if (src == null)
            {
                return null;
            }

            return new OutletMasterData
            {
                OutletId = src.OutletId,
                OutletName = src.OutletName,
                Street = src.Street,
                Zip = src.Zip,
                City = src.City,
                Country = src.Country,
                VatId = src.VatId,
                LocationId = src.LocationId
            };
        }

        public static AzureAgencyMasterData Map(AgencyMasterData src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureAgencyMasterData
            {
                PartitionKey = src.AgencyId.ToString(),
                RowKey = src.AgencyId.ToString(),
                Street = src.Street,
                Zip = src.Zip,
                City = src.City,
                Country = src.Country,
                VatId = src.VatId,
                AgencyId = src.AgencyId,
                Name = src.Name,
                TaxId = src.TaxId
            };
        }

        public static AgencyMasterData Map(AzureAgencyMasterData src)
        {
            if (src == null)
            {
                return null;
            }

            return new AgencyMasterData
            {
                Street = src.Street,
                Zip = src.Zip,
                City = src.City,
                Country = src.Country,
                VatId = src.VatId,
                AgencyId = src.AgencyId,
                Name = src.Name,
                TaxId = src.TaxId
            };
        }

        public static AzurePosSystemMasterData Map(PosSystemMasterData src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzurePosSystemMasterData
            {
                PartitionKey = src.PosSystemId.ToString(),
                RowKey = src.PosSystemId.ToString(),
                BaseCurrency = src.BaseCurrency,
                Brand = src.Brand,
                Model = src.Model,
                PosSystemId = src.PosSystemId,
                SoftwareVersion = src.SoftwareVersion,
                Type = src.Type
            };
        }

        public static PosSystemMasterData Map(AzurePosSystemMasterData src)
        {
            if (src == null)
            {
                return null;
            }

            return new PosSystemMasterData
            {
                BaseCurrency = src.BaseCurrency,
                Brand = src.Brand,
                Model = src.Model,
                PosSystemId = src.PosSystemId,
                SoftwareVersion = src.SoftwareVersion,
                Type = src.Type
            };
        }

        public static string GetHashString(long value)
        {
            var descRowBytes = BitConverter.GetBytes(long.MaxValue - value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(descRowBytes);
            }
            return BitConverter.ToString(descRowBytes);
        }
    }
}
