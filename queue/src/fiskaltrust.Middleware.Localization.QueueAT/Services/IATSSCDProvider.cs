using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueAT.Services
{
    public interface IATSSCDProvider
    {
        Task<int> GetCurrentlyActiveInstanceIndexAsync();
        Task<List<(ftSignaturCreationUnitAT scu, IATSSCD sscd)>> GetAllInstances();
        int SwitchToNextScu();
        void SwitchToFirstScu();
    }
}
