using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.v2.MasterData;

public interface IMasterDataService
{
    Task<MasterDataConfiguration> GetCurrentDataAsync();
    Task PersistConfigurationAsync();
    Task<bool> HasDataChangedAsync();
    MasterDataConfiguration? GetFromConfig();
}
