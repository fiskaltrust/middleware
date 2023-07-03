using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Services
{
    public interface IITSSCDProvider
    {
        IITSSCD Instance { get; }

        Task RegisterCurrentScuAsync();
    }
}
