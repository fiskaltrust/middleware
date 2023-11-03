using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class ITSSCDProvider : IITSSCDProvider
    {
        private readonly ILogger<ITSSCDProvider> _logger;
        private readonly IClientFactory<IITSSCD> _clientFactory;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly MiddlewareConfiguration _middlewareConfiguration;
        private readonly QueueITConfiguration _queueConfiguration;

        private readonly SemaphoreSlim _semaphoreInstance = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _semaphoreRegister = new SemaphoreSlim(1, 1);

        private IITSSCD _instance;

        public IITSSCD Instance
        {
            get
            {
                try
                {
                    _semaphoreInstance.Wait();
                    if (_instance == null)
                    {
                        RegisterCurrentScuAsync().Wait();
                    }
                    return _instance;
                }
                finally
                {
                    _semaphoreInstance.Release();
                }
            }
        }

        public ITSSCDProvider(ILogger<ITSSCDProvider> logger, IClientFactory<IITSSCD> clientFactory, IConfigurationRepository configurationRepository, MiddlewareConfiguration middlewareConfiguration, QueueITConfiguration queueConfiguration)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _configurationRepository = configurationRepository;
            _middlewareConfiguration = middlewareConfiguration;
            _queueConfiguration = queueConfiguration;
        }

        public async Task RegisterCurrentScuAsync()
        {
            try
            {
                await _semaphoreRegister.WaitAsync().ConfigureAwait(false);
                var queueIT = await _configurationRepository.GetQueueITAsync(_middlewareConfiguration.QueueId).ConfigureAwait(false);

                if (!queueIT.ftSignaturCreationUnitITId.HasValue)
                {
                    _instance = null;
                    return;
                }

                var signaturCreationUnitIT = await _configurationRepository.GetSignaturCreationUnitITAsync(queueIT.ftSignaturCreationUnitITId.Value).ConfigureAwait(false);
                var uri = GetUriForSignaturCreationUnit(signaturCreationUnitIT);
                var config = new ClientConfiguration
                {
                    Url = uri.ToString(),
                    UrlType = uri.Scheme
                };

                if (_queueConfiguration.ScuTimeoutMs.HasValue)
                {
                    config.Timeout = TimeSpan.FromMilliseconds(_queueConfiguration.ScuTimeoutMs.Value);
                }
                if (_queueConfiguration.ScuMaxRetries.HasValue)
                {
                    config.RetryCount = _queueConfiguration.ScuMaxRetries.Value;
                }

                _instance = _clientFactory.CreateClient(config);
                try
                {
                    var tseInfo = await _instance.GetRTInfoAsync().ConfigureAwait(false);
                    signaturCreationUnitIT.InfoJson = JsonConvert.SerializeObject(tseInfo);
                    await _configurationRepository.InsertOrUpdateSignaturCreationUnitITAsync(signaturCreationUnitIT).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to updated status of SCU (Url: {ScuUrl}, Id: {ScuId}). Will try again later...", config.Url, queueIT.ftSignaturCreationUnitITId.Value);
                }
            }
            finally
            {
                _semaphoreRegister.Release();
            }
        }

        public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
        {
            return await Instance.ProcessReceiptAsync(request);
        }


        public async Task<RTInfo> GetRTInfoAsync()
        {
            return await Instance.GetRTInfoAsync();
        }

        private static Uri GetUriForSignaturCreationUnit(ftSignaturCreationUnitIT signaturCreationUnit)
        {
            var url = signaturCreationUnit.Url;
            try
            {
                var urls = JsonConvert.DeserializeObject<string[]>(url);
                var grpcUrl = urls.FirstOrDefault(x => x.StartsWith("grpc://"));
                url = grpcUrl ?? urls.First();
            }
            catch { }
            return new Uri(url);
        }
    }
}
