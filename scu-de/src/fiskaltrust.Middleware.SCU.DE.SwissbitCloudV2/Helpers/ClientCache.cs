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
        //private readonly ISwissbitCloudV2ApiProvider _swissbitCloudV2;

        public ClientCache() { } //=> _swissbitCloudV2 = swissbitCloudV2;

        public Task<bool> IsClientExistent(string clientId)
        {
            if (!_registeredClients.Any())
            {
                /*
                var clientDto = await _fiskalyApiProvider.GetClientsAsync(tssId);

                foreach (var item in clientDto.Where(x => x.State.Equals("REGISTERED")))
                {
                    if (!_registeredClients.ContainsKey(item.SerialNumber))
                    {
                        _registeredClients.Add(item.SerialNumber, item.Id);
                    }
                }
                */
            }
            return Task.FromResult( _registeredClients.ContainsKey(clientId));
        }

        public List<string> GetClientIds() => _registeredClients.Keys.ToList();

        public void AddClient(string serialNumber, Guid clientId) => _registeredClients.Add(serialNumber, clientId);

        public void RemoveClient(string serialNumber) => _registeredClients.Remove(serialNumber);

        public Guid GetClientId(string serialNumber) => _registeredClients[serialNumber];
    }
}
