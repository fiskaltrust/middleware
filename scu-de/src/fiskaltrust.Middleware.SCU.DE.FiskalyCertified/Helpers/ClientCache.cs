using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Services;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers
{
    public class ClientCache
    {
        private readonly Dictionary<string, Guid> _registeredClients = new Dictionary<string, Guid>();
        private readonly IFiskalyApiProvider _fiskalyApiProvider;

        public ClientCache(IFiskalyApiProvider fiskalyApiProvider) => _fiskalyApiProvider = fiskalyApiProvider;

        public async Task<bool> IsClientExistent(Guid tssId, string clientId)
        {
            if (!_registeredClients.Any())
            {
                var clientDto = await _fiskalyApiProvider.GetClientsAsync(tssId);

                foreach (var item in clientDto.Where(x => x.State.Equals("REGISTERED")))
                {
                    if (!_registeredClients.ContainsKey(item.SerialNumber))
                    {
                        _registeredClients.Add(item.SerialNumber, item.Id);
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
