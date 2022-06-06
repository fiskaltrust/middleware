using System.Threading.Tasks;
using fiskaltrust.ifPOS.v0;

namespace fiskaltrust.Middleware.Localization.QueueDE.Services
{
    public interface IATSSCDProvider
    {
        Task<IATSSCD> GetCurrentlyActiveInstanceAsync();
        void SwitchToNextScu();
    }
}
