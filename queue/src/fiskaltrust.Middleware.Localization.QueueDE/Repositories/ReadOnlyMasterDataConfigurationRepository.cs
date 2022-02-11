using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueDE.Repositories
{
    public class ReadOnlyMasterDataConfigurationRepository : IReadOnlyMasterDataConfiguration
    {
        private readonly MasterDataConfiguration _masterDataConfiguration;

        public ReadOnlyMasterDataConfigurationRepository(MasterDataConfiguration masterDataConfiguration)
        {
            _masterDataConfiguration = masterDataConfiguration;
        }

        Task<MasterDataConfiguration> IReadOnlyMasterDataConfiguration.GetMasterDataConfiguration() => Task.FromResult(_masterDataConfiguration);
    }
}
