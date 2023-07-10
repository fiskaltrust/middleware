using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Contracts.Interfaces
{
    public interface ISigningDevice
    {
        public Task<bool> IsSigningDeviceAvailable();
    }
}
