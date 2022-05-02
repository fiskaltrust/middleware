using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.me;

namespace fiskaltrust.Middleware.Localization.QueueME.Services
{
    public interface IMESSCDProvider
    {
        IMESSCD Instance { get; }

        Task RegisterCurrentScuAsync();
    }
}
