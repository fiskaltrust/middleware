using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudAndroid.Management
{
    public class SwissbitCloudManagementClient : ISwissbitCloudManagementClient
    {
        public Task<(List<string> clientIds, string remoteCspVersion, string status)> GetTseDetailsAsync(string fccId, string fccSecret, string tssId) => throw new System.NotImplementedException();
        public Task RegisterClientAsync(string fccId, string fccSecret, string clientId) => throw new System.NotImplementedException();
    }
}