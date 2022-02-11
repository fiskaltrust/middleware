using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;

namespace fiskaltrust.Middleware.Localization.QueueDE.Services
{
    public interface IDESSCDProvider
    {
        IDESSCD Instance { get; }

        Task RegisterCurrentScuAsync();
    }
}
