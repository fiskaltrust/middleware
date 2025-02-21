using System.Threading.Tasks;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.storage.V0
{
    public interface IReadOnlyMasterDataConfiguration
    {
        Task<MasterDataConfiguration> GetMasterDataConfiguration();
    }
}
