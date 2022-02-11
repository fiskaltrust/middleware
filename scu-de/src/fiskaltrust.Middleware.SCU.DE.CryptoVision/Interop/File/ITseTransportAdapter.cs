using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File
{
    public interface ITseTransportAdapter : IDisposable
    {
        Task<List<ITseData>> ExecuteAsync(ITseCommand tseCommand);

        public void OpenFile();

        public void CloseFile();

        public void ReopenFile();
    }
}
