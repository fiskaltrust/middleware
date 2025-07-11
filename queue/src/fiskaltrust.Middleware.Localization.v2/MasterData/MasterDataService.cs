using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0.MasterData;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.v2.MasterData;

public class MasterDataService : IMasterDataService
{
    public const string CONFIG_KEY = "init_masterData";

    private readonly Dictionary<string, object> _configuration;
    private readonly AsyncLazy<IMasterDataRepository<AccountMasterData>> _accountMasterDataRepository;
    private readonly AsyncLazy<IMasterDataRepository<OutletMasterData>> _outletMasterDataRepository;
    private readonly AsyncLazy<IMasterDataRepository<PosSystemMasterData>> _posSystemMasterDataRepository;
    private readonly AsyncLazy<IMasterDataRepository<AgencyMasterData>> _agencyMasterDataRepository;

    public MasterDataService(Dictionary<string, object> configuration, IStorageProvider storageProvider)
    {
        _configuration = configuration;
        _accountMasterDataRepository = storageProvider.CreateAccountMasterDataRepository();
        _outletMasterDataRepository = storageProvider.CreateOutletMasterDataRepository();
        _posSystemMasterDataRepository = storageProvider.CreatePosSystemMasterDataRepository();
        _agencyMasterDataRepository = storageProvider.CreateAgencyMasterDataRepository();
    }

    public async Task<MasterDataConfiguration> GetCurrentDataAsync()
    {
        return new MasterDataConfiguration
        {
            Account = (await (await _accountMasterDataRepository).GetAsync().ConfigureAwait(false))?.FirstOrDefault(),
            Outlet = (await (await _outletMasterDataRepository).GetAsync().ConfigureAwait(false))?.FirstOrDefault(),
            Agencies = await (await _agencyMasterDataRepository).GetAsync().ConfigureAwait(false),
            PosSystems = await (await _posSystemMasterDataRepository).GetAsync().ConfigureAwait(false)
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
}