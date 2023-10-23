using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v0;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueAT.Services
{
    public interface IATSSCDProvider
    {
        Task<(ftSignaturCreationUnitAT scu, IATSSCD sscd, int currentIndex)> GetCurrentlyActiveInstanceAsync();
        Task<List<ftSignaturCreationUnitAT>> GetAllInstances();
        int SwitchToNextScu();
        void SwitchToFirstScu();
    }
}
