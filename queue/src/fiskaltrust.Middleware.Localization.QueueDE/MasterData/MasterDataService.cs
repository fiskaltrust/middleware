using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE.MasterData
{
    public class MasterDataService : IMasterDataService
    {
        private const string CONFIG_KEY = "init_masterData";

        private readonly Dictionary<string, object> _configuration;
        private readonly IMasterDataRepository<AccountMasterData> _accountMasterDataRepository;
        private readonly IMasterDataRepository<OutletMasterData> _outletMasterDataRepository;
        private readonly IMasterDataRepository<PosSystemMasterData> _posSystemMasterDataRepository;
        private readonly IMasterDataRepository<AgencyMasterData> _agencyMasterDataRepository;

        public MasterDataService(
            MiddlewareConfiguration middlewareConfig,
            IMasterDataRepository<AccountMasterData> accountMasterDataRepository,
            IMasterDataRepository<OutletMasterData> outletMasterDataRepository,
            IMasterDataRepository<PosSystemMasterData> posSystemMasterDataRepository,
            IMasterDataRepository<AgencyMasterData> agencyMasterDataRepository)
        {
            _configuration = middlewareConfig.Configuration;
            _accountMasterDataRepository = accountMasterDataRepository;
            _outletMasterDataRepository = outletMasterDataRepository;
            _posSystemMasterDataRepository = posSystemMasterDataRepository;
            _agencyMasterDataRepository = agencyMasterDataRepository;
        }

        public async Task<MasterDataConfiguration> GetCurrentDataAsync()
        {
            return new MasterDataConfiguration
            {
                Account = (await _accountMasterDataRepository.GetAsync().ConfigureAwait(false))?.FirstOrDefault(),
                Outlet = (await _outletMasterDataRepository.GetAsync().ConfigureAwait(false))?.FirstOrDefault(),
                Agencies = await _agencyMasterDataRepository.GetAsync().ConfigureAwait(false),
                PosSystems = await _posSystemMasterDataRepository.GetAsync().ConfigureAwait(false)
            };
        }

        public async Task<bool> HasDataChangedAsync()
        {
            if (!_configuration.ContainsKey(CONFIG_KEY))
            {
                return false;
            }

            var currentJson = JsonConvert.SerializeObject(await GetCurrentDataAsync().ConfigureAwait(false));
            var nextJson = JsonConvert.SerializeObject(JsonConvert.DeserializeObject<MasterDataConfiguration>(_configuration[CONFIG_KEY].ToString()));

            return currentJson != nextJson;
        }

        public MasterDataConfiguration GetFromConfig()
        {
            return _configuration.ContainsKey(CONFIG_KEY)
                ? JsonConvert.DeserializeObject<MasterDataConfiguration>(_configuration[CONFIG_KEY].ToString())
                : null;
        }

        public async Task PersistConfigurationAsync()
        {
            if (!_configuration.ContainsKey(CONFIG_KEY))
            {
                return;
            }
            var masterdata = JsonConvert.DeserializeObject<MasterDataConfiguration>(_configuration[CONFIG_KEY].ToString());
            if (masterdata != null)
            {
                await _accountMasterDataRepository.ClearAsync().ConfigureAwait(false);
                await _accountMasterDataRepository.CreateAsync(masterdata.Account).ConfigureAwait(false);

                await _outletMasterDataRepository.ClearAsync().ConfigureAwait(false);
                await _outletMasterDataRepository.CreateAsync(masterdata.Outlet).ConfigureAwait(false);

                await _agencyMasterDataRepository.ClearAsync().ConfigureAwait(false);
                foreach (var agency in masterdata.Agencies ?? Enumerable.Empty<AgencyMasterData>())
                {
                    await _agencyMasterDataRepository.CreateAsync(agency).ConfigureAwait(false);
                }

                await _posSystemMasterDataRepository.ClearAsync().ConfigureAwait(false);
                foreach (var posSystem in masterdata.PosSystems ?? Enumerable.Empty<PosSystemMasterData>())
                {
                    await _posSystemMasterDataRepository.CreateAsync(posSystem).ConfigureAwait(false);
                }
            }
        }
    }
}
