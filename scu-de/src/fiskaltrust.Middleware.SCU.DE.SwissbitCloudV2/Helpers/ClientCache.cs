using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Services;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Helpers
{
    public class ClientCache
    {
        private readonly Dictionary<string, Guid> _registeredClients = new Dictionary<string, Guid>();
        private readonly ISwissbitCloudV2ApiProvider _swissbitCloudV2;

        public ClientCache(ISwissbitCloudV2ApiProvider swissbitCloudV2) => _swissbitCloudV2 = swissbitCloudV2;

        public async Task<bool> IsClientExistent(string clientId)
        {
            if (!_registeredClients.Any())
            {
                
                var clients = await _swissbitCloudV2.GetClientsAsync();

                foreach (var client in clients)
                {
                    if (!_registeredClients.ContainsKey(client))
                    {
                        _registeredClients.Add(client, Guid.NewGuid());
                    }
                }
            }
            return _registeredClients.ContainsKey(clientId);
        }

        public List<string> GetClientIds() => _registeredClients.Keys.ToList();

        public void AddClient(string serialNumber, Guid clientId) => _registeredClients.Add(serialNumber, clientId);

        public void RemoveClient(string serialNumber) => _registeredClients.Remove(serialNumber);

        public Guid GetClientId(string serialNumber) => _registeredClients[serialNumber];
    }
}
