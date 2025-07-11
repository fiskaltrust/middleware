using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0.MasterData;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.v2.MasterData;

public class MasterDataService : IMasterDataService
{
    public const string CONFIG_KEY = "init_masterData";

    private readonly Dictionary<string, object> _configuration;
    private readonly Lazy<Task<IMasterDataRepository<AccountMasterData>>> _accountMasterDataRepository;
    private readonly Lazy<Task<IMasterDataRepository<OutletMasterData>>> _outletMasterDataRepository;
    private readonly Lazy<Task<IMasterDataRepository<PosSystemMasterData>>> _posSystemMasterDataRepository;
    private readonly Lazy<Task<IMasterDataRepository<AgencyMasterData>>> _agencyMasterDataRepository;

    public MasterDataService(Dictionary<string, object> configuration, IStorageProvider storageProvider)
    {
        _configuration = configuration;
        _accountMasterDataRepository = storageProvider.AccountMasterDataRepository;
        _outletMasterDataRepository = storageProvider.OutletMasterDataRepository;
        _posSystemMasterDataRepository = storageProvider.PosSystemMasterDataRepository;
        _agencyMasterDataRepository = storageProvider.AgencyMasterDataRepository;
    }

    public async Task<MasterDataConfiguration> GetCurrentDataAsync()
    {
        return new MasterDataConfiguration
        {
            Account = (await (await _accountMasterDataRepository.Value).GetAsync().ConfigureAwait(false))?.FirstOrDefault(),
            Outlet = (await (await _outletMasterDataRepository.Value).GetAsync().ConfigureAwait(false))?.FirstOrDefault(),
            Agencies = await (await _agencyMasterDataRepository.Value).GetAsync().ConfigureAwait(false),
            PosSystems = await (await _posSystemMasterDataRepository.Value).GetAsync().ConfigureAwait(false)
        };
    }

    public async Task<bool> HasDataChangedAsync()
    {
        if (!_configuration.ContainsKey(CONFIG_KEY) || string.IsNullOrEmpty(_configuration[CONFIG_KEY]?.ToString()))
        {
            return false;
        }

        var currentJson = JsonConvert.SerializeObject(await GetCurrentDataAsync().ConfigureAwait(false));
        var nextJson = JsonConvert.SerializeObject(JsonConvert.DeserializeObject<MasterDataConfiguration>(_configuration[CONFIG_KEY].ToString()!));

        return currentJson != nextJson;
    }

    public MasterDataConfiguration? GetFromConfig()
    {
        if (!_configuration.ContainsKey(CONFIG_KEY) || string.IsNullOrEmpty(_configuration[CONFIG_KEY]?.ToString()))
        {
            return null;
        }

        return _configuration.ContainsKey(CONFIG_KEY)
            ? JsonConvert.DeserializeObject<MasterDataConfiguration>(_configuration[CONFIG_KEY].ToString()!)
            : null;
    }

    public async Task PersistConfigurationAsync()
    {
        if (!_configuration.ContainsKey(CONFIG_KEY) || string.IsNullOrEmpty(_configuration[CONFIG_KEY]?.ToString()))
        {
            return;
        }

        var masterdata = JsonConvert.DeserializeObject<MasterDataConfiguration>(_configuration[CONFIG_KEY].ToString()!);
        if (masterdata != null)
        {
            await (await _accountMasterDataRepository.Value).ClearAsync().ConfigureAwait(false);
            await (await _accountMasterDataRepository.Value).CreateAsync(masterdata.Account).ConfigureAwait(false);

            await (await _outletMasterDataRepository.Value).ClearAsync().ConfigureAwait(false);
            await (await _outletMasterDataRepository.Value).CreateAsync(masterdata.Outlet).ConfigureAwait(false);

            await (await _agencyMasterDataRepository.Value).ClearAsync().ConfigureAwait(false);
            foreach (var agency in masterdata.Agencies ?? Enumerable.Empty<AgencyMasterData>())
            {
                await (await _agencyMasterDataRepository.Value).CreateAsync(agency).ConfigureAwait(false);
            }

            await (await _posSystemMasterDataRepository.Value).ClearAsync().ConfigureAwait(false);
            foreach (var posSystem in masterdata.PosSystems ?? Enumerable.Empty<PosSystemMasterData>())
            {
                await (await _posSystemMasterDataRepository.Value).CreateAsync(posSystem).ConfigureAwait(false);
            }
        }
    }
}