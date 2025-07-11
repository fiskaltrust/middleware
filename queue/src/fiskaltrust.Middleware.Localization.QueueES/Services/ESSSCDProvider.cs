using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Localization.QueueES.Models;
using fiskaltrust.Middleware.Localization.QueueES.Services.Interface;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueES.Services;

public class ESSSCDProvider : IESSSCDProvider
{
    private IESSSCD? _instance { get; set; } = null;
    private readonly Func<Task<IESSSCD>> _initializer;

    public ESSSCDProvider(IClientFactory<IESSSCD> clientFactory, IStorageProvider storageProvider, Guid queueId, QueueESConfiguration queueESConfiguration)
    {
        _initializer = async () =>
        {
            if (_instance is null)
            {
                var configurationRepository = await storageProvider.ConfigurationRepository.Value;
                var queue = await configurationRepository.GetQueueESAsync(queueId);
                var scu = await configurationRepository.GetSignaturCreationUnitESAsync(queue.ftSignaturCreationUnitESId);
                _instance = clientFactory.CreateClient(new ClientConfiguration
                {
                    Timeout = queueESConfiguration.ScuTimeoutMs.HasValue ? TimeSpan.FromMilliseconds(queueESConfiguration.ScuTimeoutMs.Value) : TimeSpan.FromSeconds(15),
                    RetryCount = queueESConfiguration.ScuMaxRetries.HasValue ? queueESConfiguration.ScuMaxRetries.Value : null,
                    Url = scu.Url
                });
            }

            return _instance;
        };
    }

    public Task<IESSSCD> GetAsync() => _initializer();
}