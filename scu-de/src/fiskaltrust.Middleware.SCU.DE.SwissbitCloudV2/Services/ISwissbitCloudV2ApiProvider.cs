using System;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Services
{
    public interface ISwissbitCloudV2ApiProvider
    {
        Task CreateClientAsync(ClientDto client);
    }
}
