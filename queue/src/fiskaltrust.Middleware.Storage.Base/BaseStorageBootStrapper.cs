using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
                CashBox = ParseParameter<ftCashBox>(configuration, "init_ftCashBox"),
                SignaturCreationUnitsAT = ParseParameter<List<ftSignaturCreationUnitAT>>(configuration, "init_ftSignaturCreationUnitAT") ?? new List<ftSignaturCreationUnitAT>(),
                SignaturCreationUnitsDE = ParseParameter<List<ftSignaturCreationUnitDE>>(configuration, "init_ftSignaturCreationUnitDE") ?? new List<ftSignaturCreationUnitDE>(),
                SignaturCreationUnitsFR = ParseParameter<List<ftSignaturCreationUnitFR>>(configuration, "init_ftSignaturCreationUnitFR") ?? new List<ftSignaturCreationUnitFR>(),
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

        public async Task PersistConfigurationAsync(StorageBaseInitConfiguration config, IConfigurationRepository configurationRepository, ILogger<IMiddlewareBootstrapper> logger)
        {
            var dbCashBox = await configurationRepository.GetCashBoxAsync(config.CashBox.ftCashBoxId).ConfigureAwait(false);
            var enforceUpdateUserDefinedConfig = dbCashBox != null && dbCashBox.TimeStamp < config.CashBox.TimeStamp;

            await InitCashBoxAsync(config.CashBox, configurationRepository).ConfigureAwait(false);
            await InitFtQueueAsync(config.Queues, configurationRepository).ConfigureAwait(false);
            await InitQueueATAsync(config.QueuesAT, configurationRepository).ConfigureAwait(false);
            await InitQueueDEAsync(config.QueuesDE, configurationRepository, logger).ConfigureAwait(false);
            await InitQueueFRAsync(config.QueuesFR, configurationRepository).ConfigureAwait(false);
            await InitSignaturCreationUnitATAsync(config.SignaturCreationUnitsAT, configurationRepository).ConfigureAwait(false);
            await InitSignaturCreationUnitFRAsync(config.SignaturCreationUnitsFR, configurationRepository).ConfigureAwait(false);
            await InitSignaturCreationUnitDEAsync(config.SignaturCreationUnitsDE, configurationRepository, enforceUpdateUserDefinedConfig).ConfigureAwait(false);
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
    }
}
