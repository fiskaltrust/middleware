
using System.Text.Json;
using fiskaltrust.Middleware.Localization.QueueES.ESSSCD;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.Repositories;

namespace fiskaltrust.Middleware.Localization.v2.QueueES.Storage
{
    public class SCUStateProvider : ISCUStateProvider
    {
        private readonly IConfigurationRepository _configurationRepository;
        private readonly Guid _scuId;
        public SCUStateProvider(Guid scuId, IStorageProvider storageProvider)
        {
            _configurationRepository = storageProvider.GetConfigurationRepository();
            _scuId = scuId;
        }

        public async Task<StateData> LoadAsync()
        {
            // saving this in the SCU table is probably stupid since then you can't swap out the scu without breaking the chain and you can use the SCU only on this one queue.
            // maybe this should not be the scu state but more the "chain" state.
            // I can't yet wrap my head around what it means to capsule the fiscalization specifics behind the scu and what the consequences of that are but let's see ^^
            var scu = await _configurationRepository.GetSignaturCreationUnitESAsync(_scuId);
            if (scu?.StateData is not null)
            {
                var stateData = JsonSerializer.Deserialize<StateData>(scu.StateData);
                if (stateData is not null)
                {
                    return stateData;
                }
            }

            return new StateData
            {
                EncadenamientoAlta = null,
                EncadenamientoAnulacion = null
            };
        }


        public async Task SaveAsync(StateData stateData)
        {
            var scu = await _configurationRepository.GetSignaturCreationUnitESAsync(_scuId);
            if (scu?.StateData is null)
            {
                throw new Exception("SCU value must not be null");
            }

            scu.StateData = JsonSerializer.Serialize(stateData);
            await _configurationRepository.InsertOrUpdateSignaturCreationUnitESAsync(scu);
        }
    }
}