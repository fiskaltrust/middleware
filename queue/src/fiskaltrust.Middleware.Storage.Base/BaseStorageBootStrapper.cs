using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ftJournalFRCopyPayload = fiskaltrust.Middleware.Contracts.Models.FR.ftJournalFRCopyPayload;

namespace fiskaltrust.Middleware.Storage.Base
{
    public abstract class BaseStorageBootStrapper
    {
        public StorageBaseInitConfiguration ParseStorageConfiguration(Dictionary<string, object> configuration)
        {
            return new StorageBaseInitConfiguration()
            {
                Queues = ParseParameter<List<ftQueue>>(configuration, "init_ftQueue") ?? new List<ftQueue>(),
                QueuesAT = ParseParameter<List<ftQueueAT>>(configuration, "init_ftQueueAT") ?? new List<ftQueueAT>(),
                QueuesDE = ParseParameter<List<ftQueueDE>>(configuration, "init_ftQueueDE") ?? new List<ftQueueDE>(),
                QueuesFR = ParseParameter<List<ftQueueFR>>(configuration, "init_ftQueueFR") ?? new List<ftQueueFR>(),
                QueuesME = ParseParameter<List<ftQueueME>>(configuration, "init_ftQueueME") ?? new List<ftQueueME>(),
                QueuesIT = ParseParameter<List<ftQueueIT>>(configuration, "init_ftQueueIT") ?? new List<ftQueueIT>(),
                CashBox = ParseParameter<ftCashBox>(configuration, "init_ftCashBox"),
                SignaturCreationUnitsAT = ParseParameter<List<ftSignaturCreationUnitAT>>(configuration, "init_ftSignaturCreationUnitAT") ?? new List<ftSignaturCreationUnitAT>(),
                SignaturCreationUnitsDE = ParseParameter<List<ftSignaturCreationUnitDE>>(configuration, "init_ftSignaturCreationUnitDE") ?? new List<ftSignaturCreationUnitDE>(),
                SignaturCreationUnitsFR = ParseParameter<List<ftSignaturCreationUnitFR>>(configuration, "init_ftSignaturCreationUnitFR") ?? new List<ftSignaturCreationUnitFR>(),
                SignaturCreationUnitsME = ParseParameter<List<ftSignaturCreationUnitME>>(configuration, "init_ftSignaturCreationUnitME") ?? new List<ftSignaturCreationUnitME>(),
                SignaturCreationUnitsIT = ParseParameter<List<ftSignaturCreationUnitIT>>(configuration, "init_ftSignaturCreationUnitIT") ?? new List<ftSignaturCreationUnitIT>(),
                MasterData = ParseParameter<MasterDataConfiguration>(configuration, "init_masterData")
            };
        }

        public async Task PersistMasterDataAsync(StorageBaseInitConfiguration config, IConfigurationRepository configurationRepository,
            IMasterDataRepository<AccountMasterData> accountMasterDataRepo, IMasterDataRepository<OutletMasterData> outletMasterDataRepo,
            IMasterDataRepository<AgencyMasterData> agencyMasterDataRepo, IMasterDataRepository<PosSystemMasterData> posSystemMasterDataRepo)
        {
            if (!(await configurationRepository.GetCashBoxListAsync().ConfigureAwait(false)).Any())
            {
                if (config.MasterData?.Account != null)
                {
                    await accountMasterDataRepo.CreateAsync(config.MasterData.Account).ConfigureAwait(false);
                }
                if (config.MasterData?.Outlet != null)
                {
                    await outletMasterDataRepo.CreateAsync(config.MasterData.Outlet).ConfigureAwait(false);
                }
                if (config.MasterData?.Agencies != null)
                {
                    foreach (var agency in config.MasterData.Agencies)
                    {
                        await agencyMasterDataRepo.CreateAsync(agency).ConfigureAwait(false);
                    }
                }
                if (config.MasterData?.PosSystems != null)
                {
                    foreach (var posSystem in config.MasterData.PosSystems)
                    {
                        await posSystemMasterDataRepo.CreateAsync(posSystem).ConfigureAwait(false);
                    }
                }
            }
        }

