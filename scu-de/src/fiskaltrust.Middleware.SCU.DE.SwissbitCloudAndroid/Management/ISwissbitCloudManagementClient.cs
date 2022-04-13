using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudAndroid.Management
{
    public interface ISwissbitCloudManagementClient
    {
        Task<(List<string> clientIds, string remoteCspVersion, string status)> GetTseDetailsAsync(string fccId, string fccSecret, string tssId);
        Task RegisterClientAsync(string fccId, string fccSecret, string clientId);
    }
}