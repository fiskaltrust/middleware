using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Localization.QueueES.Externals.ifpos;

namespace fiskaltrust.Middleware.Localization.QueueES.Services
{
    public interface IESSSCDProvider
    {
        IESSSCD Instance { get; }
    }

    // TODO this needs to be removed
    
}