        public enum Migrations
        {
            JournalFRCopyPayload
        }

        public async Task PerformMigrationInitialization(IEnumerable<Migrations> newlyAppliedMigrations, IJournalFRCopyPayloadRepository journalFRCopyPayloadRepository, IMiddlewareJournalFRRepository journalFRRepository)
        {
            foreach (var migration in newlyAppliedMigrations)
            {
                switch (migration)
                {
                    case Migrations.JournalFRCopyPayload:
                        await PopulateFtJournalFRCopyPayloadTableAsync(journalFRCopyPayloadRepository, journalFRRepository);
                        break;
                }
            }
        }

        protected async Task PopulateFtJournalFRCopyPayloadTableAsync(IJournalFRCopyPayloadRepository journalFRCopyPayloadRepository, IMiddlewareJournalFRRepository journalFRRepository)
        {
            await foreach (var copyJournal in journalFRRepository.GetProcessedCopyReceiptsAsync())
            {
                var jwt = copyJournal.JWT.Split('.');
                var copyPayload = JsonConvert.DeserializeObject<ftJournalFRCopyPayload>(Encoding.UTF8.GetString(Convert.FromBase64String(jwt[1])));

                await journalFRCopyPayloadRepository.InsertAsync(copyPayload);
            }
        }

        public async Task PersistConfigurationAsync(StorageBaseInitConfiguration config, IConfigurationRepository configurationRepository, ILogger<IMiddlewareBootstrapper> logger)
        {
            var dbCashBox = await configurationRepository.GetCashBoxAsync(config.CashBox.ftCashBoxId).ConfigureAwait(false);
            var enforceUpdateUserDefinedConfig = dbCashBox != null && dbCashBox.TimeStamp < config.CashBox.TimeStamp;

            await InitCashBoxAsync(config.CashBox, configurationRepository).ConfigureAwait(false);
            await InitFtQueueAsync(config.Queues, configurationRepository).ConfigureAwait(false);
            await InitQueueATAsync(config.QueuesAT, configurationRepository).ConfigureAwait(false);
            await InitQueueDEAsync(config.QueuesDE, configurationRepository, logger).ConfigureAwait(false);
            await InitQueueFRAsync(config.QueuesFR, configurationRepository).ConfigureAwait(false);
            await InitQueueMEAsync(config.QueuesME, configurationRepository).ConfigureAwait(false);
            await InitQueueITAsync(config.QueuesIT, configurationRepository).ConfigureAwait(false);
            await InitSignaturCreationUnitATAsync(config.SignaturCreationUnitsAT, configurationRepository).ConfigureAwait(false);
            await InitSignaturCreationUnitFRAsync(config.SignaturCreationUnitsFR, configurationRepository).ConfigureAwait(false);
            await InitSignaturCreationUnitDEAsync(config.SignaturCreationUnitsDE, configurationRepository, enforceUpdateUserDefinedConfig).ConfigureAwait(false);
            await InitSignaturCreationUnitMEAsync(config.SignaturCreationUnitsME, configurationRepository).ConfigureAwait(false);
            await InitSignaturCreationUnitITAsync(config.SignaturCreationUnitsIT, configurationRepository, enforceUpdateUserDefinedConfig).ConfigureAwait(false);
        }

        private T ParseParameter<T>(Dictionary<string, object> config, string key) where T : new()
        {
            T parameter;
            if (config.ContainsKey(key))
            {
                parameter = JsonConvert.DeserializeObject<T>(config[key].ToString());
            }
            else
            {
                return default; // Sometimes we don't get that data. We just expect it to be empty then.
            }
            return parameter;
        }

        private async Task InitCashBoxAsync(ftCashBox cashBox, IConfigurationRepository configurationRepository)
        {
            var db_cb = await configurationRepository.GetCashBoxAsync(cashBox.ftCashBoxId).ConfigureAwait(false);
            if (db_cb == null || db_cb.TimeStamp < cashBox.TimeStamp)
            {
                await configurationRepository.InsertOrUpdateCashBoxAsync(cashBox).ConfigureAwait(false);
            }
        }

