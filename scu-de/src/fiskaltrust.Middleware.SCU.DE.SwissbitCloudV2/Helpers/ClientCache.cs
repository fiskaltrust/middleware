using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Services;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Helpers
{
    public class ClientCache
    {
        private readonly List<string> _registeredClients = new List<string>();
        private readonly ISwissbitCloudV2ApiProvider _swissbitCloudV2;

        public ClientCache(ISwissbitCloudV2ApiProvider swissbitCloudV2) => _swissbitCloudV2 = swissbitCloudV2;

        public async Task<bool> IsClientExistent(string clientId)
        {
            if (!_registeredClients.Any())
            {
                
                var clients = await _swissbitCloudV2.GetClientsAsync();

                foreach (var client in clients)
                {
                    if (!_registeredClients.Contains(client))
                    {
                        _registeredClients.Add(client);
                    }
                }
            }
            return _registeredClients.Contains(clientId);
        }

        public List<string> GetClientIds() => _registeredClients;

        public void AddClient(string clientId) => _registeredClients.Add(clientId);

        public void RemoveClient(string clientId) => _registeredClients.Remove(clientId);
    }
}
