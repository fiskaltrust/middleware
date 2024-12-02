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
    private readonly IMasterDataRepository<AccountMasterData> _accountMasterDataRepository;
    private readonly IMasterDataRepository<OutletMasterData> _outletMasterDataRepository;
    private readonly IMasterDataRepository<PosSystemMasterData> _posSystemMasterDataRepository;
    private readonly IMasterDataRepository<AgencyMasterData> _agencyMasterDataRepository;

    public MasterDataService(Dictionary<string, object> configuration, IStorageProvider storageProvider)
    {
        _configuration = configuration;
        _accountMasterDataRepository = storageProvider.GetAccountMasterDataRepository();
        _outletMasterDataRepository = storageProvider.GetOutletMasterDataRepository();
        _posSystemMasterDataRepository = storageProvider.GetPosSystemMasterDataRepository();
        _agencyMasterDataRepository = storageProvider.GetAgencyMasterDataRepository();
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