        private async Task InitFtQueueAsync(List<ftQueue> queue, IConfigurationRepository configurationRepository)
        {
            foreach (var item in queue)
            {
                var db_q = await configurationRepository.GetQueueAsync(item.ftQueueId).ConfigureAwait(false);
                if (db_q == null)
                {
                    await configurationRepository.InsertOrUpdateQueueAsync(item).ConfigureAwait(false);
                }
                else if (db_q.TimeStamp < item.TimeStamp)
                {
                    var changed = false;
                    if (string.IsNullOrEmpty(db_q.CountryCode) && !string.IsNullOrEmpty(item.CountryCode))
                    {
                        changed = true;
                        db_q.CountryCode = item.CountryCode;
                    }

                    if (db_q.StopMoment == null && item.StopMoment.HasValue)
                    {
                        changed = true;
                        db_q.StopMoment = item.StopMoment;
                    }

                    if (db_q.ftCashBoxId != item.ftCashBoxId)
                    {
                        changed = true;
                        db_q.ftCashBoxId = item.ftCashBoxId;
                    }

                    if (changed)
                    {
                        db_q.TimeStamp = item.TimeStamp;
                        await configurationRepository.InsertOrUpdateQueueAsync(db_q).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task InitQueueATAsync(List<ftQueueAT> queueAT, IConfigurationRepository configurationRepository)
        {
            foreach (var item in queueAT)
            {
                var db_q = await configurationRepository.GetQueueATAsync(item.ftQueueATId).ConfigureAwait(false);
                if (db_q == null)
                {
                    await configurationRepository.InsertOrUpdateQueueATAsync(item).ConfigureAwait(false);
                }
                else if (db_q.TimeStamp < item.TimeStamp)
                {
                    var changed = false;
                    if (string.IsNullOrEmpty(db_q.CashBoxIdentification) && !string.IsNullOrEmpty(item.CashBoxIdentification))
                    {
                        changed = true;
                        db_q.CashBoxIdentification = item.CashBoxIdentification;
                    }

                    if (string.IsNullOrEmpty(db_q.EncryptionKeyBase64) && !string.IsNullOrEmpty(item.EncryptionKeyBase64))
                    {
                        changed = true;
                        db_q.EncryptionKeyBase64 = item.EncryptionKeyBase64;
                    }

                    if (string.IsNullOrEmpty(db_q.ClosedSystemKind) && !string.IsNullOrEmpty(item.ClosedSystemKind))
                    {
                        changed = true;
                        db_q.ClosedSystemKind = item.ClosedSystemKind;
                        db_q.ClosedSystemNote = item.ClosedSystemNote;
                        db_q.ClosedSystemValue = item.ClosedSystemValue;
                    }

                    if (changed)
                    {
                        db_q.TimeStamp = item.TimeStamp;
                        await configurationRepository.InsertOrUpdateQueueATAsync(db_q).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task InitQueueDEAsync(List<ftQueueDE> queuesDE, IConfigurationRepository configurationRepository, ILogger<IMiddlewareBootstrapper> logger)
        {
            foreach (var item in queuesDE)
            {
                var dbQueueDe = await configurationRepository.GetQueueDEAsync(item.ftQueueDEId).ConfigureAwait(false);
                if (dbQueueDe == null)
                {
                    await configurationRepository.InsertOrUpdateQueueDEAsync(item).ConfigureAwait(false);
                }
                else if (dbQueueDe.TimeStamp < item.TimeStamp)
                {
                    var dbQueue = await configurationRepository.GetQueueAsync(item.ftQueueDEId).ConfigureAwait(false);
                    if (dbQueueDe.ftSignaturCreationUnitDEId != item.ftSignaturCreationUnitDEId)
                    {
                        if (!dbQueue.StartMoment.HasValue)
                        {
                            dbQueueDe.ftSignaturCreationUnitDEId = item.ftSignaturCreationUnitDEId;
                            dbQueueDe.TimeStamp = item.TimeStamp;
                            await configurationRepository.InsertOrUpdateQueueDEAsync(dbQueueDe).ConfigureAwait(false);
                        }
                        else
                        {
                            logger.LogError("An attempt was made to alter the configuration of an already started German queue. This is prohibited due to legal restrictions, and might lead to unpredictable errors. Please create a new Queue when a new SCU is needed.");
                        }
                    }
                }
            }
        }

        private async Task InitQueueMEAsync(List<ftQueueME> queuesME, IConfigurationRepository configurationRepository)
        {
            foreach (var item in queuesME)
            {
                var db_q = await configurationRepository.GetQueueMEAsync(item.ftQueueMEId).ConfigureAwait(false);
                if (db_q == null)
                {
                    await configurationRepository.InsertOrUpdateQueueMEAsync(item).ConfigureAwait(false);
                }
            }
        }

        private async Task InitQueueITAsync(List<ftQueueIT> queuesIT, IConfigurationRepository configurationRepository)
        {
            foreach (var item in queuesIT)
            {
                var db_q = await configurationRepository.GetQueueITAsync(item.ftQueueITId).ConfigureAwait(false);
                if (db_q == null)
                {
                    await configurationRepository.InsertOrUpdateQueueITAsync(item).ConfigureAwait(false);
                }
            }
        }

        private async Task InitQueueFRAsync(List<ftQueueFR> queuesFR, IConfigurationRepository configurationRepository)
        {
            foreach (var item in queuesFR)
            {
                var db_q = await configurationRepository.GetQueueFRAsync(item.ftQueueFRId).ConfigureAwait(false);
                if (db_q == null)
                {
                    await configurationRepository.InsertOrUpdateQueueFRAsync(item).ConfigureAwait(false);
                }
            }
        }

        private async Task InitSignaturCreationUnitATAsync(List<ftSignaturCreationUnitAT> signaturCreationUnitsAT, IConfigurationRepository configurationRepository)
        {
            foreach (var item in signaturCreationUnitsAT)
            {
                var db_scu = await configurationRepository.GetSignaturCreationUnitATAsync(item.ftSignaturCreationUnitATId).ConfigureAwait(false);
                if (db_scu == null)
                {
                    await configurationRepository.InsertOrUpdateSignaturCreationUnitATAsync(item).ConfigureAwait(false);
                }
                else if (db_scu.TimeStamp < item.TimeStamp)
                {
                    var changed = false;
                    if (string.IsNullOrEmpty(db_scu.ZDA) && !string.IsNullOrEmpty(item.ZDA))
                    {
                        changed = true;
                        db_scu.ZDA = item.ZDA;
                    }

                    if (string.IsNullOrEmpty(db_scu.SN) && !string.IsNullOrEmpty(item.SN))
                    {
                        changed = true;
                        db_scu.SN = item.SN;
                    }

                    if (string.IsNullOrEmpty(db_scu.CertificateBase64) && !string.IsNullOrEmpty(item.CertificateBase64))
                    {
                        changed = true;
                        db_scu.CertificateBase64 = item.CertificateBase64;
                    }

                    if (!string.IsNullOrEmpty(item.Url) && db_scu.Url != item.Url)
                    {
                        changed = true;
                        db_scu.Url = item.Url;
                    }

                    if (db_scu.Mode != item.Mode)
                    {
                        changed = true;
                        db_scu.Mode = item.Mode;
                    }

                    if (changed)
                    {
                        db_scu.TimeStamp = item.TimeStamp;
                        await configurationRepository.InsertOrUpdateSignaturCreationUnitATAsync(db_scu).ConfigureAwait(false);
                    }
                }
            }

            foreach (var item in (await configurationRepository.GetSignaturCreationUnitATListAsync().ConfigureAwait(false)).Where(scu => (scu.Mode & 0xff) < 99))
            {
                if (!signaturCreationUnitsAT.Select(i => i.ftSignaturCreationUnitATId).Contains(item.ftSignaturCreationUnitATId))
                {
                    item.Mode |= 0xff;
                    await configurationRepository.InsertOrUpdateSignaturCreationUnitATAsync(item).ConfigureAwait(false);
                }
            }
        }

        private async Task InitSignaturCreationUnitDEAsync(List<ftSignaturCreationUnitDE> signaturCreationUnitsDE, IConfigurationRepository configurationRepository, bool enforceUpdateUserDefinedConfig)
        {
            foreach (var item in signaturCreationUnitsDE)
            {
                var db_scu = await configurationRepository.GetSignaturCreationUnitDEAsync(item.ftSignaturCreationUnitDEId).ConfigureAwait(false);
                if (db_scu == null)
                {
                    await configurationRepository.InsertOrUpdateSignaturCreationUnitDEAsync(item).ConfigureAwait(false);
                }
                else if (db_scu.TimeStamp < item.TimeStamp || enforceUpdateUserDefinedConfig)
                {
                    var changed = false;
                    if (!string.IsNullOrEmpty(item.Url) && db_scu.Url != item.Url)
                    {
                        changed = true;
                        db_scu.Url = item.Url;
                    }
                    if (db_scu.Mode != item.Mode)
                    {
                        changed = true;
                        db_scu.Mode = item.Mode;
                    }
                    if (db_scu.ModeConfigurationJson != item.ModeConfigurationJson)
                    {
                        changed = true;
                        db_scu.ModeConfigurationJson = item.ModeConfigurationJson;
                    }

                    if (changed)
                    {
                        db_scu.TimeStamp = item.TimeStamp;
                        await configurationRepository.InsertOrUpdateSignaturCreationUnitDEAsync(db_scu).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task InitSignaturCreationUnitFRAsync(List<ftSignaturCreationUnitFR> signaturCreationUnitsFR, IConfigurationRepository configurationRepository)
        {
            foreach (var item in signaturCreationUnitsFR)
            {
                var scu = await configurationRepository.GetSignaturCreationUnitFRAsync(item.ftSignaturCreationUnitFRId).ConfigureAwait(false);
                if (scu == null)
                {
                    await configurationRepository.InsertOrUpdateSignaturCreationUnitFRAsync(item).ConfigureAwait(false);
                }
            }
        }

        private async Task InitSignaturCreationUnitMEAsync(List<ftSignaturCreationUnitME> signaturCreationUnitsME, IConfigurationRepository configurationRepository)
        {
            foreach (var item in signaturCreationUnitsME)
            {
                var scu = await configurationRepository.GetSignaturCreationUnitMEAsync(item.ftSignaturCreationUnitMEId).ConfigureAwait(false);
                if (scu == null)
                {
                    await configurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(item).ConfigureAwait(false);
                }
            }
        }

        private async Task InitSignaturCreationUnitITAsync(List<ftSignaturCreationUnitIT> signaturCreationUnitsIT, IConfigurationRepository configurationRepository, bool enforceUpdateUserDefinedConfig)
        {
            foreach (var item in signaturCreationUnitsIT)
            {
                var db_scu = await configurationRepository.GetSignaturCreationUnitITAsync(item.ftSignaturCreationUnitITId).ConfigureAwait(false);
                if (db_scu == null)
                {
                    await configurationRepository.InsertOrUpdateSignaturCreationUnitITAsync(item).ConfigureAwait(false);
                }
                else if (db_scu.TimeStamp < item.TimeStamp || enforceUpdateUserDefinedConfig)
                {
                    var changed = false;
                    if (!string.IsNullOrEmpty(item.Url) && db_scu.Url != item.Url)
                    {
                        changed = true;
                        db_scu.Url = item.Url;
                    }
 
                    if (changed)
                    {
                        db_scu.TimeStamp = item.TimeStamp;
                        await configurationRepository.InsertOrUpdateSignaturCreationUnitITAsync(db_scu).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
